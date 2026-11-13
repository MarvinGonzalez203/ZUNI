using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Zuni.Resources;

public static class BusquedaGuias
{
    public static string Normalizar(string texto) => Regex.Replace(string.Concat(texto.ToLowerInvariant().Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)),@"\b(xq|q|oy)\b",m=>m.Value switch{"xq"=>"porque","q"=>"que",_=>"hoy"});
    public static string Sonido(string texto)
    {
        var s=Normalizar(texto).Replace("h","").Replace("ll","y").Replace("qu","k").Replace("v","b").Replace("z","s");
        s=Regex.Replace(s,"c([ei])","s$1");s=Regex.Replace(s,"g([ei])","j$1");return s.Replace('c','k');
    }
    private static readonly HashSet<string> Vacias=Normalizar("como que porque para por donde puedo quiero necesito el la los las de del un una a al en con mis mi me se y es hay ver saber hacer").Split(' ').ToHashSet();
    private static readonly HashSet<string> SonidosVacios=Vacias.Select(Sonido).ToHashSet();
    public static string[] Palabras(string texto)=>Regex.Matches(Normalizar(texto),"[a-z0-9]+").Select(m=>m.Value).Where(p=>p.Length>1&&!SonidosVacios.Contains(Sonido(p))).Distinct().Take(256).ToArray();
    public static bool Equivale(string a,string b)
    {
        a=Sonido(a);b=Sonido(b);if(a==b)return true;
        var limite=a.Length>=7?2:a.Length>=4?1:0;
        if(limite==0||Math.Abs(a.Length-b.Length)>limite)return false;
        var d=new int[a.Length+1,b.Length+1];for(var i=0;i<=a.Length;i++)d[i,0]=i;for(var j=0;j<=b.Length;j++)d[0,j]=j;
        for(var i=1;i<=a.Length;i++)for(var j=1;j<=b.Length;j++){
            d[i,j]=Math.Min(Math.Min(d[i-1,j]+1,d[i,j-1]+1),d[i-1,j-1]+(a[i-1]==b[j-1]?0:1));
            if(i>1&&j>1&&a[i-1]==b[j-2]&&a[i-2]==b[j-1])d[i,j]=Math.Min(d[i,j],d[i-2,j-2]+1);
        }
        return d[a.Length,b.Length]<=limite;
    }
    public static bool Coincide(IEnumerable<string> textos,string consulta)
    {
        var terms=Palabras(consulta);if(terms.Length==0)return true;
        var palabras=Palabras(string.Join(' ',textos));
        return terms.All(q=>palabras.Any(t=>Equivale(q,t)||q.Length>=3&&t.StartsWith(q,StringComparison.Ordinal)));
    }
}
