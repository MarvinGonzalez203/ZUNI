using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Globalization;

namespace Zuni.Localizacion;

public static class ConfiguracionIdioma
{
    public static IServiceCollection AddZuniIdiomas(this IServiceCollection services,IConfiguration config)
    {
        services.AddLocalization();
        services.AddControllersWithViews(o=> {o.Filters.Add<MensajesLocalizados>();o.ModelMetadataDetailsProviders.Add(new EtiquetasLocalizadas());})
            .AddDataAnnotationsLocalization(o=>o.DataAnnotationLocalizerProvider=(_,_)=>new Localizador());
        services.Configure<RequestLocalizationOptions>(o=> {
            o.DefaultRequestCulture=new RequestCulture("es-GT","es");
            o.SupportedCultures=[new CultureInfo("es-GT"),new CultureInfo("en-US")];
            o.SupportedUICultures=[new CultureInfo("es"),new CultureInfo("en")];
            o.ApplyCurrentCultureToResponseHeaders=true;
            o.RequestCultureProviders=[new CookieRequestCultureProvider { CookieName=NombreCookie(config) }];
        });
        return services;
    }
    public static string NombreCookie(IConfiguration config)=>Zuni.Seguridad.AislamientoInstalacion.Cookie(config)+".Idioma";
}

public sealed class EtiquetasLocalizadas : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IDisplayMetadataProvider
{
    public void CreateDisplayMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DisplayMetadataProviderContext context)
    {
        var nombre=context.DisplayMetadata.DisplayName;
        if(nombre is not null)context.DisplayMetadata.DisplayName=()=>Idioma.Texto(nombre());
    }
}

public sealed class MensajesLocalizados : IAsyncAlwaysRunResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context,ResultExecutionDelegate next)
    {
        if(Idioma.EsIngles)
        {
            foreach(var state in context.ModelState.Values)
                for(var i=0;i<state.Errors.Count;i++)
                    if(state.Errors[i].Exception is null) state.Errors[i]=new ModelError(Idioma.Texto(state.Errors[i].ErrorMessage));
        }
        await next();
    }
}
