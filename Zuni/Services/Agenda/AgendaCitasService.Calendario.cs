using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;

namespace Zuni.Services.Agenda;

public sealed partial class AgendaCitasService
{
    private IQueryable<DisponibilidadPsicologo> Intervalos(string id, DateOnly fecha, ConfiguracionAgendaDia? dia)
    {
        var query = db.DisponibilidadesPsicologo.Where(d => d.PsicologoUsuarioId == id && d.Activo);
        return dia is { UsaBaseSemanal: false }
            ? query.Where(d => d.ConfiguracionAgendaDiaId == dia.Id)
            : query.Where(d => d.ConfiguracionAgendaDiaId == null && d.DiaSemana == fecha.DayOfWeek);
    }

    private async Task<CalendarioAgendaViewModel> LeerCalendario(string? id, DateOnly? fecha, bool editor,
        Guid? perfilId, CancellationToken ct)
    {
        var ahora = Ahora;
        var hoy = AgendaReglas.Hoy(ahora);
        var mes = AgendaReglas.MesPermitido(fecha, ahora, true);
        var siguiente = mes.AddMonths(1);
        var configuraciones = await db.ConfiguracionesAgendaDia.AsNoTracking()
            .Where(d => d.PsicologoUsuarioId == id && d.Fecha >= mes && d.Fecha < siguiente).ToListAsync(ct);
        var ids = configuraciones.Select(d => d.Id).ToArray();
        var intervalos = await db.DisponibilidadesPsicologo.AsNoTracking().Where(d => d.PsicologoUsuarioId == id && d.Activo &&
            (d.ConfiguracionAgendaDiaId == null || ids.Contains(d.ConfiguracionAgendaDiaId.Value))).ToListAsync(ct);
        var inicio = AgendaReglas.Utc(mes, TimeOnly.MinValue);
        var fin = AgendaReglas.Utc(siguiente, TimeOnly.MinValue);
        var almuerzo=await Almuerzo(id,ct);
        var pendientes=await db.Citas.AsNoTracking().Where(c=>c.Estado==EstadoCita.Solicitada&&c.FechaHoraInicioUtc>=inicio&&c.FechaHoraInicioUtc<fin&&
            ((editor||perfilId==null)?c.PsicologoUsuarioId==id:c.PerfilEstudianteId==perfilId)).Select(c=>c.FechaHoraInicioUtc).ToListAsync(ct);
        var citasConsulta = editor && id is not null ? await ResumenPsicologo(id).Where(c => c.InicioUtc >= inicio && c.InicioUtc < fin).OrderBy(c => c.InicioUtc).ToListAsync(ct) : [];
        if (!editor && perfilId is not null)
            citasConsulta = await (from c in db.Citas.AsNoTracking() join u in db.Users on c.PsicologoUsuarioId equals u.Id
                where c.PerfilEstudianteId == perfilId && c.FechaHoraInicioUtc >= inicio && c.FechaHoraInicioUtc < fin
                orderby c.FechaHoraInicioUtc select new CitaResumen { Id=c.Id, Nombre=u.FullName, InicioUtc=c.FechaHoraInicioUtc, FinUtc=c.FechaHoraFinUtc, Estado=c.Estado, Modalidad=c.Modalidad, FinAproximado=c.FinAproximado }).ToListAsync(ct);
        // Solo horas y pertenencia propia: nunca proyectar la identidad de otra persona al calendario.
        var ocupadas = await db.Citas.AsNoTracking().Where(c => EstadosActivos.Contains(c.Estado) &&
            (c.PsicologoUsuarioId == id || c.PerfilEstudianteId == perfilId) && c.FechaHoraInicioUtc < fin && c.FechaHoraFinUtc > inicio)
            .Select(c => new { Inicio = c.FechaHoraInicioUtc, Fin = c.FechaHoraFinUtc,
                Propia = c.PerfilEstudianteId == perfilId, Profesional = c.PsicologoUsuarioId == id }).ToListAsync(ct);
        var dias = new List<DiaAgendaViewModel>();
        for (var dia = mes; dia < siguiente; dia = dia.AddDays(1))
        {
            var config = configuraciones.SingleOrDefault(c => c.Fecha == dia);
            var baseSemanal = config?.UsaBaseSemanal != false;
            var rangos = intervalos.Where(d => baseSemanal
                ? d.ConfiguracionAgendaDiaId == null && d.DiaSemana == dia.DayOfWeek
                : d.ConfiguracionAgendaDiaId == config!.Id).OrderBy(d => d.HoraInicio).ToArray();
            var desde = AgendaReglas.Utc(dia, TimeOnly.MinValue);
            var hasta = AgendaReglas.Utc(dia.AddDays(1), TimeOnly.MinValue);
            var citasDia = ocupadas.Where(c => c.Inicio < hasta && c.Fin > desde).ToArray();
            var franjas = config?.Bloqueado == true ? [] : rangos.SelectMany(d => d.DuracionMinutos == 0
                    // Sin duración establecida: huecos libres y tramos ocupados reales; cada cita ocupa
                    // solo su propio horario y el resto del rango sigue reservable.
                    ? AgendaReglas.TramosVariables(d, dia, citasDia.Select(c => (c.Inicio, c.Fin)).Concat(almuerzo?.Habilitado==true?new[]{(AgendaReglas.Utc(dia,almuerzo.Inicio),AgendaReglas.Utc(dia,almuerzo.Fin))}:[]), ahora)
                        .Select(t => new FranjaAgenda(t.Inicio, t.Fin, d.Modalidad, t.Ocupada, true))
                    : AgendaReglas.Franjas(d, dia)
                        .Where(f => f.Inicio > ahora && !CruzaAlmuerzo(almuerzo,f.Inicio,f.Fin))
                        .Select(f => new FranjaAgenda(f.Inicio, f.Fin, d.Modalidad,
                            citasDia.Any(c => AgendaReglas.Cruza(f.Inicio, f.Fin, c.Inicio, c.Fin)), false)))
                .OrderBy(f => f.InicioUtc).ToArray();
            dias.Add(new DiaAgendaViewModel
            {
                Fecha = dia, Editable = dia >= hoy && dia <= hoy.AddMonths(12),
                Consultable = dia <= hoy.AddMonths(12),
                Citas = citasConsulta.Where(c => c.InicioUtc >= desde && c.InicioUtc < hasta).Select(c=>new CitaCalendarioResumen {
                    Id=c.Id,Revision=c.Revision,ProgramadorRol=c.ProgramadorRol,Nombre=c.Nombre,InicioUtc=c.InicioUtc,FinUtc=c.FinUtc,Estado=c.Estado,Modalidad=c.Modalidad,
                    EsImprevista=c.EsImprevista,FinAproximado=c.FinAproximado,TieneRegistroAtencion=c.TieneRegistroAtencion }).ToArray(),
                Bloqueado = config?.Bloqueado == true, UsaBaseSemanal = baseSemanal,
                TieneCitasActivas = citasDia.Any(c => c.Profesional),
                CitaPropiaActiva = citasDia.Any(c => c.Propia),
                PendientesPorRevisar=pendientes.Count(p=>p>=desde&&p<hasta),
                Estado = pendientes.Any(p=>p>=desde&&p<hasta)?"Pendiente de revisar":dia < hoy ? "Pasado" : dia > hoy.AddMonths(12) ? "Fuera de plazo" :
                    config?.Bloqueado == true ? "Ocupado" : franjas.Any(f => !f.Ocupada) ? "Disponible" :
                    rangos.Length > 0 ? "Ocupado" : "Sin horarios",
                Franjas = franjas,
                Intervalos = !editor ? [] : rangos.Select(d => new IntervaloAgenda(d.Id, d.HoraInicio, d.HoraFin,
                    d.DuracionMinutos, d.Modalidad, citasDia.Any(c => c.Profesional &&
                        AgendaReglas.Cruza(AgendaReglas.Utc(dia, d.HoraInicio), AgendaReglas.Utc(dia, d.HoraFin), c.Inicio, c.Fin)))).ToArray()
            });
        }
        return new CalendarioAgendaViewModel { Mes = mes, Hoy = hoy, Editor = editor, Historico=true, Dias = dias, AhoraUtc = ahora,Almuerzo=almuerzo?.Habilitado==true?new AlmuerzoResumen(almuerzo.Inicio,almuerzo.Fin):null,
            EstudiantesProgramables = editor && id is not null ? await SeleccionProgramables((id,"Psicologo"),ct) : [] };
    }

