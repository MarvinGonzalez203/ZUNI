using System.Security.Cryptography;
using System.Text;
using Zuni.Models.Evaluaciones;
namespace Zuni.Services;

// Fuente de los ítems y claves: https://ipip.ori.org/New_IPIP-50-item-scale.htm
// Adaptación propia al español: NO se presenta como validada en Guatemala.
// El factor IV original es estabilidad emocional; aquí se invierte a Neuroticismo.
public static class Ipip50
{
    public const string Version = "IPIP50-ES-ZUNI-1";
    public static readonly Guid EvaluacionId = Id("evaluation");
    public sealed record Item(int Numero, char Rasgo, bool Inversa, string Original, string Texto)
    {
        public Guid Id => Ipip50.Id("item-" + Numero);
    }
    public static Guid Id(string value) => new(SHA256.HashData(Encoding.UTF8.GetBytes(Version + ":" + value)).AsSpan(0, 16));
    public static IReadOnlyList<Item> Items { get; } = Array.AsReadOnly(new Item[]
    {
        new(1,'E',false,"Am the life of the party.","Soy el alma de la fiesta."),
        new(2,'A',true,"Feel little concern for others.","Me preocupo poco por los demás."),
        new(3,'C',false,"Am always prepared.","Siempre estoy preparado/a."),
        new(4,'N',false,"Get stressed out easily.","Me estreso con facilidad."),
        new(5,'O',false,"Have a rich vocabulary.","Tengo un vocabulario amplio."),
        new(6,'E',true,"Don't talk a lot.","No hablo mucho."),
        new(7,'A',false,"Am interested in people.","Me intereso por las personas."),
        new(8,'C',true,"Leave my belongings around.","Dejo mis pertenencias por cualquier lugar."),
        new(9,'N',true,"Am relaxed most of the time.","Estoy relajado/a la mayor parte del tiempo."),
        new(10,'O',true,"Have difficulty understanding abstract ideas.","Me cuesta comprender ideas abstractas."),
        new(11,'E',false,"Feel comfortable around people.","Me siento cómodo/a cuando estoy con otras personas."),
        new(12,'A',true,"Insult people.","Insulto a las personas."),
        new(13,'C',false,"Pay attention to details.","Presto atención a los detalles."),
        new(14,'N',false,"Worry about things.","Me preocupo por las cosas."),
        new(15,'O',false,"Have a vivid imagination.","Tengo una imaginación vívida."),
        new(16,'E',true,"Keep in the background.","Prefiero mantenerme en segundo plano."),
        new(17,'A',false,"Sympathize with others' feelings.","Comprendo los sentimientos de los demás."),
        new(18,'C',true,"Make a mess of things.","Desordeno las cosas."),
        new(19,'N',true,"Seldom feel blue.","Rara vez me siento desanimado/a."),
        new(20,'O',true,"Am not interested in abstract ideas.","No me interesan las ideas abstractas."),
        new(21,'E',false,"Start conversations.","Inicio conversaciones."),
        new(22,'A',true,"Am not interested in other people's problems.","No me interesan los problemas de otras personas."),
        new(23,'C',false,"Get chores done right away.","Hago mis tareas pendientes de inmediato."),
        new(24,'N',false,"Am easily disturbed.","Me altero con facilidad."),
        new(25,'O',false,"Have excellent ideas.","Tengo ideas excelentes."),
        new(26,'E',true,"Have little to say.","Tengo poco que decir."),
        new(27,'A',false,"Have a soft heart.","Soy una persona de buen corazón."),
        new(28,'C',true,"Often forget to put things back in their proper place.","A menudo olvido devolver las cosas a su lugar."),
        new(29,'N',false,"Get upset easily.","Me disgusto con facilidad."),
        new(30,'O',true,"Do not have a good imagination.","No tengo mucha imaginación."),
        new(31,'E',false,"Talk to a lot of different people at parties.","Hablo con muchas personas diferentes en las fiestas."),
        new(32,'A',true,"Am not really interested in others.","En realidad no me intereso por los demás."),
        new(33,'C',false,"Like order.","Me gusta el orden."),
        new(34,'N',false,"Change my mood a lot.","Mi estado de ánimo cambia mucho."),
        new(35,'O',false,"Am quick to understand things.","Comprendo las cosas con rapidez."),
        new(36,'E',true,"Don't like to draw attention to myself.","No me gusta llamar la atención sobre mí."),
        new(37,'A',false,"Take time out for others.","Dedico tiempo a los demás."),
        new(38,'C',true,"Shirk my duties.","Evito cumplir con mis obligaciones."),
        new(39,'N',false,"Have frequent mood swings.","Tengo cambios de ánimo frecuentes."),
        new(40,'O',false,"Use difficult words.","Uso palabras difíciles."),
        new(41,'E',false,"Don't mind being the center of attention.","No me molesta ser el centro de atención."),
        new(42,'A',false,"Feel others' emotions.","Percibo las emociones de los demás."),
        new(43,'C',false,"Follow a schedule.","Sigo un horario."),
        new(44,'N',false,"Get irritated easily.","Me irrito con facilidad."),
        new(45,'O',false,"Spend time reflecting on things.","Dedico tiempo a reflexionar sobre las cosas."),
        new(46,'E',true,"Am quiet around strangers.","Soy callado/a cuando estoy con desconocidos."),
        new(47,'A',false,"Make people feel at ease.","Hago que las personas se sientan a gusto."),
        new(48,'C',false,"Am exacting in my work.","Soy exigente con mi trabajo."),
        new(49,'N',false,"Often feel blue.","A menudo me siento desanimado/a."),
        new(50,'O',false,"Am full of ideas.","Se me ocurren muchas ideas.")
    });

