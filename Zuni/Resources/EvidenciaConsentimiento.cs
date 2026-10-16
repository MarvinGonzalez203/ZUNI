namespace Zuni.Resources;
public static class EvidenciaConsentimiento
{
    public static string ParticipacionPrueba => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName=="en"
        ? "I understood the instructions and participate voluntarily. My answers will be available to my linked psychologist."
        : "Comprendí las instrucciones y participo voluntariamente. Mis respuestas estarán disponibles para mi psicólogo vinculado.";
    public static string Aviso => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName=="en"
        ? "To document your decision, ZUNI stores your account identifier, the consent text and version, your decision, the date and time, your IP address and browser information. Access and retention follow the institution's privacy policy."
        : "Para dejar constancia de tu decisión, ZUNI conserva el identificador de tu cuenta, el texto y versión del consentimiento, tu decisión, la fecha y hora, tu dirección IP y datos del navegador. El acceso y conservación se rigen por la política de privacidad de la institución.";
}
