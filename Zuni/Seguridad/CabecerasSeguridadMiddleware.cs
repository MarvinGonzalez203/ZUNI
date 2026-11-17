using System.Security.Cryptography;
namespace Zuni.Seguridad;
public sealed class CabecerasSeguridadMiddleware(RequestDelegate next,IConfiguration config)
{
    public const string NonceKey="Zuni.CspNonce";
    public const string PoliticaContenido="frame-ancestors 'none'; object-src 'none'; base-uri 'self'";
    public static string PoliticaEstricta(string nonce)=>$"default-src 'self'; script-src 'self' 'nonce-{nonce}'; script-src-attr 'none'; style-src 'self' 'nonce-{nonce}'; style-src-attr 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'; worker-src 'self'; frame-src 'none'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self'";
    public Task InvokeAsync(HttpContext context)
    {
        var nonce=Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        context.Items[NonceKey]=nonce;
        context.Response.OnStarting(()=>
        {
            var h=context.Response.Headers;
            h.TryAdd("X-Content-Type-Options","nosniff");h.TryAdd("Referrer-Policy","no-referrer");
            h["X-Frame-Options"]="DENY";h.TryAdd("Cross-Origin-Opener-Policy","same-origin");
            h.TryAdd("Cross-Origin-Resource-Policy","same-origin");
            // Report-Only por defecto: activación explícita tras ensayar todos los roles en staging.
            if(config.GetValue<bool>("Seguridad:CspEnforced"))h.TryAdd("Content-Security-Policy",PoliticaEstricta(nonce));
            else {h.TryAdd("Content-Security-Policy",PoliticaContenido);h.TryAdd("Content-Security-Policy-Report-Only",PoliticaEstricta(nonce));}
            h.TryAdd("Permissions-Policy","camera=(), microphone=(), geolocation=()");
            return Task.CompletedTask;
        });
        return next(context);
    }
}
