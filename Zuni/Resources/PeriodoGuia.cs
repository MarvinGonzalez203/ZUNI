using System.Globalization;
using System.Text.RegularExpressions;

namespace Zuni.Resources;

public sealed record PeriodoGuia(DateOnly Desde,DateOnly Hasta,string Consulta)
{
    private static readonly string[] Meses=["enero","febrero","marzo","abril","mayo","junio","julio","agosto","septiembre","octubre","noviembre","diciembre"];
    public static PeriodoGuia? Leer(string? texto,DateOnly hoy)
    {
        var q=BusquedaGuias.Normalizar(texto??"");
        var vocabulario=Meses.Concat(new[]{"hoy","ayer","anteayer","semana","pasada","pasado","ultimos","lunes","martes","miercoles","jueves","viernes","sabado","domingo"}).ToArray();
        q=Regex.Replace(q,@"\b[a-z]+\b",m=>vocabulario.FirstOrDefault(p=>BusquedaGuias.Sonido(p)==BusquedaGuias.Sonido(m.Value))??m.Value);
        PeriodoGuia R(DateOnly a,DateOnly b,string expresion)=>new(a,b,Regex.Replace(q,expresion," ").Trim());
        foreach(var (nombre,dias) in new[]{("anteayer",-2),("ayer",-1),("hoy",0)})if(Regex.IsMatch(q,$@"\b{nombre}\b"))return R(hoy.AddDays(dias),hoy.AddDays(dias),$@"\b{nombre}\b");
        var ultimos=Regex.Match(q,@"\bultimos?\s+(\d{1,3})\s+dias?\b");
        if(ultimos.Success&&int.TryParse(ultimos.Groups[1].Value,out var n)&&n is >0 and <=366)return R(hoy.AddDays(1-n),hoy,Regex.Escape(ultimos.Value));
        var semana=hoy.AddDays(-(((int)hoy.DayOfWeek+6)%7));
        foreach(var (nombre,a,b) in new[]{("esta semana",semana,semana.AddDays(6)),("la semana pasada",semana.AddDays(-7),semana.AddDays(-1)),("este mes",new DateOnly(hoy.Year,hoy.Month,1),new DateOnly(hoy.Year,hoy.Month,1).AddMonths(1).AddDays(-1)),("el mes pasado",new DateOnly(hoy.Year,hoy.Month,1).AddMonths(-1),new DateOnly(hoy.Year,hoy.Month,1).AddDays(-1)),("este ano",new DateOnly(hoy.Year,1,1),new DateOnly(hoy.Year,12,31)),("el ano pasado",new DateOnly(hoy.Year-1,1,1),new DateOnly(hoy.Year-1,12,31))})
            if(q.Contains(nombre,StringComparison.Ordinal))return R(a,b,Regex.Escape(nombre));
        var fecha=Regex.Match(q,@"\b(\d{1,2})/(\d{1,2})(?:/(\d{4}))?\b");
        if(fecha.Success&&DateOnly.TryParseExact(fecha.Groups[1].Value+"/"+fecha.Groups[2].Value+"/"+(fecha.Groups[3].Success?fecha.Groups[3].Value:hoy.Year.ToString()),"d/M/yyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out var f))return R(f,f,Regex.Escape(fecha.Value));
        for(var i=0;i<12;i++){
            var m=Regex.Match(q,$@"\b(?:(\d{{1,2}})\s+de\s+|en\s+){Meses[i]}(?:\s+(?:de\s+)?(\d{{4}}))?\b");
            if(!m.Success)continue;
            var year=m.Groups[2].Success?int.Parse(m.Groups[2].Value):hoy.Year;if(year is <1 or >9998)continue;
            var a=new DateOnly(year,i+1,1);
            if(m.Groups[1].Success){var d=int.Parse(m.Groups[1].Value);if(d<1||d>DateTime.DaysInMonth(year,i+1))continue;a=new DateOnly(year,i+1,d);return R(a,a,Regex.Escape(m.Value));}
            return R(a,a.AddMonths(1).AddDays(-1),Regex.Escape(m.Value));
        }
        var nombresDias=new[]{"domingo","lunes","martes","miercoles","jueves","viernes","sabado"};
        for(var i=0;i<7;i++)if(Regex.IsMatch(q,$@"\bel {nombresDias[i]}\b")){var a=hoy.AddDays(-(((int)hoy.DayOfWeek-i+7)%7));return R(a,a,$@"\bel {nombresDias[i]}\b");}
        return null;
    }
}
