using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Zuni.Data;
using Zuni.Demo;

try
{
    if(args.Length==0||args.Contains("--help"))
    {
        Console.WriteLine("Uso: dotnet run --project tools/Evaluaciones.Demo -- preview|create|list|remove --database BASE --student-id ID --batch LOTE [--publisher-id ADMIN] [--manifest ARCHIVO] [--confirm-local]");
        Console.WriteLine("create/remove requieren --confirm-local. publisher-id se exige para preview/create. Solo localhost. Sin migraciones automáticas.");
        return;
    }
    var operation=args[0];if(!new[]{"preview","create","list","remove"}.Contains(operation))throw new InvalidOperationException("Operación desconocida.");
    var options=new Dictionary<string,string>();bool confirmed=false;
    for(int i=1;i<args.Length;i++)
    {
        if(args[i]=="--confirm-local"){confirmed=true;continue;}
        if(!new[]{"--database","--student-id","--batch","--publisher-id","--manifest"}.Contains(args[i])||i+1>=args.Length||args[i+1].StartsWith("--")||!options.TryAdd(args[i],args[++i]))throw new InvalidOperationException("Opciones inválidas o repetidas.");
    }
    string Required(string key)=>options.TryGetValue(key,out var value)&&!string.IsNullOrWhiteSpace(value)?value:throw new InvalidOperationException("Falta "+key);
    if((operation is "create" or "remove")&&!confirmed)throw new InvalidOperationException("La escritura requiere --confirm-local: confirma que la base es local y no compartida.");
    var candidate=DemoManifest.New(Required("--database"),Required("--student-id"),Required("--batch"));
    var path=options.GetValueOrDefault("--manifest",Path.Combine(".demo-data",candidate.Prefix+".json"));
    var manifest=File.Exists(path)?await DemoManifest.Load(path):candidate;
    if(manifest.Database!=candidate.Database||manifest.StudentId!=candidate.StudentId||manifest.Batch!=candidate.Batch)throw new InvalidOperationException("El manifiesto corresponde a otro destino/estudiante/lote.");
    if(operation is "list" or "remove" && !File.Exists(path))throw new InvalidOperationException("Se requiere el manifiesto original. No se infiere propiedad por el nombre de las evaluaciones.");
    var cfg=new ConfigurationBuilder().AddUserSecrets("df74818a-cd0f-4ce3-9d60-29cf450de409").AddEnvironmentVariables().Build();
    var connection=cfg.GetConnectionString("DefaultConnection");if(string.IsNullOrWhiteSpace(connection))throw new InvalidOperationException("Configura ConnectionStrings:DefaultConnection por User Secrets o entorno. No pases credenciales por argumentos.");
    await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options);
    var dataset=new DemoDataset(db,manifest);await dataset.ValidateSchema();
    if(operation is "preview" or "create")await dataset.ValidateAccounts(Required("--publisher-id"));
    if(operation=="preview")
    {
        for(int i=0;i<5;i++)Console.WriteLine($"{manifest.Code(i)} | {DemoDataset.Titles[i]} | {(i<3?((Zuni.Models.Evaluaciones.EstadoEvaluacion)i).ToString():"Finalizada")} | respuestas {(i==2?1:i>=3?3:0)}/3 | publicado {i==3}");
        Console.WriteLine("Vista previa del plan inicial. No reinicia datos existentes ni escribe registros. Manifiesto: "+path);
    }
    else
    {
        // Se guarda antes de la transacción: un corte tras el commit no pierde la prueba de propiedad.
        if(operation=="create"&&!File.Exists(path))await manifest.SaveNew(path);
        if(operation=="create")Console.WriteLine("Asignaciones creadas: "+await dataset.Create(Required("--publisher-id")));
        if(operation=="remove")Console.WriteLine("Asignaciones retiradas: "+await dataset.Remove()+". Se conserva el manifiesto y la auditoría histórica.");
        foreach(var row in await dataset.List())Console.WriteLine($"{row.Code} | {row.AssignmentId} | {row.State} | {row.Responses}/3 | publicado {row.Published}");
        Console.WriteLine("Manifiesto: "+path);
    }
}
catch(Exception e)
{
    // Las excepciones del proveedor podrían incluir datos privados: no mostrar mensajes de conexión ni stack traces.
    Console.Error.WriteLine(e is InvalidOperationException?e.Message:"Operación abortada ("+e.GetType().Name+"). Revisa conexión, esquema y permisos localmente; no se imprimen credenciales.");
    Environment.ExitCode=1;
}
