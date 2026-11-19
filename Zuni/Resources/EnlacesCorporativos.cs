namespace Zuni.Resources;
public static class EnlacesCorporativos
{
 public static string Inicio(IConfiguration config) => Seguro(config["DenisCorp:HomeUrl"]) ?? "https://zuni.lat/";
 public static string? Producto(IConfiguration config,string id) => id=="pos"?null:Seguro(config["DenisCorp:ProductHomes:"+id]);
 private static string? Seguro(string? value)
 {
  if(string.IsNullOrWhiteSpace(value))return null;
  if(value.StartsWith('/')&&!value.StartsWith("//")&&!value.Contains('\\'))return value;
  if(!Uri.TryCreate(value,UriKind.Absolute,out var url)||url.UserInfo.Length>0)return null;
  return url.Scheme=="https"||url.Scheme=="http"&&url.IsLoopback?url.AbsoluteUri:null;
 }
}
