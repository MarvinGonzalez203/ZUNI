param()
# Script simple: las opciones de dotnet (-o, -c, etc.) no son parámetros comunes de PowerShell.
$Argumentos=$args
$ErrorActionPreference='Stop'
$raiz=Split-Path $PSScriptRoot -Parent
$candidatos=@()
if($env:DOTNET10_ROOT){$candidatos+=Join-Path $env:DOTNET10_ROOT 'dotnet.exe'}
$pista=Join-Path $raiz '.verification-tools/dotnet.json'
if(Test-Path -LiteralPath $pista){$candidatos+=Join-Path ((Get-Content -LiteralPath $pista -Raw|ConvertFrom-Json).root) 'dotnet.exe'}
$candidatos+=Join-Path $raiz '.verification-sdk/dotnet10/dotnet.exe'
$candidatos+=Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'
$comando=Get-Command dotnet.exe -ErrorAction SilentlyContinue
if($comando){$candidatos+=$comando.Source}
foreach($candidato in $candidatos){
    if(!(Test-Path -LiteralPath $candidato)){continue}
    $sdks=& $candidato --list-sdks
    if($sdks -match '^10\.0\.'){
        $env:DOTNET_ROOT=Split-Path $candidato -Parent
        $env:PATH=$env:DOTNET_ROOT+[IO.Path]::PathSeparator+$env:PATH
        & $candidato @Argumentos
        exit $LASTEXITCODE
    }
}
throw 'Instala el SDK .NET 10 LTS o configura DOTNET10_ROOT a su carpeta. No se utilizará .NET 8/9.'
