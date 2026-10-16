using System.Security.Claims;
using CfSaisie.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace CfSaisie.Api;

/// <summary>
/// Connexion Microsoft Entra ID (section « Authentification »). Les identifiants (locataire, application, groupes)
/// sont dans la configuration ; le secret client dans les user-secrets en local, dans les secrets du conteneur ensuite.
/// </summary>
public sealed class OptionsAuthentification
{
    public const string Section = "Authentification";

    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Secret client de l'inscription d'application : jamais dans appsettings ni dans Git.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Adresse de retour déclarée dans l'inscription d'application (ex. https://localhost:5173).
    /// Si elle est renseignée, Microsoft renvoie sur cette adresse et la page d'accueil transmet le code à /api/signin-oidc ;
    /// sinon, l'adresse de retour est {portail}/api/signin-oidc.
    /// </summary>
    public string? AdresseRetour { get; set; }

    /// <summary>Identifiants d'objet (GUID) des groupes Entra ID donnant chaque rôle du portail.</summary>
    public OptionsGroupes Groupes { get; set; } = new();
}

public sealed class OptionsGroupes
{
    public string Utilisateur { get; set; } = string.Empty;
    public string Administrateur { get; set; } = string.Empty;
    public string SuperAdministrateur { get; set; } = string.Empty;
}

public static class Authentification
{
    // Les connexions et déconnexions passent par /api pour profiter du relais de Vite en développement.
    public const string CheminRetour = "/api/signin-oidc";
    public const string CheminRetourDeconnexion = "/api/signout-callback-oidc";

