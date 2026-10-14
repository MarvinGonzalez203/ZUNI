namespace Zuni.Models.Atencion;
public sealed record MiSolicitudConsulta(EstadoSolicitudViewModel? Solicitud,string? Psicologo,bool PerfilCompleto,bool EdadVerificada);
public sealed record SolicitudPsicologoResumen(Guid Id,string Estudiante,string Carne,EstadoSolicitudAtencion Estado,DateTime FechaUtc,bool PuedeConsultarExpediente);
