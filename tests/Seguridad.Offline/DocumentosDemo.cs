using Zuni.Models.Comercial;
using Zuni.Models.Informes;
using Zuni.Services.Informes;
using Zuni.Services.Institucion;

// Reproducible fictional documents. No database, credentials, case records or authorizations are loaded.
internal static class DocumentosDemo
{
    internal static void Crear(string carpeta)
    {
        Directory.CreateDirectory(carpeta);
        var fecha=new DateTime(2026,10,29,16,0,0,DateTimeKind.Utc);
        var centro=new CentroComercial{Id=Guid.Parse("00000000-0000-0000-0000-00000000df01"),Revision=Guid.Parse("00000000-0000-0000-0000-00000000df02"),Nombre="Colegio Huellitas (ejemplo ficticio)",CorreoContacto="administracion@huellitas.example",MesesPlan=12,Inicio=new(2026,10,28),Fin=new(2027,10,27),Estado=EstadoContrato.Activo,ImporteGtq=2030,EsDemostracion=true,NotaComercial="Ejemplo ficticio para demostrar el formato de impresión. El importe es ilustrativo y no representa una contratación ni un pago real. La infraestructura y la base de cada institución se administran de forma independiente."};
        File.WriteAllBytes(Path.Combine(carpeta,"contrato-huellitas-demostracion.pdf"),ContratoCentroPdf.Crear(centro,fecha));
        var informe=new VersionInformeEducativo{
            InformeId=Guid.Parse("00000000-0000-0000-0000-00000000de01"),Numero=1,Estado=EstadoInforme.Emitida,FechaEmisionUtc=fecha,
            Institucion="Colegio Huellitas - caso completamente ficticio",Estudiante="Ana Lucía Pérez (persona ficticia)",Carne="9999-26-0123",Psicologo="Lic. Andrea Reyes (profesional ficticia)",Destinatario="Lic. Sofía Mendoza - Dirección académica (persona ficticia)",
            PeriodoInicio=new(2026,10,1),PeriodoFin=new(2026,10,28),FechaSeguimiento=new(2026,11,12),
            Objetivo="Coordinar apoyos educativos para una estudiante ficticia mayor de edad que solicita ayuda para organizar entregas y preparar evaluaciones. Este ejemplo no evalúa a una persona real.",
            Fuentes="Entrevista de orientación simulada; calendario de tareas y registro de entregas ficticios, revisados con la estudiante. Se utiliza únicamente información pertinente para organizar el apoyo académico.",
            Observaciones="En el ejemplo, la estudiante comprende las consignas durante las actividades guiadas. Tres de ocho tareas se entregaron después de la fecha prevista. Se identifica la necesidad de ordenar prioridades y dividir las actividades extensas en pasos verificables.",
            IncluyeAnalisisRendimiento=true,ContrasteRendimientoConfirmado=true,
            RelatoRendimiento="La estudiante refiere que varias entregas coinciden en una misma semana y que inicia tarde las tareas cuando no ha definido cuál debe realizar primero.",
            ValoracionRendimiento="Al contrastar el relato con el calendario y las entregas simuladas, se observa coincidencia entre acumulación de actividades y retrasos. La información sugiere explorar apoyos de planificación; no permite atribuir el rendimiento a una causa única ni concluir falta de capacidad o motivación. No se incluyen detalles de la vida privada.",
            Necesidades="Un calendario semanal único, instrucciones por etapas y un referente académico que ayude a priorizar las entregas. Coordinar medidas viables sin etiquetar ni exponer a la estudiante ante su grupo.",
            Recomendaciones="1. Acordar con la estudiante un plan semanal de tres prioridades.\n2. Dividir tareas extensas en entregas parciales cuando la institución lo permita.\n3. Verificar una vez por semana el cumplimiento de acuerdos y ajustar la carga planificada.\n4. Ofrecer un espacio privado de seguimiento con el profesional, sin solicitar notas profesionales ni respuestas de cuestionarios.",
            ResponsablesAcciones="Estudiante: revisar el calendario y comunicar dificultades de cumplimiento. Referente académico: aclarar prioridades y fechas. Dirección: facilitar la coordinación de los apoyos acordados. Psicólogo: revisar el avance educativo y decidir los ajustes profesionales que correspondan."
        };
        File.WriteAllBytes(Path.Combine(carpeta,"informe-educativo-demostracion.pdf"),InformePdf.Crear(informe,demostracion:true));
        Console.WriteLine("Dos PDF ficticios generados sin consultar ni modificar una base de datos.");
    }
}
