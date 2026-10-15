using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Models.Institucion;
namespace Zuni.Controllers;

[Authorize(Roles="Administrador,SuperAdmin"),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class TarifasController:Controller
{
    [HttpGet] public IActionResult Index()=>View(TarifaSaas.Propuesta);
}
