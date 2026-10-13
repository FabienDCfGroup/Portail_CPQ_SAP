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

builder.Services.AddDbContext<CfSaisieContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("CfSaisie")));

// SAP : implémentation simulée tant que les interfaces SAP ne sont pas documentées.
builder.Services.AddSingleton<SapSimule>();
builder.Services.AddSingleton<ISapClientService>(sp => sp.GetRequiredService<SapSimule>());
builder.Services.AddSingleton<ISapLogistiqueService>(sp => sp.GetRequiredService<SapSimule>());
builder.Services.AddSingleton<ISapCommandeService>(sp => sp.GetRequiredService<SapSimule>());

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

var app = builder.Build();

// Erreurs métier → 400/404/502 avec un message lisible par l'écran.
app.UseExceptionHandler(e => e.Run(async contexte =>
{
    var erreur = contexte.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (statut, message) = erreur switch
    {
        RequeteInvalideException or DemandeLancementInvalideException or EnvironnementInconnuException => (StatusCodes.Status400BadRequest, erreur.Message),
        DossierIntrouvableException => (StatusCodes.Status404NotFound, erreur.Message),
        MiddlewareCpqException => (StatusCodes.Status502BadGateway, erreur.Message),
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

app.MapPortail();
app.MapAdministration();

app.Run();