    public static IReadOnlyList<Item> Orden(Guid asignacion) => Items
        .GroupBy(i => i.Rasgo)
        .Select(g => g.OrderBy(i => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(asignacion + ":" + i.Numero))), StringComparer.Ordinal).ToArray())
        .SelectMany(g => g.Select((item,index) => (item,index)))
        .OrderBy(x => x.index).ThenBy(x => "EACNO".IndexOf(x.item.Rasgo)).Select(x => x.item).ToArray();

    public static Dictionary<char,decimal> Calcular(IEnumerable<RespuestaEvaluacion> respuestas)
    {
        var values = respuestas.ToArray();
        if(values.Length != 50 || values.Select(r=>r.PreguntaId).Distinct().Count()!=50 ||
            values.Any(r=>r.Valor is <1 or >5 || !Items.Any(i=>i.Id==r.PreguntaId)))
            throw new EvaluacionOperacionException("Se requieren las 50 respuestas válidas para calcular el perfil.");
        var map=values.ToDictionary(r=>r.PreguntaId,r=>r.Valor);
        return Items.GroupBy(i=>i.Rasgo).ToDictionary(g=>g.Key,g=>g.Average(i=>(decimal)(i.Inversa ? 6-map[i.Id] : map[i.Id])));
    }

    public static IReadOnlyList<DimensionBigFive> Dimensiones(ParticipacionBigFive p) => new[]
    {
        new DimensionBigFive("Apertura / intelecto e imaginación",p.Apertura!.Value,"Preferencia por lo concreto","Interés por ideas e imaginación"),
        new DimensionBigFive("Responsabilidad",p.Responsabilidad!.Value,"Mayor espontaneidad","Mayor organización"),
        new DimensionBigFive("Extraversión",p.Extraversion!.Value,"Mayor reserva social","Mayor actividad social"),
        new DimensionBigFive("Amabilidad",p.Amabilidad!.Value,"Mayor distancia interpersonal","Mayor consideración interpersonal"),
        new DimensionBigFive("Neuroticismo / sensibilidad emocional",p.Neuroticismo!.Value,"Mayor calma emocional","Mayor reactividad emocional")
    };
}
