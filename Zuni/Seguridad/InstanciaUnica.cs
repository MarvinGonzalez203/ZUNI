using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
namespace Zuni.Seguridad;

// Guard against accidentally starting replicas while limits, keys and audit remain single-instance.
// This is NOT HA/fencing: deployment remains one process; a network-loss interval is not failover.
public sealed class InstanciaUnica(IConfiguration config,IHostApplicationLifetime lifetime,ILogger<InstanciaUnica> log):BackgroundService
{
    private NpgsqlConnection? connection;
    private volatile bool disponible;
    public bool Disponible=>disponible;
    public static long Clave(string database)=>BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes("Denis.Zuni.SingleWriter.v1:"+database)));
    public async Task Adquirir(CancellationToken ct=default)
    {
        if(connection is not null)throw new InvalidOperationException("La instancia ya solicitó su exclusión.");
        var options=new NpgsqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection")){Pooling=false,Timeout=5,CommandTimeout=5};
        var candidate=new NpgsqlConnection(options.ConnectionString);
        try
        {
            await candidate.OpenAsync(ct);
            await using var command=new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)",candidate);
            command.Parameters.AddWithValue("key",Clave(candidate.Database));
            if(await command.ExecuteScalarAsync(ct) is not true)throw new InvalidOperationException("Otra instancia de ZUNI usa esta base. No se inició una réplica.");
            connection=candidate;disponible=true;
        }
        catch{await candidate.DisposeAsync();throw;}
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2),stoppingToken);
                if(connection is null)throw new InvalidOperationException("Falta el guard de instancia.");
                await using var command=new NpgsqlCommand("SELECT 1",connection);
                await command.ExecuteScalarAsync(stoppingToken);
            }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch
            {
                disponible=false;
                log.LogCritical(new EventId(1418,"InstanciaPerdida"),"Se perdió el guard de instancia. Se detiene ZUNI; conexión y credenciales omitidas.");
                lifetime.StopApplication();break;
            }
        }
    }
    public override void Dispose(){disponible=false;connection?.Dispose();base.Dispose();}
}
