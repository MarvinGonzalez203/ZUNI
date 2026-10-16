using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Zuni.Models.Atencion;
using Zuni.Models.Agenda;
using Zuni.Models.Informes;
using Zuni.Models;
using Zuni.Seguridad;

namespace Zuni.Data;

public static class CifradoClinico
{
    // Lista explícita: no cambia claves, índices, relaciones ni filtros por paciente/psicólogo.
    public static readonly IReadOnlyDictionary<Type,string[]> Campos = new Dictionary<Type,string[]>
    {
        [typeof(ExpedienteInicial)] = ["Sexo","Direccion","IdiomaPreferido","MotivoConsulta","NombreReferente","MotivoReferencia","ConsideracionesAtencion"],
        [typeof(ContactoEmergenciaExpediente)] = ["Nombre","Relacion","Telefono"],
        [typeof(RegistroAtencionCita)] = ["ResultadoProfesionalPrivado","ResenaRecomendacionesEstudiante","ResultadoBreveEstudiante","IndicacionesProximaCita"],
        [typeof(VersionInformeEducativo)] = ["Objetivo","Fuentes","Observaciones","Necesidades","Recomendaciones","ResponsablesAcciones","RelatoRendimiento","ValoracionRendimiento","Estudiante","Carne","Psicologo","Destinatario"],
        [typeof(AutorizacionInforme)] = ["TextoConsentimiento"],
        [typeof(AuditoriaUsuario)] = ["DatosAnteriores","DatosNuevos","Motivo"],
        [typeof(Zuni.Models.Privacidad.SolicitudDerechoDatos)] = ["Detalle","Respuesta"]
    };
    public static string Proposito(Type tipo,string campo) => tipo.Name+"."+campo;
    public static void Configurar(ModelBuilder b,ProteccionPersistencia proteccion)
    {
        foreach(var (tipo,campos) in Campos)
            foreach(var campo in campos)
            {
                var proposito=Proposito(tipo,campo);
                var p=b.Entity(tipo).Property<string>(campo);
                p.HasConversion(new ValueConverter<string,string>(v=>proteccion.Cifrar(v,proposito),v=>proteccion.Descifrar(v,proposito)));
                p.HasColumnType("text"); p.Metadata.SetMaxLength(null);
            }
        b.Entity<RegistroSeguridad>(e=>{
            e.ToTable("RegistrosSeguridad");e.HasKey(r=>r.Id);e.HasIndex(r=>r.EventoId).IsUnique();
            e.HasIndex(r=>new{r.Tipo,r.Referencia}).IsUnique();e.Property(r=>r.Tipo).HasMaxLength(80);e.Property(r=>r.Referencia).HasMaxLength(600);
            e.Property(r=>r.Firma).HasMaxLength(100);
        });
        b.Entity<ExportacionSeguridad>(e=>{e.ToTable("ExportacionesSeguridad");e.HasKey(r=>r.EventoId);});
        b.Entity<EstadoProteccion>(e=>{e.ToTable("EstadoProteccion");e.HasKey(r=>r.Id);e.Property(r=>r.Id).ValueGeneratedNever();});
    }
}
