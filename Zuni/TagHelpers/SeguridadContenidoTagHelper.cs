using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Zuni.Seguridad;
using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;
using System.Net;
namespace Zuni.TagHelpers;
// Se aplica a las plantillas Razor de confianza; nunca convierte HTML aportado por usuarios en código confiable.
[HtmlTargetElement("script")][HtmlTargetElement("style")]
[HtmlTargetElement(Attributes="href")][HtmlTargetElement(Attributes="src")][HtmlTargetElement(Attributes="action")]
[HtmlTargetElement(Attributes="data-url")]
public sealed class SeguridadContenidoTagHelper : TagHelper
{
    [HtmlAttributeNotBound][ViewContext] public ViewContext Vista {get;set;}=null!;
    public override int Order=>int.MaxValue;
    public override void Process(TagHelperContext context,TagHelperOutput output)
    {
        if(output.TagName is "script" or "style" && Vista.HttpContext.Items[CabecerasSeguridadMiddleware.NonceKey] is string nonce)
            output.Attributes.SetAttribute("nonce",nonce);
        foreach(var name in new[]{"href","src","action","data-url"})
        {
            var original=output.Attributes[name]?.Value;
            if(original is null)continue;
            string value;
            if(original is IHtmlContent content)
            {
                using var writer=new StringWriter();content.WriteTo(writer,HtmlEncoder.Default);
                value=WebUtility.HtmlDecode(writer.ToString());
            }
            else value=original.ToString()??"";
            var route=BasePublica.Ruta(value,Vista.HttpContext.Request.PathBase);
            if(route!=value)output.Attributes.SetAttribute(name,route);
        }
    }
}
