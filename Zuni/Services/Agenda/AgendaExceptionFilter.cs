using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Zuni.Services.Agenda;

// Se aplica solo a las acciones nuevas; nunca expone SQL ni datos de conexión.
public sealed class AgendaExceptionFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        context.Result = context.Exception switch
        {
            AgendaAccesoException => new ForbidResult(),
            AgendaOperacionException ex => new ViewResult
            {
                ViewName = "~/Views/Shared/AgendaError.cshtml",
                StatusCode = 409,
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                    new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), context.ModelState) { Model = ex.Message }
            },
            Microsoft.EntityFrameworkCore.DbUpdateException or Npgsql.NpgsqlException =>
                new ContentResult { StatusCode = 503, Content = Zuni.Localizacion.Idioma.Texto("Agenda no disponible temporalmente. Revisa tus citas antes de repetir una operación.") },
            _ => null
        };
        context.ExceptionHandled = context.Result is not null;
    }
}