    // Materialización solo al editar intervalos de una fecha; bloquear/habilitar no duplica la base.
    // Las filas originales se conservan porque las citas históricas pueden seguir referenciándolas.
    private List<DisponibilidadPsicologo> Particularizar(ConfiguracionAgendaDia dia, List<DisponibilidadPsicologo> actuales)
    {
        if (!dia.UsaBaseSemanal) return actuales;
        var copias = actuales.Select(d => new DisponibilidadPsicologo
        {
            PsicologoUsuarioId = dia.PsicologoUsuarioId, ConfiguracionAgendaDiaId = dia.Id,
            DiaSemana = dia.Fecha.DayOfWeek, HoraInicio = d.HoraInicio, HoraFin = d.HoraFin,
            DuracionMinutos = d.DuracionMinutos, Modalidad = d.Modalidad
        }).ToList();
        db.DisponibilidadesPsicologo.AddRange(copias);
        dia.UsaBaseSemanal = false;
        return copias;
    }

    private async Task<ConfiguracionAgendaDia> ObtenerDia(string id, DateOnly fecha, CancellationToken ct)
    {
        var dia = await db.ConfiguracionesAgendaDia.SingleOrDefaultAsync(d => d.PsicologoUsuarioId == id && d.Fecha == fecha, ct);
        if (dia is not null) return dia;
        dia = new ConfiguracionAgendaDia { PsicologoUsuarioId = id, Fecha = fecha };
        db.ConfiguracionesAgendaDia.Add(dia);
        return dia;
    }

