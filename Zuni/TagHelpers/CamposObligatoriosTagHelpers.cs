using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Zuni.TagHelpers;

// Solo Required explícito: la nulabilidad de una propiedad no hace obligatorio
// un dato opcional. Las reglas condicionales se declaran en la vista.
[HtmlTargetElement("label")]
[HtmlTargetElement("legend", Attributes = "zuni-required")]
[HtmlTargetElement("h2", Attributes = "zuni-required")]
public sealed class EtiquetaObligatoriaTagHelper : TagHelper
{
    internal const string Marca = "<span class=\"required-marker\" aria-hidden=\"true\"> *</span><span class=\"visually-hidden required-description\"> (Campo obligatorio)</span>";
    [HtmlAttributeName("asp-for")] public ModelExpression? For { get; set; }
    [HtmlAttributeName("zuni-required")] public bool? Obligatorio { get; set; }
    public override int Order => 1000;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var child = await output.GetChildContentAsync();
        var html = output.Content.IsModified ? output.Content.GetContent() : child.GetContent();
        var required = Obligatorio ?? (For?.Metadata.ContainerType?.GetProperty(For.Metadata.PropertyName ?? "")?.IsDefined(typeof(RequiredAttribute), true) == true
            || Regex.IsMatch(html, @"<(?:input|select|textarea)\b[^>]*\srequired(?:\s|=|/?>)", RegexOptions.IgnoreCase));
        output.Attributes.RemoveAll("zuni-required");
        if (context.AllAttributes.ContainsName("zuni-required") || For is not null)
            output.Attributes.SetAttribute("data-required-label", required ? "true" : "false");
        if (required)
        {
            // En etiquetas que envuelven el control, mantener la marca junto al texto.
            html = output.TagName == "label" && Regex.IsMatch(html, @"^\s*<input\b", RegexOptions.IgnoreCase)
                ? html + Marca
                : Regex.Replace(html, @"<(?:input|select|textarea)\b", m => Marca + m.Value, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            if (!html.Contains("class=\"required-marker\"", StringComparison.Ordinal)) html += Marca;
        }
        output.Content.SetHtmlContent(html);
    }
}

[HtmlTargetElement("form")]
public sealed class LeyendaObligatoriaTagHelper : TagHelper
{
    public override int Order => 1000;
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var child = await output.GetChildContentAsync();
        if (child.GetContent().Contains("class=\"required-marker\"", StringComparison.Ordinal))
            output.PreContent.AppendHtml("<p class=\"required-fields-help\"><span class=\"required-marker\" aria-hidden=\"true\">*</span> Campo obligatorio</p>");
    }
}
