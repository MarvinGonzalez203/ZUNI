using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Services;
namespace Zuni.Data;
public static class CarrerasConfiguration
{
    public static void ConfigurarCarreras(this ModelBuilder b)
    {
        var e=b.Entity<CarreraInstitucional>();e.ToTable("CarrerasInstitucionales");e.HasIndex(c=>c.NombreNormalizado).IsUnique();e.Property(c=>c.Revision).IsConcurrencyToken();
        var lista=new (string Area,string Nombre)[]{
            ("Ciencias económicas y administrativas","Licenciatura en Administración de Empresas"),
            ("Ciencias económicas y administrativas","Licenciatura en Contaduría Pública y Auditoría"),
            ("Ciencias jurídicas y sociales","Licenciatura en Ciencias Jurídicas y Sociales (Abogado y Notario)"),
            ("Ciencias jurídicas y sociales","Licenciatura en Criminología y Política Criminal"),
            ("Ciencias jurídicas y sociales","Licenciatura en Trabajo Social"),
            ("Ingeniería y tecnología","Ingeniería en Sistemas de Información y Ciencias de la Computación"),
            ("Ciencias de la salud","Licenciatura en Enfermería"),
            ("Ciencias de la salud","Profesorado / Técnico en Enfermería"),
            ("Salud mental","Licenciatura en Psicología Clínica")};
        e.HasData(lista.Select((c,i)=>new CarreraInstitucional{Id=Guid.Parse($"caaaaaaa-0000-0000-0000-{i+1:000000000000}"),Revision=Guid.Parse($"cbbbbbbb-0000-0000-0000-{i+1:000000000000}"),Nombre=c.Nombre,NombreNormalizado=CatalogoCarreras.Normalizar(c.Nombre),Area=c.Area,Activa=true}));
    }
}