    public static void AjouterAuthentification(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<OptionsAuthentification>()
            .Bind(builder.Configuration.GetSection(OptionsAuthentification.Section))
            .Validate(a => Guid.TryParse(a.TenantId, out _) && Guid.TryParse(a.ClientId, out _),
                "Authentification:TenantId et Authentification:ClientId doivent être les identifiants (GUID) de l'inscription d'application Entra ID.")
            .Validate(a => !string.IsNullOrWhiteSpace(a.ClientSecret),
                "Authentification:ClientSecret est absent. Renseignez-le avec : dotnet user-secrets set \"Authentification:ClientSecret\" \"<secret>\" --project src/CfSaisie.Api")
            .Validate(a => string.IsNullOrWhiteSpace(a.AdresseRetour) || Uri.TryCreate(a.AdresseRetour, UriKind.Absolute, out _),
                "Authentification:AdresseRetour doit être une adresse absolue (ex. https://localhost:5173).")
            .Validate(a => new[] { a.Groupes.Utilisateur, a.Groupes.Administrateur, a.Groupes.SuperAdministrateur }.All(g => Guid.TryParse(g, out _)),
                "Authentification:Groupes:Utilisateur, :Administrateur et :SuperAdministrateur doivent être les identifiants (GUID) des groupes Entra ID.")
            .ValidateOnStart();

        builder.Services
            .AddAuthentication(o =>
            {
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                o.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(o =>
            {
                o.Cookie.Name = "CfSaisie.Session";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.ExpireTimeSpan = TimeSpan.FromHours(8);
                o.SlidingExpiration = true;
                // Appels de l'écran (fetch) : codes HTTP, pas de redirection.
                o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
                o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
            })
            .AddOpenIdConnect();

        builder.Services.AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
            .Configure<IOptions<OptionsAuthentification>>((o, options) => Configurer(o, options.Value));

        builder.Services.AddAuthorization(o =>
        {
            o.AddPolicy(Roles.PolitiquePortail, p => p.RequireRole(Roles.Utilisateur, Roles.Administrateur, Roles.SuperAdministrateur));
            o.AddPolicy(Roles.PolitiqueAdministration, p => p.RequireRole(Roles.Administrateur, Roles.SuperAdministrateur));
            o.AddPolicy(Roles.PolitiqueConfigurateurs, p => p.RequireRole(Roles.SuperAdministrateur));
        });
    }

    private static void Configurer(OpenIdConnectOptions o, OptionsAuthentification a)
    {
        o.Authority = $"{a.Instance.TrimEnd('/')}/{a.TenantId}/v2.0";
        o.ClientId = a.ClientId;
        o.ClientSecret = a.ClientSecret;
        o.ResponseType = OpenIdConnectResponseType.Code;
        o.UsePkce = true;
        // Retour en GET (et non en POST) : les cookies de corrélation restent en SameSite=Lax, y compris en http://localhost.
        o.ResponseMode = OpenIdConnectResponseMode.Query;
        o.CorrelationCookie.SameSite = SameSiteMode.Lax;
        o.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.NonceCookie.SameSite = SameSiteMode.Lax;
        o.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.CallbackPath = CheminRetour;
        o.SignedOutCallbackPath = CheminRetourDeconnexion;
        o.SignedOutRedirectUri = "/";
        o.Scope.Clear();
        o.Scope.Add("openid");
        o.Scope.Add("profile");
        o.Scope.Add("email");
        o.SaveTokens = false;
        // Noms de revendications d'origine (oid, name, preferred_username, groups) et rôle du portail dans « role ».
        o.MapInboundClaims = false;
        o.TokenValidationParameters.NameClaimType = "name";
        o.TokenValidationParameters.RoleClaimType = "role";

        o.Events.OnRedirectToIdentityProvider = c =>
        {
            // Seule /api/connexion déclenche la page Microsoft ; un appel d'écran non connecté reçoit 401.
            if (!c.Request.Path.StartsWithSegments("/api/connexion"))
            {
                c.Response.StatusCode = StatusCodes.Status401Unauthorized;
                c.HandleResponse();
                return Task.CompletedTask;
            }
            // Adresse déclarée dans Entra ID : elle est aussi reprise par le gestionnaire pour échanger le code (même valeur exigée).
            if (!string.IsNullOrWhiteSpace(a.AdresseRetour))
            {
                c.ProtocolMessage.RedirectUri = a.AdresseRetour;
            }
            if (c.Properties.Items.TryGetValue("prompt", out var prompt) && prompt is not null)
            {
                c.ProtocolMessage.Prompt = prompt;
            }
            return Task.CompletedTask;
        };

        o.Events.OnTokenValidated = async c =>
        {
            var identite = (ClaimsIdentity)c.Principal!.Identity!;
            var groupes = identite.FindAll("groups").Select(g => g.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            // Au-delà d'environ 200 groupes, Entra ID n'émet plus la liste (« overage ») : l'inscription doit être
            // configurée pour n'émettre que les groupes attribués à l'application.
            var listeTronquee = identite.HasClaim(cl => cl.Type is "hasgroups" or "_claim_names");
            var role = DeterminerRole(groupes, a.Groupes);

            if (role is null)
            {
                await JournaliserAsync(c.HttpContext, "RefuseSansGroupe", c.Principal, null,
                    listeTronquee ? "Liste des groupes non émise par Entra ID (trop de groupes) : limiter le jeton aux groupes attribués à l'application." : null);
                c.HandleResponse();
                c.Response.Redirect("/?acces=refuse");
                return;
            }

            // Le cookie ne garde que le rôle du portail, pas la liste des groupes.
            foreach (var g in identite.FindAll("groups").ToList()) identite.RemoveClaim(g);
            identite.AddClaim(new Claim("role", role));
            await JournaliserAsync(c.HttpContext, "Succes", c.Principal, role, null);
        };

        o.Events.OnRemoteFailure = async c =>
        {
            await JournaliserAsync(c.HttpContext, "Echec", null, null, c.Failure?.Message);
            c.HandleResponse();
            c.Response.Redirect("/?acces=erreur");
        };
    }

    /// <summary>Rôle le plus élevé parmi les groupes du portail ; null si la personne n'appartient à aucun.</summary>
    public static string? DeterminerRole(IReadOnlySet<string> groupes, OptionsGroupes g) =>
        groupes.Contains(g.SuperAdministrateur) ? Roles.SuperAdministrateur
        : groupes.Contains(g.Administrateur) ? Roles.Administrateur
        : groupes.Contains(g.Utilisateur) ? Roles.Utilisateur
        : null;

    private static async Task JournaliserAsync(HttpContext http, string resultat, ClaimsPrincipal? utilisateur, string? role, string? message)
    {
        var db = http.RequestServices.GetRequiredService<CfSaisieContext>();
        var horloge = http.RequestServices.GetRequiredService<TimeProvider>();
        db.JournalConnexions.Add(new JournalConnexion
        {
            Date = horloge.GetLocalNow().DateTime,
            Resultat = resultat,
            ObjetId = utilisateur?.FindFirst("oid")?.Value,
            Compte = Tronquer(utilisateur?.FindFirst("preferred_username")?.Value, 256),
            Nom = Tronquer(utilisateur?.FindFirst("name")?.Value, 200),
            Role = role,
            Message = Tronquer(message, 500),
            AdresseIp = Tronquer(http.Connection.RemoteIpAddress?.ToString(), 64),
            Navigateur = Tronquer(http.Request.Headers.UserAgent.ToString(), 512),
        });
        await db.SaveChangesAsync(http.RequestAborted);
    }

    private static string? Tronquer(string? v, int max) => string.IsNullOrEmpty(v) ? null : v.Length <= max ? v : v[..max];

    /// <summary>Connexion (page Microsoft) et déconnexion, accessibles sans être connecté.</summary>
    public static void MapConnexion(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/connexion", (string? retour, string? compte) =>
        {
            var proprietes = new AuthenticationProperties { RedirectUri = RetourLocal(retour) };
            // « Se connecter avec un autre compte » après un refus.
            if (compte == "autre") proprietes.Items["prompt"] = "select_account";
            return Results.Challenge(proprietes, [OpenIdConnectDefaults.AuthenticationScheme]);
        }).AllowAnonymous();

        app.MapGet("/api/deconnexion", () =>
            Results.SignOut(new AuthenticationProperties { RedirectUri = "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
            .AllowAnonymous();
    }

    // Retour après connexion limité au portail lui-même (pas de redirection vers un autre site).
    private static string RetourLocal(string? retour) =>
        !string.IsNullOrEmpty(retour) && retour.StartsWith('/') && !retour.StartsWith("//") && !retour.StartsWith("/\\") ? retour : "/";
}
