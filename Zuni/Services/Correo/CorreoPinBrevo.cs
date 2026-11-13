using System.Net.Http.Json;
using System.Net.Mail;

namespace Zuni.Services.Correo;

public interface ICorreoPin
{
    bool Disponible { get; }
    string UrlPublica { get; }
    Task Enviar(string destinatario,string enlace,CancellationToken ct);
}
public sealed class CorreoPinBrevo(HttpClient http,IConfiguration configuracion) : ICorreoPin
{
    public string UrlPublica => configuracion["Correo:UrlPublica"]??"";
    public bool Disponible => configuracion["Correo:Brevo:ApiKey"] is {} key && key.Length is >=20 and <=4096 && !key.Any(char.IsControl) && !string.IsNullOrWhiteSpace(key) &&
        MailAddress.TryCreate(configuracion["Correo:Remitente"],out _) &&
        configuracion.GetValue<bool>("Correo:RemitenteVerificado") &&
        Uri.TryCreate(UrlPublica,UriKind.Absolute,out var uri)&&uri.Scheme=="https"&&uri.AbsolutePath=="/"&&string.IsNullOrEmpty(uri.Query)&&string.IsNullOrEmpty(uri.Fragment)&&string.IsNullOrEmpty(uri.UserInfo);
    public async Task Enviar(string destinatario,string enlace,CancellationToken ct)
    {
        if(!Disponible)throw new InvalidOperationException("El correo de recuperación todavía no está configurado.");
        using var request=new HttpRequestMessage(HttpMethod.Post,"https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key",configuracion["Correo:Brevo:ApiKey"]);
        request.Content=JsonContent.Create(new {
            sender=new {name="ZUNI",email=configuracion["Correo:Remitente"]},to=new[]{new{email=destinatario}},
            subject="Recuperar tu PIN profesional de ZUNI",
            textContent="Solicitaste restablecer el PIN de tus notas profesionales. Este enlace vence en 20 minutos y solo se puede usar una vez:\n\n"+enlace+"\n\nSi no lo solicitaste, ignora este correo. No compartas el enlace ni tu PIN."
        });
        using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct);
        if(!response.IsSuccessStatusCode)throw new InvalidOperationException("No se pudo enviar el correo de recuperación. Inténtalo más tarde.");
    }
}