    public Task AgregarHorarioDia(ClaimsPrincipal sesion, HorarioDiaInput input, CancellationToken ct) => Escribir(async () =>
    {
        var id = await Psicologo(sesion, ct);
        AgendaReglas.ValidarHorarioDia(input, Ahora);
        var fecha = input.Fecha!.Value;
        var dia = await ObtenerDia(id, fecha, ct);
        if (dia.Bloqueado) throw new AgendaOperacionException("Habilita el día antes de añadir horarios.");
        var actuales = await Intervalos(id, fecha, dia).ToListAsync(ct);
        var inicio = input.HoraInicio!.Value;
        var fin = input.HoraFin!.Value;
        if (actuales.Any(d => d.HoraInicio < fin && inicio < d.HoraFin))
            throw new AgendaOperacionException("El horario se superpone con otra disponibilidad activa.");
        Particularizar(dia, actuales);
        var nuevo = new DisponibilidadPsicologo
        {
            PsicologoUsuarioId = id, ConfiguracionAgendaDiaId = dia.Id, DiaSemana = fecha.DayOfWeek,
            HoraInicio = inicio, HoraFin = fin, DuracionMinutos = input.DuracionMinutos, Modalidad = input.Modalidad
        };
        db.DisponibilidadesPsicologo.Add(nuevo);
        if(input.PerfilEstudianteId is {}perfil) {
            var actor=await Programador(sesion,ct);
            if(input.Modalidad==ModalidadDisponibilidad.Ambas)throw new AgendaOperacionException("Para programar una cita al estudiante elige Presencial o Virtual; Ambas se utiliza para disponibilidad sin cita asignada.");
            await ProfesionalProgramacion(actor,perfil,ct);
            var comienza=AgendaReglas.Utc(fecha,inicio);var termina=comienza.AddMinutes(input.DuracionMinutos==0?50:input.DuracionMinutos);
            if(termina>AgendaReglas.Utc(fecha,fin))throw new AgendaOperacionException("El horario no alcanza para la cita prevista del estudiante.");
            await ValidarFranjaProgramada(perfil,id,comienza,termina,null,ct);
            var c=new Cita{PerfilEstudianteId=perfil,PsicologoUsuarioId=id,DisponibilidadPsicologoId=nuevo.Id,FechaHoraInicioUtc=comienza,FechaHoraFinUtc=termina,FinAproximado=true,ProgramadorUsuarioId=actor.Id,ProgramadorRol=actor.Rol,Estado=await EstadoProgramacion(actor,perfil,(ModalidadCita)input.Modalidad,ct),Modalidad=(ModalidadCita)input.Modalidad,FechaCreacionUtc=Ahora};
            db.Citas.Add(c);await NotificarProgramacion(c,actor,false,ct);
        }
    }, ct);

    public Task CambiarDia(ClaimsPrincipal sesion, DateOnly fecha, string operacion, Guid? horarioId, CancellationToken ct) => Escribir(async () =>
    {
        var id = await Psicologo(sesion, ct);
        AgendaReglas.FechaPermitida(fecha, Ahora);
        if (operacion is not ("bloquear" or "habilitar" or "limpiar" or "quitar"))
            throw new AgendaOperacionException("Operación no válida.");
        var dia = await ObtenerDia(id, fecha, ct);
        var actuales = await Intervalos(id, fecha, dia).OrderBy(d => d.HoraInicio).ToListAsync(ct);
        var elegido = operacion == "quitar" ? actuales.SingleOrDefault(d => d.Id == horarioId) : null;
        if (operacion == "quitar" && elegido is null) throw new AgendaOperacionException("El horario no pertenece a esta fecha o ya cambió.");
        if (operacion != "habilitar")
        {
            var desde = AgendaReglas.Utc(fecha, elegido?.HoraInicio ?? TimeOnly.MinValue);
            var hasta = elegido is null ? AgendaReglas.Utc(fecha.AddDays(1), TimeOnly.MinValue) : AgendaReglas.Utc(fecha, elegido.HoraFin);
            if (await db.Citas.AnyAsync(c => c.PsicologoUsuarioId == id && EstadosActivos.Contains(c.Estado) &&
                c.FechaHoraInicioUtc < hasta && c.FechaHoraFinUtc > desde, ct))
                throw new AgendaOperacionException("Este horario tiene citas activas. Cancélalas o reprográmalas antes de modificarlo.");
        }
        if (operacion is "bloquear" or "habilitar") dia.Bloqueado = operacion == "bloquear";
        else if (operacion == "limpiar")
        {
            // Excepción vacía: suprime la base solo para esta fecha, sin borrar ninguna fila.
            foreach (var d in actuales.Where(d => d.ConfiguracionAgendaDiaId == dia.Id)) d.Activo = false;
            dia.UsaBaseSemanal = false;
            dia.Bloqueado = false;
        }
        else
        {
            var indice = actuales.IndexOf(elegido!);
            var particulares = Particularizar(dia, actuales);
            particulares[indice].Activo = false;
        }
    }, ct);
}
