using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Evaluaciones;
namespace Zuni.Services;

public sealed class BigFiveService(ApplicationDbContext db, IConfiguration config, IWebHostEnvironment environment,
    AsignacionPsicologoService asignaciones)
{
    public const string ConsentimientoVersion = "ZUNI-BIGFIVE-1";
    public bool PruebaLocal => environment.IsDevelopment() && config.GetValue<bool>("BigFive:PruebaLocal") &&
        new[]{"localhost","127.0.0.1","::1"}.Contains(new Npgsql.NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()).Host);
    public bool Disponible => PruebaLocal || (config.GetValue<bool>("BigFive:Habilitado") &&
        config.GetValue<bool>("BigFive:RevisionProfesionalAprobada") &&
        new[]{"Institucion","ContactoPrivacidad","Conservacion","UbicacionDatos"}.All(k=>!string.IsNullOrWhiteSpace(config["BigFive:"+k])));

    private string Politica(string key,string fallback) => string.IsNullOrWhiteSpace(config["BigFive:"+key]) ? fallback : config["BigFive:"+key]!;

    public string TextoConsentimiento =>
        "Esto no es un diagnóstico médico, es para conocerte mejor. " +
        "Este cuestionario describe cinco rasgos mediante 50 afirmaciones y no evalúa enfermedades ni riesgo de suicidio. " +
        "No existen respuestas buenas o malas. Responde según cómo eres habitualmente. " +
        "Se guardan tus respuestas, avance, motivo de consulta y cinco puntuaciones asociados a tu cuenta en PostgreSQL. " +
        "El psicólogo asignado recibe únicamente el resumen de cinco dimensiones, la fecha, el motivo y la referencia. " +
        "Administrador, Director y docentes no reciben este perfil desde sus paneles. " +
        "La administración técnica de la base requiere controles institucionales adicionales de confidencialidad. " +
        "Puedes decidir no participar, detenerte y retirar este consentimiento desde esta pantalla sin sanciones académicas. " +
        "Retirarlo suspende el cuestionario y el acceso del profesional; no elimina automáticamente datos ni registros de acceso. " +
        "Esta autorización corresponde al cuestionario, no sustituye un consentimiento terapéutico ni renuncia a derechos o quejas. " +
        "Respetamos tus creencias, cultura, identidad y orientación. Este flujo está dirigido a mayores de 18 años. " +
        (PruebaLocal ? "PRUEBA LOCAL: usa solo información ficticia. La adaptación española y la política institucional están pendientes de aprobación. " : "") +
        $"Responsable: {Politica("Institucion", "pendiente de definir (prueba local)")}. " +
        $"Ubicación del almacenamiento: {Politica("UbicacionDatos", "entorno local de desarrollo")}. " +
        $"Conservación: {Politica("Conservacion", "pendiente de definir; no usar datos reales")}. " +
        $"Privacidad, retiro de datos y quejas: {Politica("ContactoPrivacidad", "pendiente de definir")}.";

    public IQueryable<ParticipacionBigFive> Vigentes() => db.Set<ParticipacionBigFive>()
        .Where(p=>p.RevocadoUtc==null && p.PruebaLocal==PruebaLocal && p.VersionInstrumento==Ipip50.Version &&
            p.Asignacion.EvaluacionId==Ipip50.EvaluacionId && p.Asignacion.Estudiante.IsActive &&
            db.UserRoles.Any(ur=>ur.UserId==p.Asignacion.EstudianteId && db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="ESTUDIANTE")) &&
            p.Psicologo.IsActive && db.UserRoles.Any(ur=>ur.UserId==p.PsicologoId && db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="PSICOLOGO")) &&
            db.AsignacionesEstudiantePsicologo.Any(v=>v.PerfilEstudiante.UsuarioId==p.Asignacion.EstudianteId &&
                v.PerfilEstudiante.Activo && v.PsicologoUsuarioId==p.PsicologoId && v.FechaFinalizacionUtc==null));

    public Task<ParticipacionBigFive?> Propia(string student,CancellationToken ct) => db.Set<ParticipacionBigFive>()
        .AsNoTracking().Include(p=>p.Asignacion).SingleOrDefaultAsync(p=>p.Asignacion.EstudianteId==student && p.VersionInstrumento==Ipip50.Version,ct);

    public async Task<bool> PuedeSolicitarCita(string student,CancellationToken ct) => Disponible &&
        await Vigentes().AnyAsync(p=>p.Asignacion.EstudianteId==student && p.Asignacion.Estado==EstadoEvaluacion.Finalizada && p.Apertura!=null,ct);

    public async Task<string?> NombreProfesional(string student,CancellationToken ct)
    {
        var linked=await db.AsignacionesEstudiantePsicologo.AsNoTracking().Where(v=>v.PerfilEstudiante.UsuarioId==student &&
            v.FechaFinalizacionUtc==null && v.PerfilEstudiante.Activo && v.PsicologoUsuario.IsActive &&
            db.UserRoles.Any(ur=>ur.UserId==v.PsicologoUsuarioId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="PSICOLOGO")))
            .Select(v=>v.PsicologoUsuario.FullName).SingleOrDefaultAsync(ct);
        if(linked is not null)return linked;
        var names=await db.Users.AsNoTracking().Where(u=>u.IsActive && db.UserRoles.Any(ur=>ur.UserId==u.Id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="PSICOLOGO")))
            .Select(u=>u.FullName).Take(2).ToArrayAsync(ct);
        return names.Length==1 ? names[0] : null;
    }

    public async Task<Guid> Aceptar(string student,ConsentimientoBigFiveInput input,CancellationToken ct)
    {
        if(!Disponible)throw new EvaluacionOperacionException("El cuestionario aún no está habilitado por la institución.");
        if(!input.Acepto || !input.MayorDeEdad || string.IsNullOrWhiteSpace(input.MotivoConsulta) || input.MotivoConsulta.Trim().Length is <5 or >1500 ||
            !new[]{"Ninguna","Docente","Estudiante","Coordinacion","Otra"}.Contains(input.Referencia))
            throw new EvaluacionOperacionException("Confirma el consentimiento, la mayoría de edad y el motivo de consulta.");
        var profile=await db.PerfilesEstudiante.AsNoTracking().SingleOrDefaultAsync(p=>p.UsuarioId==student && p.Activo,ct);
        if(profile is null)throw new EvaluacionOperacionException("Completa tu perfil antes de continuar.");
        // El servicio existente administra su propia transacción y necesita un contexto limpio.
        var outcome=await asignaciones.AsignarAsync(profile.Id,ct);
        if(outcome is not (ResultadoAsignacionPsicologo.Asignado or ResultadoAsignacionPsicologo.YaAsignado))
            throw new EvaluacionOperacionException("Aún no hay un psicólogo asignado disponible. La institución debe revisar la asignación antes del cuestionario.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        // Serializa altas del mismo estudiante y también evita duplicar el catálogo compartido.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({803501501L})",ct);
        var existing=await Propia(student,ct);
        if(existing!=null)
        {
            if(existing.PruebaLocal!=PruebaLocal)throw new EvaluacionOperacionException("Esta participación pertenece al entorno de prueba. No se reutiliza como evaluación institucional.");
            if(existing.RevocadoUtc!=null)throw new EvaluacionOperacionException("El consentimiento fue retirado. Solicita orientación al profesional antes de una nueva aplicación.");
            return existing.AsignacionId;
        }
        var link=await db.AsignacionesEstudiantePsicologo.AsNoTracking().SingleOrDefaultAsync(v=>v.PerfilEstudianteId==profile.Id && v.FechaFinalizacionUtc==null,ct);
        if(link is null || !await db.Users.AnyAsync(u=>u.Id==link.PsicologoUsuarioId && u.IsActive &&
            db.UserRoles.Any(ur=>ur.UserId==u.Id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="PSICOLOGO")),ct))
            throw new EvaluacionOperacionException("El profesional asignado no está disponible.");
        if(!await db.Set<Evaluacion>().AnyAsync(e=>e.Id==Ipip50.EvaluacionId,ct))
        {
            db.Add(new Evaluacion {Id=Ipip50.EvaluacionId,Codigo=Ipip50.Version,Titulo="Big Five · IPIP-50",Descripcion="Cinco dimensiones de personalidad, sin diagnóstico. Adaptación española ZUNI v1.",
                Instrucciones="Describe cómo eres habitualmente, no cómo te gustaría ser. Elige cuánto te describe cada afirmación, de 1 a 5.",
                Preguntas=Ipip50.Items.Select(i=>new PreguntaEvaluacion {Id=i.Id,EvaluacionId=Ipip50.EvaluacionId,Orden=i.Numero,Texto=i.Texto,Obligatoria=true}).ToArray()});
        }
        var now=DateTime.UtcNow;
        var assignment=new AsignacionEvaluacion {Id=Guid.NewGuid(),EvaluacionId=Ipip50.EvaluacionId,EstudianteId=student,Estado=EstadoEvaluacion.EnProceso,FechaAsignacionUtc=now,FechaInicioUtc=now};
        db.Add(new ParticipacionBigFive {AsignacionId=assignment.Id,Asignacion=assignment,PsicologoId=link.PsicologoUsuarioId,
            VersionInstrumento=Ipip50.Version,PruebaLocal=PruebaLocal,VersionConsentimiento=ConsentimientoVersion,TextoConsentimiento=TextoConsentimiento,ConsentimientoUtc=now,
            MotivoConsulta=input.MotivoConsulta.Trim(),Referencia=input.Referencia});
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        return assignment.Id;
    }

    public async Task Retirar(string student,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var assignment=await db.Set<AsignacionEvaluacion>().FromSqlInterpolated($"SELECT * FROM \"AsignacionesEvaluacion\" WHERE \"EstudianteId\"={student} AND \"EvaluacionId\"={Ipip50.EvaluacionId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if(assignment!=null)
        {
            var participation=await db.Set<ParticipacionBigFive>().SingleAsync(p=>p.AsignacionId==assignment.Id,ct);
            participation.RevocadoUtc??=DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<ResumenBigFive>> Resumenes(string psychologist,CancellationToken ct,string? student=null)
    {
        // Proyección: jamás enviar entidades, respuestas ni el banco de preguntas al navegador del psicólogo.
        var rows=await Vigentes().AsNoTracking().Where(p=>p.PsicologoId==psychologist && (student==null||p.Asignacion.EstudianteId==student) && p.Asignacion.Estado==EstadoEvaluacion.Finalizada && p.Apertura!=null)
            .OrderByDescending(p=>p.Asignacion.FechaFinalizacionUtc).Take(100)
            .Select(p=>new {p.AsignacionId,Nombre=p.Asignacion.Estudiante.FullName,p.Asignacion.FechaFinalizacionUtc,p.MotivoConsulta,p.Referencia,
                p.Apertura,p.Responsabilidad,p.Extraversion,p.Amabilidad,p.Neuroticismo}).ToListAsync(ct);
        db.AddRange(rows.Select(p=>new AccesoBigFive {AsignacionId=p.AsignacionId,PsicologoId=psychologist,FechaUtc=DateTime.UtcNow}));
        await db.SaveChangesAsync(ct);
        return rows.Select(p=>new ResumenBigFive(p.AsignacionId,p.Nombre,p.FechaFinalizacionUtc,p.MotivoConsulta,p.Referencia,
            Ipip50.Dimensiones(new ParticipacionBigFive{Apertura=p.Apertura,Responsabilidad=p.Responsabilidad,Extraversion=p.Extraversion,Amabilidad=p.Amabilidad,Neuroticismo=p.Neuroticismo}))).ToArray();
    }
}
