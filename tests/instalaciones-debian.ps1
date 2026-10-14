param([Parameter(Mandatory)][string]$MigrationSql, [string]$Image='zuni-r08-check:20261014')
$ErrorActionPreference='Stop'
$prefix='zuni-check-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
$pg="$prefix-pg"; $network="$prefix-net"; $apps=@("$prefix-a","$prefix-b")
$pass=[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
$oldPassword=$env:POSTGRES_PASSWORD
$oldConnection=$env:ConnectionStrings__DefaultConnection; $oldPgPassword=$env:PGPASSWORD
$checks=0
function Assert($condition,$label) { if (!$condition) { throw "Prueba Debian: $label" }; $script:checks++ }
function Sql([string]$database,[string]$role,[string]$sql) {
    $result=$sql | docker exec -i $pg psql -X -A -t -v ON_ERROR_STOP=1 -U $role -d $database 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'Falló una consulta de la base ficticia.' }
    return ($result -join "`n").Trim()
}
try {
    docker network create $network | Out-Null
    $env:POSTGRES_PASSWORD=$pass
    docker run -d --name $pg --network $network -e POSTGRES_PASSWORD postgres:18-bookworm | Out-Null
    Assert ($LASTEXITCODE -eq 0) 'PostgreSQL iniciado sin puerto público'
    $ready=$false
    for($i=0;$i -lt 30;$i++) { docker exec $pg pg_isready -U postgres *> $null; if($LASTEXITCODE -eq 0){$ready=$true;break}; Start-Sleep -Seconds 1 }
    Assert $ready 'base disponible'
    foreach($suffix in @('a','b')) {
        Sql postgres postgres "CREATE ROLE inst_$suffix LOGIN PASSWORD '$pass';" | Out-Null
        Sql postgres postgres "CREATE DATABASE inst_$suffix OWNER inst_$suffix;" | Out-Null
        Sql postgres postgres "REVOKE ALL ON DATABASE inst_$suffix FROM PUBLIC; GRANT CONNECT ON DATABASE inst_$suffix TO inst_$suffix;" | Out-Null
        Get-Content -Raw -LiteralPath $MigrationSql | docker exec -i $pg psql -X -q -v ON_ERROR_STOP=1 -U "inst_$suffix" -d "inst_$suffix" *> $null
        Assert ($LASTEXITCODE -eq 0) "migraciones completas en institución $suffix"
        $brandingSql='INSERT INTO "ConfiguracionInstitucion" ("Id","Nombre","Lema","ColorPrincipal","PoliticaCorreo","DominiosAutorizados","Revision") VALUES (1,''Institucion {0}'',''Lema {0}'',''#cea2fd'',1,'''',''00000000-0000-0000-0000-000000000000'');' -f $suffix
        Sql "inst_$suffix" "inst_$suffix" $brandingSql | Out-Null
        $sqlFixture='CREATE TABLE prueba_aislamiento (marca text); INSERT INTO prueba_aislamiento VALUES (''institucion-'+$suffix+''');'
        Sql "inst_$suffix" "inst_$suffix" $sqlFixture | Out-Null
        $connection="Host=$pg;Database=inst_$suffix;Username=inst_$suffix;Password=$pass"
        $env:ConnectionStrings__DefaultConnection=$connection
        docker run -d --name "$prefix-$suffix" --network $network -e ConnectionStrings__DefaultConnection -e ASPNETCORE_ENVIRONMENT=Production -e "Zuni__InstallationId=colegio-$suffix" -e Zuni__ProxyInverso=true -e DataProtection__KeysPath=/tmp/keys $Image | Out-Null
        Assert ($LASTEXITCODE -eq 0) "aplicación $suffix iniciada"
    }
    $env:PGPASSWORD=$pass
    docker exec -e PGPASSWORD $pg psql -h 127.0.0.1 -U inst_a -d inst_b -c 'SELECT 1' *> $null
    Assert ($LASTEXITCODE -ne 0) 'credenciales A no acceden a base B'
    docker exec -e PGPASSWORD $pg psql -h 127.0.0.1 -U inst_b -d inst_a -c 'SELECT 1' *> $null
    Assert ($LASTEXITCODE -ne 0) 'credenciales B no acceden a base A'
    foreach($suffix in @('a','b')) {
        $response=''
        $request='exec 3<>/dev/tcp/127.0.0.1/8080; printf "GET /Cuenta/IniciarSesion HTTP/1.1\r\nHost: localhost\r\nX-Forwarded-Proto: https\r\nConnection: close\r\n\r\n" >&3; cat <&3'
        for($i=0;$i -lt 30;$i++) { $response=(docker exec "$prefix-$suffix" bash -c $request 2>$null) -join "`n"; if($response.Contains('HTTP/1.1 200')) { break }; Start-Sleep -Seconds 1 }
        Assert ($response.Contains('HTTP/1.1 200') -and $response.Contains("Institucion $suffix")) "institución $suffix visible en su propia aplicación"
        Assert ($response.Contains('X-Content-Type-Options: nosniff')) "cabeceras seguras $suffix"
        Assert ((docker exec "$prefix-$suffix" id -u) -ne '0') "aplicación $suffix sin root"
    }
    $keysA=(docker exec $apps[0] sh -c 'sha256sum /tmp/keys/*.xml').Split(' ')[0]
    $keysB=(docker exec $apps[1] sh -c 'sha256sum /tmp/keys/*.xml').Split(' ')[0]
    Assert ($keysA -ne $keysB) 'claves criptográficas separadas'
    docker restart $apps[0] | Out-Null
    Assert ((docker exec $apps[0] sh -c 'sha256sum /tmp/keys/*.xml').Split(' ')[0] -eq $keysA) 'claves conservadas tras reinicio'
    docker exec $pg pg_dump -U inst_a -d inst_a -Fc -f /tmp/inst-a.dump
    Assert ($LASTEXITCODE -eq 0) 'respaldo real generado'
    docker exec $pg pg_restore --list /tmp/inst-a.dump *> $null
    Assert ($LASTEXITCODE -eq 0) 'respaldo legible'
    Sql postgres postgres 'CREATE DATABASE zuni_restore_a OWNER inst_a;' | Out-Null
    docker exec $pg pg_restore --exit-on-error --no-owner --no-privileges -U inst_a -d zuni_restore_a /tmp/inst-a.dump
    Assert ($LASTEXITCODE -eq 0) 'restauración en base nueva'
    Assert ((Sql zuni_restore_a inst_a 'SELECT marca FROM prueba_aislamiento') -eq 'institucion-a') 'dato ficticio restaurado'
    Assert ((Sql inst_b inst_b 'SELECT marca FROM prueba_aislamiento') -eq 'institucion-b') 'restauración no altera institución B'
    Assert ((Sql zuni_restore_a inst_a 'SELECT count(*) FROM "__EFMigrationsHistory"') -eq (Sql inst_a inst_a 'SELECT count(*) FROM "__EFMigrationsHistory"')) 'historial completo restaurado'
    foreach($script in @('backup.sh','restore-test.sh')) {
        docker cp "$PSScriptRoot/../deploy/debian/$script" "${pg}:/tmp/$script"
        docker exec $pg bash -n "/tmp/$script"
        Assert ($LASTEXITCODE -eq 0) "sintaxis Debian $script"
    }
    docker exec -e PGPASSWORD $pg bash -c 'umask 077; printf "*:*:*:*:%s\n" "$PGPASSWORD" > /tmp/backup.pgpass'
    docker exec -e PGHOST=127.0.0.1 -e PGUSER=inst_a -e PGDATABASE=inst_a -e PGPASSFILE=/tmp/backup.pgpass $pg bash /tmp/backup.sh ensayo-a /tmp/backups *> $null
    Assert ($LASTEXITCODE -eq 0) 'script de respaldo funciona con credenciales protegidas'
    $scriptDump=(docker exec $pg find /tmp/backups -type f -name '*.dump').Trim()
    docker exec -e PGHOST=127.0.0.1 -e PGUSER=postgres -e PGPASSFILE=/tmp/backup.pgpass $pg bash /tmp/restore-test.sh $scriptDump zuni_restore_script_a *> $null
    Assert ($LASTEXITCODE -eq 0) 'script de restauración funciona sobre base nueva'
    Assert ((Sql zuni_restore_script_a postgres 'SELECT marca FROM prueba_aislamiento') -eq 'institucion-a') 'script conserva el dato restaurado'
    docker exec -e PGHOST=127.0.0.1 -e PGUSER=postgres -e PGPASSFILE=/tmp/backup.pgpass $pg bash /tmp/restore-test.sh $scriptDump zuni_restore_script_a *> $null
    Assert ($LASTEXITCODE -ne 0) 'script rechaza una base de destino existente'
    Assert ((Sql zuni_restore_script_a postgres 'SELECT marca FROM prueba_aislamiento') -eq 'institucion-a') 'rechazo no altera el destino existente'
    Write-Output "OK: $checks comprobaciones reales en Debian/PostgreSQL; dos instalaciones aisladas y respaldo restaurado."
} finally {
    $env:POSTGRES_PASSWORD=$oldPassword; $env:ConnectionStrings__DefaultConnection=$oldConnection; $env:PGPASSWORD=$oldPgPassword
    foreach($container in @($apps)+@($pg)) { docker rm -f -v $container *> $null }
    docker network rm $network *> $null
}
