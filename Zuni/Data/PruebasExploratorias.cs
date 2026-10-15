using Microsoft.EntityFrameworkCore;
using Zuni.Models.Pruebas;
namespace Zuni.Data;
public static class PruebasExploratorias
{
    private static Guid Id(int tipo,int nivel,int numero)=>Guid.Parse($"eeeeeeee-{tipo:0000}-{nivel:0000}-0000-{numero:000000000000}");
    public static void Configurar(ModelBuilder b)
    {
        var nombres=new[]{"Cuestionario de Screening / Tamizaje de Tendencias Conductuales","Inventario de Tendencias de Personalidad y Estilo Afrontativo","Perfil Integral de Tendencias de Personalidad, Actitudes y Patrones Conductuales"};
        var subtitulos=new[]{"Tamizaje de Tendencias e Intereses Básico","Inventario de Estilos de Comportamiento y Gestión Emocional","Inventario Clínico/Psicopedagógico de Tendencias Globales"};
        var temas=new[]{"Interacción y comunicación","Organización y estilo de trabajo","Afrontamiento cotidiano","Decisiones y adaptación","Intereses y motivación académica"};
        string[][] items=[
            ["Me resulta cómodo iniciar una conversación con alguien que no conozco.","Disfruto compartir ideas en actividades de grupo.","Busco momentos a solas para ordenar mis ideas.","Expreso mis desacuerdos con respeto.","Pido aclaraciones cuando no entiendo lo que otra persona quiere decir.","Me resulta cómodo explicar mis ideas ante un grupo.","Escucho puntos de vista diferentes antes de responder.","Prefiero conversar individualmente cuando un tema es importante para mí.","Me tomo un tiempo para pensar antes de intervenir en una conversación.","Puedo indicar a los demás cuando necesito espacio personal.","Me interesa colaborar con personas que tienen experiencias diferentes a las mías."],
            ["Organizo mis actividades antes de comenzar.","Divido una tarea extensa en pasos más pequeños.","Prefiero trabajar con instrucciones claras.","Reviso lo que hice antes de dar una tarea por terminada.","Reservo tiempo para cumplir mis compromisos académicos.","Me resulta útil mantener una rutina de estudio.","Ajusto mis prioridades cuando se acumulan actividades.","Puedo sostener la atención en una tarea durante el tiempo que planifiqué.","Busco un ambiente de trabajo que me permita concentrarme.","Anoto los compromisos para recordar sus fechas.","Pido orientación cuando una tarea supera los recursos con los que cuento."],
            ["Reconozco cuando una actividad me está generando tensión.","Hago pausas para recuperar la concentración cuando lo necesito.","Busco apoyo de alguien de confianza ante una dificultad.","Identifico qué parte de una situación está bajo mi control.","Puedo expresar cómo me siento ante una situación exigente.","Me doy tiempo para recuperar la calma antes de reaccionar.","Distingo entre una preocupación y una acción que puedo realizar.","Mantengo actividades de descanso cuando tengo responsabilidades académicas.","Reviso mis expectativas cuando me siento sobrecargado.","Puedo decir que necesito ayuda sin sentir que debo resolver todo solo.","Utilizo estrategias que ya me han servido para afrontar una dificultad."],
            ["Comparo varias alternativas antes de tomar una decisión.","Puedo ajustar un plan cuando las circunstancias cambian.","Considero las consecuencias de mis decisiones.","Busco información cuando tengo dudas sobre una opción.","Puedo reconocer que una primera idea necesita cambios.","Me resulta posible actuar aunque no tenga toda la información disponible.","Aprendo de los resultados de una decisión anterior.","Consulto a otras personas sin dejar que decidan siempre por mí.","Identifico qué criterios son importantes para elegir entre varias opciones.","Puedo cambiar de estrategia cuando una forma de trabajar no da resultado.","Reviso un problema desde más de un punto de vista."],
            ["Identifico actividades académicas que despiertan mi interés.","Me motiva aprender habilidades que pueda utilizar en el futuro.","Relaciono mis estudios con metas que son importantes para mí.","Disfruto explorar temas nuevos aunque no sean obligatorios.","Reconozco qué tareas me resultan especialmente interesantes.","Busco oportunidades para poner en práctica lo que aprendo.","Puedo explicar por qué elegí mi área de estudio.","Mantengo curiosidad por distintas formas de ejercer una profesión.","Reviso mis metas académicas cuando conozco nuevas posibilidades.","Me interesa conocer actividades de trabajo antes de decidir si son para mí.","Encuentro motivos personales para continuar una tarea que requiere esfuerzo."]];
        var ps=new List<Prueba>();var ds=new List<DimensionPrueba>();var qs=new List<PreguntaPrueba>();var os=new List<OpcionPrueba>();
        for(int nivel=1;nivel<=3;nivel++) {
            var p=new Prueba{Id=Id(1,nivel,0),Revision=Id(5,nivel,nivel==3?1:0),Nivel=nivel,Nombre=nombres[nivel-1],Subtitulo=subtitulos[nivel-1],Activa=false,CreadaUtc=new DateTime(2026,10,14,0,0,0,DateTimeKind.Utc),
                Descripcion=$"Nivel {nivel}: {(nivel==1?15:nivel==2?35:54)} afirmaciones para explorar tendencias declaradas. Preguntas propuestas de ZUNI para revisión profesional; instrumento sin validación psicométrica ni baremos. No produce diagnósticos, banderas automáticas de riesgo ni decisiones académicas.",
                Instrucciones="No existen respuestas correctas ni incorrectas; responde según lo que mejor describa tu forma habitual de pensar, sentir o actuar. Escala: 1 = totalmente en desacuerdo / nada identificado; 10 = totalmente de acuerdo / totalmente identificado. Los valores intermedios expresan grados de identificación. Puedes guardar un avance o retirar tu participación. El psicólogo debe explicarte la consigna antes de habilitar el cuestionario.",Fuente="Propuesta original ZUNI de ítems exploratorios. Pendiente de revisión y validación profesional; no corresponde a un test estandarizado publicado."};ps.Add(p);
            var cantidad=nivel==1?3:nivel==2?7:11;int orden=0;int numero=0;
            for(int d=0;d<5;d++) {
                var dim=new DimensionPrueba{Id=Id(2,nivel,d+1),PruebaId=p.Id,Nombre=temas[d]};ds.Add(dim);
                for(int i=0;i<cantidad;i++) {
                    // Preserve stable IDs; omit the redundant adaptation item only in level 3.
                    numero++;
                    if(nivel==3 && d==3 && i==9)continue;
                    var q=new PreguntaPrueba{Id=Id(3,nivel,numero),PruebaId=p.Id,DimensionId=dim.Id,Orden=++orden,Texto=items[d][i]};qs.Add(q);
                    for(int v=1;v<=10;v++)os.Add(new(){Id=Id(4,nivel,(numero-1)*10+v),PreguntaPruebaId=q.Id,Orden=v,Valor=v,Texto=v==1?"1 · Totalmente en desacuerdo / Nada identificado":v==10?"10 · Totalmente de acuerdo / Totalmente identificado":v.ToString()});
                }
            }
        }
        // Sin navegaciones en el seed; la estructura se enlaza mediante sus claves estables.
        b.Entity<Prueba>().HasData(ps);b.Entity<DimensionPrueba>().HasData(ds);b.Entity<PreguntaPrueba>().HasData(qs);b.Entity<OpcionPrueba>().HasData(os);
    }
}
