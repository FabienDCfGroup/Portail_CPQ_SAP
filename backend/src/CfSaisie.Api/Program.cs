using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json.Serialization;
using CfSaisie.Api;
using CfSaisie.Api.Endpoints;
using CfSaisie.Api.Services;
using CfSaisie.Cpq;
using CfSaisie.Data;
using CfSaisie.Sap;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOptions<OptionsMiddleware>()
    .Bind(builder.Configuration.GetSection(OptionsMiddleware.Section))
    .Validate(o => o.Environnements.Count > 0 && o.Environnements.Values.All(e => !string.IsNullOrWhiteSpace(e.Serveur)),
        "Middleware:Environnements doit contenir au moins un environnement avec un serveur.")
    .Validate(o => o.Environnements.ContainsKey(o.EnvironnementParDefaut), "Middleware:EnvironnementParDefaut doit être un environnement déclaré.")
    .Validate(o => o.Simulation || !string.IsNullOrWhiteSpace(o.Profil.MotDePasse),
        "Middleware:Profil:MotDePasse est absent. Renseignez-le avec : dotnet user-secrets set \"Middleware:Profil:MotDePasse\" \"<mot de passe>\" --project src/CfSaisie.Api")
    .Validate(o => o.Simulation || o.Environnements.Values.All(e => e.Profil is null
            || (!string.IsNullOrWhiteSpace(e.Profil.Identifiant) && !string.IsNullOrWhiteSpace(e.Profil.MotDePasse))),
        "Un profil propre à un environnement doit avoir un identifiant et un mot de passe (Middleware:Environnements:<code>:Profil).")
    .ValidateOnStart();
builder.Services.Configure<OptionsPortail>(builder.Configuration.GetSection(OptionsPortail.Section));
builder.AjouterAuthentification();

// Export XML des commandes et devis validés : dossier propre à chaque environnement (poste, test, production).
builder.Services.AddOptions<OptionsExport>()
    .Bind(builder.Configuration.GetSection(OptionsExport.Section))
    .Validate(o => OptionsExport.EstUtilisable(o.DossierXml),
        "Export:DossierXml doit désigner un dossier existant et accessible en écriture (variable d'environnement Export__DossierXml sur les serveurs).")
    .ValidateOnStart();
builder.Services.AddScoped<ExportXmlDossier>();

builder.Services.AddDbContext<CfSaisieContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("CfSaisie")));

// Données ERP (clients, adresses, agences) : tables du schéma erp, alimentées par le flux ERP.
builder.Services.AddScoped<DonneesErp>();
builder.Services.AddScoped<ISapClientService>(sp => sp.GetRequiredService<DonneesErp>());
builder.Services.AddScoped<ISapLogistiqueService>(sp => sp.GetRequiredService<DonneesErp>());

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<OpenRequestBuilder>();
if (builder.Configuration.GetValue<bool>($"{OptionsMiddleware.Section}:{nameof(OptionsMiddleware.Simulation)}"))
{
    builder.Services.AddSingleton<IMiddlewareCpq, MiddlewareCpqSimule>();
}
else
{
    // Même délai que l'ancien outil (JsonHelpers : 5 minutes).
    builder.Services.AddHttpClient<IMiddlewareCpq, MiddlewareCpqClient>(c => c.Timeout = TimeSpan.FromMinutes(5));
}
builder.Services.AddScoped<LanceurConfigurateur>();
builder.Services.AddScoped<ServiceDossiers>();
builder.Services.AddScoped<ServiceLancements>();
builder.Services.AddScoped<ServicePlanning>();
builder.Services.AddScoped<ServiceSuivi>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LangueInterface>();

// API Planning CCX (n8n) : simulée tant que la clé n'est pas renseignée, ce qui n'est admis que sur le poste de développement.
builder.Services.AddOptions<ParametresPlanningCcx>()
    .Bind(builder.Configuration.GetSection(ParametresPlanningCcx.Section))
    .Validate(p => !p.Simulation || builder.Environment.IsDevelopment(),
        "PlanningCcx:CleApi est absente. Renseignez-la (secret du conteneur, ou en local : dotnet user-secrets set \"PlanningCcx:CleApi\" \"<clé>\" --project src/CfSaisie.Api)")
    .Validate(p => p.Simulation || Uri.TryCreate(p.Url, UriKind.Absolute, out _), "PlanningCcx:Url doit être une URL absolue.")
    .Validate(p => p.NbJours > 0 && p.SeuilPlacesLimitees >= 0, "PlanningCcx:NbJours et PlanningCcx:SeuilPlacesLimitees sont invalides.")
    .ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ParametresPlanningCcx>>().Value);
if (string.IsNullOrWhiteSpace(builder.Configuration[$"{ParametresPlanningCcx.Section}:{nameof(ParametresPlanningCcx.CleApi)}"]))
{
    builder.Services.AddSingleton<IPlanningCcxService, PlanningCcxSimule>();
}
else
{
    builder.Services.AddHttpClient<IPlanningCcxService, PlanningCcxClient>(c => c.Timeout = TimeSpan.FromSeconds(20));
}

var app = builder.Build();

// Erreurs métier → 400/404/502 avec un message lisible par l'écran.
app.UseExceptionHandler(e => e.Run(async contexte =>
{
    var erreur = contexte.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (statut, message) = erreur switch
    {
        RequeteInvalideException or DemandeLancementInvalideException or EnvironnementInconnuException => (StatusCodes.Status400BadRequest, erreur.Message),
        DossierIntrouvableException => (StatusCodes.Status404NotFound, erreur.Message),
        MiddlewareCpqException or PlanningCcxException => (StatusCodes.Status502BadGateway, erreur.Message),
        ExportXmlException => (StatusCodes.Status500InternalServerError, erreur.Message),
        _ => (StatusCodes.Status500InternalServerError, "Erreur inattendue du serveur."),
    };
    if (statut == StatusCodes.Status502BadGateway || statut == StatusCodes.Status500InternalServerError)
    {
        app.Logger.LogError(erreur, "Erreur lors du traitement de {Chemin}", contexte.Request.Path);
    }
    await Results.Problem(message, statusCode: statut).ExecuteAsync(contexte);
}));

using (var scope = app.Services.CreateScope())
{
    await InitialisationBase.InitialiserAsync(scope.ServiceProvider.GetRequiredService<CfSaisieContext>());
}

// Derrière un relais (Vite en développement, proxy en déploiement) : schéma https et IP d'origine du navigateur,
// pour les adresses envoyées à Microsoft, le cookie sécurisé et le journal des connexions.
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor });

// Authentification Entra ID : cookie de session ; toutes les routes /api exigent un rôle du portail (sauf connexion/déconnexion).
app.UseAuthentication();
app.UseAuthorization();

app.MapPortail();
app.MapAdministration();
app.MapConnexion();

app.Run();
