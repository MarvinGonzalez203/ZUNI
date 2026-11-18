param(
    [string]$Commit='HEAD',
    [string]$OutputRoot='',
    [string]$Dotnet=''
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(!$OutputRoot){$OutputRoot=Join-Path $repo '.visual-check/despliegue-1.6.2'}
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
if(!$OutputRoot.StartsWith($repo+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'La salida debe quedar dentro del workspace, en una carpeta ignorada.'}
if(!$Dotnet){
    $candidates=@((Join-Path $repo '.verification-sdk/dotnet10/dotnet.exe'))
    $installed=Get-Command dotnet -ErrorAction SilentlyContinue
    if($installed){$candidates+=$installed.Source}
    foreach($candidate in $candidates){if(Test-Path -LiteralPath $candidate){$versions=& $candidate --list-sdks;if($versions -match '^10\.0\.'){ $Dotnet=$candidate;break }}}
}
if(!$Dotnet){throw 'Configura -Dotnet con el SDK .NET 10.'}
$sdk=& $Dotnet --list-sdks
if(!($sdk -match '^10\.0\.')){throw 'Se requiere SDK .NET 10.'}
$sha=(& git -C $repo rev-parse --verify --end-of-options "$Commit`^{commit}").Trim()
if($LASTEXITCODE -ne 0 -or $sha -notmatch '^[a-f0-9]{40}$'){throw 'Commit de origen inválido.'}
$tag=[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6)
$work=Join-Path $OutputRoot $tag
$source=Join-Path $work 'source'
$publish=Join-Path $work 'publish'
$tool=Join-Path $work 'tool-publish'
$delivery=Join-Path $work 'actualizacion-1.6.2'
foreach($directory in @($source,$publish,$tool,$delivery)){New-Item -ItemType Directory -Path $directory | Out-Null}
$archive=Join-Path $work 'source.tar'
& git -C $repo archive --format=tar "--output=$archive" $sha
if($LASTEXITCODE -ne 0){throw 'No se pudo crear el archivo Git.'}
& tar -xf $archive -C $source
if($LASTEXITCODE -ne 0){throw 'No se pudo extraer el origen.'}
$project=Join-Path $source 'Zuni/Zuni.csproj'
$toolProject=Join-Path $source 'tools/Despliegue/Zuni.Despliegue.csproj'
if(!(Test-Path -LiteralPath $toolProject)){throw 'El commit debe incluir las herramientas de despliegue. Confirma sus archivos antes de preparar.'}
[xml]$projectXml=Get-Content -LiteralPath $project -Raw
if($projectXml.Project.PropertyGroup.Version -ne '1.6.2'){throw 'El origen no es ZUNI 1.6.2.'}
$common=@('-c','Release','--no-self-contained','--ignore-failed-sources','-p:UseAppHost=false','-p:DebugType=None','-p:DebugSymbols=false','-p:NuGetAudit=false',"-p:SourceRevisionId=$sha",'-p:ContinuousIntegrationBuild=true')
& $Dotnet publish $project @common -o $publish *> (Join-Path $work 'publish.log')
if($LASTEXITCODE -ne 0){throw "Falló publish. Registro local: $work/publish.log"}
& $Dotnet publish $toolProject @common -o $tool *> (Join-Path $work 'tool-publish.log')
if($LASTEXITCODE -ne 0){throw "Falló el comprobador. Registro: $work/tool-publish.log"}
$checker=Join-Path $delivery 'comprobador'
New-Item -ItemType Directory -Path $checker | Out-Null
foreach($file in Get-ChildItem -LiteralPath $tool -Recurse -File){
    if($file.Extension -in @('.dll','.so','.dylib') -or $file.Name.EndsWith('.deps.json') -or $file.Name.EndsWith('.runtimeconfig.json')){
        $destination=Join-Path $checker ([IO.Path]::GetRelativePath($tool,$file.FullName))
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))|Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
    }
}
foreach($name in @('Zuni.Despliegue.dll','Zuni.Despliegue.deps.json','Zuni.Despliegue.runtimeconfig.json')){Copy-Item -LiteralPath (Join-Path $tool $name) -Destination $publish}
foreach($name in @('appsettings.Development.json','appsettings.Testing.json')){
    $file=Join-Path $publish $name
    if(Test-Path -LiteralPath $file){Remove-Item -LiteralPath $file}
}
if(Test-Path -LiteralPath (Join-Path $publish 'App_Data')){throw 'La publicación contiene App_Data.'}
$runtime=Get-Content -LiteralPath (Join-Path $publish 'Zuni.runtimeconfig.json') -Raw | ConvertFrom-Json
if($runtime.runtimeOptions.tfm -ne 'net10.0'){throw 'Runtime inesperado.'}
$info=[ordered]@{version='1.6.2';commit=$sha;framework='net10.0';deployment='framework-dependent-portable';buildUtc=[DateTime]::UtcNow.ToString('o');excludedLocalPrivacyChanges=$true;scope=@('ZUNI','Denis Corp portal');additionalApplicationsActivated=$false}
$utf8=New-Object Text.UTF8Encoding($false)
function Write-Utf8Lf([string]$path,[string]$content){[IO.File]::WriteAllText($path,($content -replace "`r`n","`n"),$utf8)}
Write-Utf8Lf (Join-Path $publish 'release-info.json') (($info|ConvertTo-Json -Depth 5)+"`n")
Write-Utf8Lf (Join-Path $delivery 'perfil.json') (([ordered]@{version='1.6.2';commit=$sha;os='Debian 13';service='zuni-demo';database='zuni';postgresPort=5433;postgresMajor=18;publicOrigin='https://zuni.lat';basePath='';preflightDefault=$true}|ConvertTo-Json)+"`n")
$versionedDeploy=Join-Path $source 'deploy/debian'
foreach($pair in @(@('actualizar-demo-1.6.2.sh','actualizar.sh'),@('revertir-demo-1.6.2.sh','revertir.sh'),@('permisos-1.6.2.sql','permisos-nuevos.sql'),@('LEEME-MOBA-1.6.2.md','LEEME-MOBA.md'))){
    Write-Utf8Lf (Join-Path $delivery $pair[1]) ([IO.File]::ReadAllText((Join-Path $versionedDeploy $pair[0])))
}
$clonePermissions=[IO.File]::ReadAllText((Join-Path $versionedDeploy 'permisos-runtime.sql.example')).Replace('zuni_institucion_a_app','zuni_app')
Write-Utf8Lf (Join-Path $delivery 'permisos-clon.sql') $clonePermissions
& $Dotnet (Join-Path $checker 'Zuni.Despliegue.dll') sql (Join-Path $delivery 'migraciones.sql')
if($LASTEXITCODE -ne 0){throw 'Falló la generación de SQL sin conexión.'}
& $Dotnet (Join-Path $checker 'Zuni.Despliegue.dll') autoprueba
if($LASTEXITCODE -ne 0){throw 'Fallaron las comprobaciones de seguridad del paquete.'}
$payload=Join-Path $delivery 'zuni-publicacion.tar.gz'
& tar -czf $payload -C $publish .
if($LASTEXITCODE -ne 0){throw 'No se pudo generar el tar.'}
& $Dotnet (Join-Path $checker 'Zuni.Despliegue.dll') validar-tar $payload
if($LASTEXITCODE -ne 0){throw 'La publicación contiene rutas o archivos no permitidos.'}
$hashes=foreach($file in Get-ChildItem -LiteralPath $delivery -Recurse -File | Sort-Object FullName){
    $relative=[IO.Path]::GetRelativePath($delivery,$file.FullName).Replace('\','/')
    if($relative -notmatch '^(comprobador/([a-zA-Z0-9][a-zA-Z0-9_.-]*/)*)?[a-zA-Z0-9][a-zA-Z0-9_.-]*$'){throw 'Ruta inesperada en entrega.'}
    (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$relative
}
Write-Utf8Lf (Join-Path $delivery 'SHA256SUMS') (($hashes -join "`n")+"`n")
$zip=Join-Path $work 'zuni-1.6.2-mobaxterm.zip'
Compress-Archive -LiteralPath $delivery -DestinationPath $zip -CompressionLevel Optimal
$result=[ordered]@{version='1.6.2';commit=$sha;directory=$delivery;zip=$zip;zipSha256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant();payloadSha256=(Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash.ToLowerInvariant();manifestSha256=(Get-FileHash -LiteralPath (Join-Path $delivery 'SHA256SUMS') -Algorithm SHA256).Hash.ToLowerInvariant();deployed=$false;buildLog=$work}
Write-Utf8Lf (Join-Path $work 'resultado.json') (($result|ConvertTo-Json)+"`n")
Write-Utf8Lf (Join-Path $OutputRoot 'ultimo-paquete.json') (($result|ConvertTo-Json)+"`n")
$result|ConvertTo-Json
