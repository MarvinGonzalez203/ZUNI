using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Zuni.Seguridad;

// Claves independientes para confidencialidad e integridad; nunca se almacenan en PostgreSQL.
public sealed class ProteccionPersistencia
{
    public sealed record Llave(string Cifrado, string Firma);
    public sealed record Anillo(string Activa, Dictionary<string, Llave> Llaves);
    private readonly Anillo anillo;
    public string Instalacion { get; }
    public ProteccionPersistencia(Anillo anillo, string instalacion)
    {
        if (!anillo.Llaves.ContainsKey(anillo.Activa) || anillo.Llaves.Any(k =>
            !System.Text.RegularExpressions.Regex.IsMatch(k.Key,"^[a-zA-Z0-9-]{1,40}$") ||
            Convert.FromBase64String(k.Value.Cifrado).Length != 32 || Convert.FromBase64String(k.Value.Firma).Length != 32))
            throw new InvalidOperationException("El anillo de claves de seguridad no es válido.");
        this.anillo = anillo; Instalacion = instalacion;
    }
    public static ProteccionPersistencia Efimera(string instalacion = "pruebas") => new(NuevoAnillo(), instalacion);
    private static Anillo NuevoAnillo() => new("k1", new() { ["k1"] = new(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))) });
    public static void Inicializar(string archivo)
    {
        PrepararDirectorio(Path.GetDirectoryName(archivo)!);
        // CreateNew evita sustituir una clave necesaria para descifrar datos ya existentes.
        using (var f = new FileStream(archivo, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { JsonSerializer.Serialize(f, NuevoAnillo()); f.Flush(true); }
        RestringirArchivo(archivo);
    }
    public static void PrepararDirectorio(string carpeta)
    {
        Directory.CreateDirectory(carpeta);
        if(OperatingSystem.IsWindows()){
            var acl=new DirectorySecurity();acl.SetAccessRuleProtection(true,false);
            acl.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!,FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
            new DirectoryInfo(carpeta).SetAccessControl(acl);
        }
        else File.SetUnixFileMode(carpeta,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
    }
    public static void RestringirArchivo(string archivo)
    {
        if (OperatingSystem.IsWindows())
        {
            var acl = new FileSecurity(); acl.SetAccessRuleProtection(true,false);
            acl.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!,FileSystemRights.FullControl,AccessControlType.Allow));
            new FileInfo(archivo).SetAccessControl(acl);
        }
        else File.SetUnixFileMode(archivo, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
    public static ProteccionPersistencia Cargar(string archivo, string instalacion) => new(
        JsonSerializer.Deserialize<Anillo>(File.ReadAllText(archivo)) ?? throw new InvalidOperationException("Faltan claves de seguridad."),instalacion);
    public string Cifrar(string texto, string campo)
    {
        var nonce = RandomNumberGenerator.GetBytes(12); var claro = Encoding.UTF8.GetBytes(texto);
        var cifrado = new byte[claro.Length]; var tag = new byte[16];
        using var aes = new AesGcm(Convert.FromBase64String(anillo.Llaves[anillo.Activa].Cifrado),16);
        try { aes.Encrypt(nonce,claro,cifrado,tag,Encoding.UTF8.GetBytes(Instalacion+":"+campo)); }
        finally { CryptographicOperations.ZeroMemory(claro); }
        return $"zenc1:{anillo.Activa}:{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(tag)}:{Convert.ToBase64String(cifrado)}";
    }
    public string Descifrar(string valor, string campo)
    {
        // No aceptar silenciosamente texto plano o sobres dañados después de la migración.
        try
        {
            var p=valor.Split(':');
            if(p.Length!=5 || p[0]!="zenc1" || !anillo.Llaves.TryGetValue(p[1],out var llave))throw new CryptographicException();
            var cifrado=Convert.FromBase64String(p[4]); var claro=new byte[cifrado.Length];
            using var aes=new AesGcm(Convert.FromBase64String(llave.Cifrado),16);
            try { aes.Decrypt(Convert.FromBase64String(p[2]),cifrado,Convert.FromBase64String(p[3]),claro,Encoding.UTF8.GetBytes(Instalacion+":"+campo)); return Encoding.UTF8.GetString(claro); }
            finally { CryptographicOperations.ZeroMemory(claro); }
        }
        catch(Exception e) when(e is CryptographicException or FormatException or ArgumentException)
        { throw new CryptographicException("No se pudo verificar un dato protegido. No se devolverá contenido parcial."); }
    }
    public string Firmar(string contenido) => anillo.Activa+":"+Convert.ToBase64String(HMACSHA256.HashData(
        Convert.FromBase64String(anillo.Llaves[anillo.Activa].Firma),Encoding.UTF8.GetBytes(Instalacion+":"+contenido)));
    public bool Verificar(string contenido,string firma)
    {
        try { var p=firma.Split(':');return p.Length==2 && anillo.Llaves.TryGetValue(p[0],out var k) &&
            CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(p[1]),HMACSHA256.HashData(Convert.FromBase64String(k.Firma),Encoding.UTF8.GetBytes(Instalacion+":"+contenido))); }
        catch(FormatException){return false;}
    }
}

public sealed record RutasSeguridad(string Claves, string Bitacora)
{
    public static RutasSeguridad Obtener(IConfiguration config,IHostEnvironment env)
    {
        var raiz=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ZUNI","seguridad",AislamientoInstalacion.Nombre(config));
        string Ruta(string nombre,string defecto)
        {
            var valor=config["SeguridadClinica:"+nombre];
            if(string.IsNullOrWhiteSpace(valor) && !env.IsDevelopment())throw new InvalidOperationException("Configura SeguridadClinica:"+nombre+" fuera del directorio de publicación.");
            if(!string.IsNullOrWhiteSpace(valor)&&!Path.IsPathFullyQualified(valor))throw new InvalidOperationException("La ruta de seguridad debe ser absoluta.");
            var ruta=Path.GetFullPath(valor??defecto);
            var relativa=Path.GetRelativePath(env.ContentRootPath,ruta);
            if(!Path.IsPathRooted(relativa) && !relativa.StartsWith(".."+Path.DirectorySeparatorChar))throw new InvalidOperationException("Las claves y bitácoras deben estar fuera de la aplicación.");
            return ruta;
        }
        var claves=Ruta("ClavesPath",Path.Combine(raiz,"claves.json"));var bitacora=Ruta("BitacoraPath",Path.Combine(raiz,"bitacora.jsonl"));
        if(string.Equals(claves,bitacora,OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal))throw new InvalidOperationException("Claves y bitácora deben ser archivos distintos.");
        return new(claves,bitacora);
    }
}
