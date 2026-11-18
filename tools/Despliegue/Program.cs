using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Zuni.Data;
using Zuni.Seguridad;

internal static class Deploy
{
    public const string Baseline="20261111063900_PermitirAtencionProfesionalSinSolicitud";
    public const string Target="20261117015815_AgregarDobleFactorYAusencias";
    public const string Version="1.6.2";
    static async Task<int> Main(string[] args)
    {
        try
        {
            if(args.Length==0)throw new ArgumentException();
            switch(args[0])
            {
                case "sql-base-prueba":
                case "sql":
                    using(var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=sql_generation_only;Username=unused").Options,ProteccionPersistencia.Efimera()))
                    {
                        var sql=db.GetService<IMigrator>().GenerateScript(args[0]=="sql"?Baseline:"0",args[0]=="sql"?Target:Baseline,MigrationsSqlGenerationOptions.Idempotent|MigrationsSqlGenerationOptions.NoTransactions);
                        await File.WriteAllTextAsync(args[1],sql);
                    }
                    Console.WriteLine("SQL_GENERADO_SIN_CONEXION");break;
                case "validar-tar":ValidateArchive(args[1]);Console.WriteLine("ARCHIVO_VALIDADO");break;
                case "entorno":ValidateEnvironment();Console.WriteLine("ENTORNO_COMPATIBLE: conexión local, rutas persistentes y proxy; valores privados omitidos.");break;
                case "ensayo":await Probe(args[1],args[2],args[3]);break;
                case "autoprueba":SelfTest();break;
                default:throw new ArgumentException();
            }
            return 0;
        }
        catch(Exception e)
        {
            // Never print connection strings, inherited environment or child output.
            Console.Error.WriteLine("COMPROBACION_FALLIDA ("+e.GetType().Name+"). Consulta el registro privado del ensayo.");
            return 1;
        }
    }
    internal static string SafeName(string name)
    {
        if(name.StartsWith('/')||name.Contains('\\')||name.Contains(':')||name.Any(char.IsControl))throw new InvalidDataException();
        while(name.StartsWith("./",StringComparison.Ordinal))name=name[2..];
        name=name.TrimEnd('/');
        if(name.Length==0)return "";
        var parts=name.Split('/');
        if(parts.Any(p=>p is ".." or "." or ""||p.EndsWith(' ')||p.EndsWith('.')))throw new InvalidDataException();
        var blocked=new[]{"App_Data",".git",".local-security","secrets.json","claves.json","bitacora.jsonl","users.json"};
        if(parts.Any(p=>blocked.Contains(p,StringComparer.OrdinalIgnoreCase)||p.StartsWith(".env",StringComparison.OrdinalIgnoreCase)))throw new InvalidDataException();
        if((name!="appsettings.json"&&name.Contains("appsettings.",StringComparison.OrdinalIgnoreCase))||Regex.IsMatch(name,@"\.(pem|pfx|key|dump|db|sqlite|sqlite3)$",RegexOptions.IgnoreCase))throw new InvalidDataException();
        return name;
    }
    internal static void ValidateArchive(string path)
    {
        if(new FileInfo(path).Length>256L*1024*1024)throw new InvalidDataException();
        using var file=File.OpenRead(path);using var gzip=new GZipStream(file,CompressionMode.Decompress);using var tar=new TarReader(gzip);
        long total=0;int count=0;var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        TarEntry? entry;
        while((entry=tar.GetNextEntry()) is not null)
        {
            if(entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile or TarEntryType.Directory))throw new InvalidDataException();
            var name=SafeName(entry.Name);if(name==""&&entry.EntryType==TarEntryType.Directory)continue;
            if(!names.Add(name)||++count>20000||entry.Length>256L*1024*1024)throw new InvalidDataException();
            total=checked(total+entry.Length);if(total>1024L*1024*1024)throw new InvalidDataException();
        }
        if(!names.Contains("Zuni.dll")||!names.Contains("release-info.json")||!names.Contains("Zuni.Despliegue.dll"))throw new InvalidDataException();
    }
    static NpgsqlConnectionStringBuilder ValidateEnvironment()
    {
        if(!OperatingSystem.IsLinux())throw new PlatformNotSupportedException();
        var connection=new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));
        if(connection.Host!="127.0.0.1"||connection.Port!=5433||connection.Database!="zuni"||connection.Username!="zuni_app")throw new InvalidOperationException();
        var expected=new Dictionary<string,string>{{"DataProtection__KeysPath","/var/lib/zuni/demo/dataprotection"},{"SeguridadClinica__ClavesPath","/var/lib/zuni/demo/seguridad/claves.json"},{"SeguridadClinica__BitacoraPath","/var/lib/zuni/demo/auditoria/bitacora.jsonl"}};
        foreach(var (key,value) in expected)if(Environment.GetEnvironmentVariable(key)?.TrimEnd('/')!=value)throw new InvalidOperationException();
        if(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!="Production"||Environment.GetEnvironmentVariable("ASPNETCORE_URLS")!="http://127.0.0.1:5101")throw new InvalidOperationException();
        if(Environment.GetEnvironmentVariable("Zuni__ProxyInverso")?.ToLowerInvariant()!="true"||!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Hosting__PathBase")))throw new InvalidOperationException();
        if(!File.Exists(expected["SeguridadClinica__ClavesPath"])||!Directory.Exists(expected["DataProtection__KeysPath"]))throw new InvalidOperationException();
        return connection;
    }
    static ProcessStartInfo Child(string release,string database,string state,params string[] arguments)
    {
        var connection=ValidateEnvironment();connection.Database=database;connection.ApplicationName="zuni-deploy-clone-162";
        if(!Regex.IsMatch(database,@"^zuni_verif_162_[0-9]{8}_[0-9]{6}_[0-9]+$"))throw new InvalidOperationException();
        if(state!="/var/lib/zuni/ensayos-demo/"+database||!Directory.Exists(state)||new DirectoryInfo(state).LinkTarget is not null)throw new InvalidOperationException();
        release=Path.GetFullPath(release);
        if(!release.StartsWith("/opt/zuni/demo/releases/",StringComparison.Ordinal)||new DirectoryInfo(release).LinkTarget is not null)throw new InvalidOperationException();
        var start=new ProcessStartInfo("/usr/bin/dotnet"){UseShellExecute=false,WorkingDirectory=release,RedirectStandardOutput=true,RedirectStandardError=true};
        start.ArgumentList.Add(Path.Combine(release,"Zuni.dll"));foreach(var arg in arguments)start.ArgumentList.Add(arg);
        start.Environment["ConnectionStrings__DefaultConnection"]=connection.ConnectionString;
        start.Environment["DataProtection__KeysPath"]=state+"/dataprotection";
        start.Environment["SeguridadClinica__ClavesPath"]=state+"/seguridad/claves.json";
        start.Environment["SeguridadClinica__BitacoraPath"]=state+"/auditoria/bitacora.jsonl";
        start.Environment["ASPNETCORE_ENVIRONMENT"]="Production";
        start.Environment["ASPNETCORE_URLS"]="http://127.0.0.1:15162";
        start.Environment["Correo__RemitenteVerificado"]="false";
        return start;
    }
    static async Task Probe(string release,string database,string state)
    {
        var routes=new[]{"/","/Cuenta/IniciarSesion","/deniscorp/zuni","/deniscorp/blog","/deniscorp/contacto","/css/deniscorp.css","/js/pin-profesional.js","/js/portal-denis-corp.js"};
        using(var verify=Process.Start(Child(release,database,state,"--verificar-seguridad"))!)
        {
            await Capture(verify,state+"/integridad.log",TimeSpan.FromSeconds(45));
            if(verify.ExitCode!=0)throw new InvalidOperationException();
        }
        using var app=Process.Start(Child(release,database,state))!;
        var stdout=app.StandardOutput.ReadToEndAsync();var stderr=app.StandardError.ReadToEndAsync();
        try
        {
            using var http=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(3)};
            bool ready=false;
            for(int i=0;i<45&&!app.HasExited;i++)
            {
                try{using var response=await http.SendAsync(Request("/"));ready=response.StatusCode==HttpStatusCode.OK;}catch(HttpRequestException){}catch(TaskCanceledException){}
                if(ready)break;await Task.Delay(1000);
            }
            if(!ready||app.HasExited)throw new InvalidOperationException();
            foreach(var route in routes)
            {
                using var response=await http.SendAsync(Request(route));
                if(response.StatusCode!=HttpStatusCode.OK)throw new InvalidOperationException();
                if(route=="/"&&!(await response.Content.ReadAsStringAsync()).Contains("Denis Corp",StringComparison.Ordinal))throw new InvalidOperationException();
                if(!response.Headers.Contains("X-Content-Type-Options"))throw new InvalidOperationException();
            }
            if(app.HasExited)throw new InvalidOperationException();
            Console.WriteLine("ENSAYO_CLON_OK: integridad, arranque .NET 10 y ocho rutas; sin tráfico público.");
        }
        finally
        {
            if(!app.HasExited)app.Kill(entireProcessTree:true);
            await app.WaitForExitAsync();
            await File.WriteAllTextAsync(state+"/http.log",await stdout+await stderr);
            if(OperatingSystem.IsLinux())File.SetUnixFileMode(state+"/http.log",UnixFileMode.UserRead|UnixFileMode.UserWrite);
        }
    }
    static HttpRequestMessage Request(string path)
    {
        var request=new HttpRequestMessage(HttpMethod.Get,"http://127.0.0.1:15162"+path);request.Headers.Host="zuni.lat";request.Headers.Add("X-Forwarded-Proto","https");return request;
    }
    static async Task Capture(Process process,string log,TimeSpan timeout)
    {
        var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
        try{await process.WaitForExitAsync(new CancellationTokenSource(timeout).Token);}
        finally{if(!process.HasExited){process.Kill(entireProcessTree:true);await process.WaitForExitAsync();}await File.WriteAllTextAsync(log,await output+await error);if(OperatingSystem.IsLinux())File.SetUnixFileMode(log,UnixFileMode.UserRead|UnixFileMode.UserWrite);}
    }
    static void SelfTest()
    {
        int checks=0;void Check(bool ok){if(!ok)throw new Exception("self test");checks++;}
        foreach(var name in new[]{"Zuni.dll","./wwwroot/css/deniscorp.css","wwwroot/images/zuni-logo-transparent.png","appsettings.json"})Check(SafeName(name).Length>0);
        foreach(var name in new[]{"../x","/etc/x","a/../x","a\\b","C:/x",".env","App_Data/keys/x.xml","secrets.json","x/claves.json","appsettings.Development.json","foo.pfx","a//b","a/./b","x\nx","a./b"})
        {try{SafeName(name);Check(false);}catch(InvalidDataException){checks++;}}
        var folder=Path.Combine(Path.GetTempPath(),"zuni-deploy-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            foreach(var bad in new[]{"none","link","duplicate","traversal","private","missing"})
            {
                var file=Path.Combine(folder,bad+".tar.gz");
                using(var output=File.Create(file))using(var zip=new GZipStream(output,CompressionMode.Compress))using(var tar=new TarWriter(zip,TarEntryFormat.Pax))
                {
                    foreach(var name in new[]{"Zuni.dll","release-info.json","Zuni.Despliegue.dll"})if(!(bad=="missing"&&name=="Zuni.dll"))tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile,name){DataStream=new MemoryStream([1,2,3])});
                    if(bad=="link")tar.WriteEntry(new PaxTarEntry(TarEntryType.SymbolicLink,"link"){LinkName="/etc/passwd"});
                    if(bad=="duplicate")tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile,"ZUNI.dll"){DataStream=new MemoryStream([1])});
                    if(bad=="traversal")tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile,"../escape"){DataStream=new MemoryStream([1])});
                    if(bad=="private")tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile,"App_Data/private.json"){DataStream=new MemoryStream([1])});
                }
                if(bad=="none"){ValidateArchive(file);checks++;}
                else{try{ValidateArchive(file);Check(false);}catch(InvalidDataException){checks++;}}
            }
        }
        finally
        {
            var root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
            if(Path.GetDirectoryName(Path.GetFullPath(folder))!=root||new DirectoryInfo(folder).LinkTarget is not null||!Path.GetFileName(folder).StartsWith("zuni-deploy-test-",StringComparison.Ordinal))throw new InvalidOperationException();
            Directory.Delete(folder,recursive:true);
        }
        Check(Zuni.Resources.VersionSistema.Actual==Version);
        Console.WriteLine("OK: "+checks+" comprobaciones de rutas, archivos privados, enlaces y duplicados.");
    }
}
