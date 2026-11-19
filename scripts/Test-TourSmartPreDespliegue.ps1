#requires -Version 5.1
<#
.SYNOPSIS
  Inspecciona de forma no destructiva los requisitos de entrega y pre-despliegue.
.DESCRIPTION
  No conecta con SQL, no despliega y no lee ni imprime contrasenas.
  Los avisos son recomendaciones, no certificaciones de seguridad.
#>
param([switch]$Production)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$errores = 0
$avisos = 0
function OK([string]$m) { Write-Host "[OK] $m" -ForegroundColor Green }
function ErrorCheck([string]$m) { $script:errores++; Write-Host "[ERROR] $m" -ForegroundColor Red }
function Aviso([string]$m) { $script:avisos++; Write-Host "[AVISO] $m" -ForegroundColor Yellow }
function Existe([string]$ruta) { Test-Path -LiteralPath (Join-Path $raiz $ruta) -PathType Leaf }

Write-Host 'TourSmart RD - Revision previa a entrega y despliegue (solo lectura)'
foreach ($ruta in @('AgenciaToursAPI.sln', 'AgenciaToursAPI.csproj',
 'TourSmart.Web/TourSmart.Web.csproj', 'Migrations/ApplicationDbContextModelSnapshot.cs',
 'Services/RecordatoriosExcursionesService.cs', 'Controllers/ComprobantesTransferenciaController.cs',
 'TourSmart.Web/Views/TableroAdelantos/Index.cshtml',
 'docs/AUDITORIA_FINAL_520.md')) {
    if (Existe $ruta) { OK "Archivo requerido: $ruta" }
    else { ErrorCheck "Falta el archivo requerido: $ruta" }
}
try {
    $dotnet = & dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) { ErrorCheck 'El SDK de dotnet no responde.' }
    elseif ($dotnet -notmatch '^8\.') { Aviso "SDK detectado: $dotnet. Confirmar compatibilidad con .NET 8." }
    else { OK "SDK .NET $dotnet detectado" }
} catch { ErrorCheck 'No se encontro el comando dotnet.' }

# Solo comprobar si los directorios existen, no enumerar datos sensibles.
$imagenes = Join-Path $raiz 'TourSmart.Web/wwwroot/uploads'
if (Test-Path $imagenes) {
    Aviso 'Hay carpeta uploads: respaldar fotos de tours, destinos y portada antes de publicar o reemplazar la web.'
} else {
    Aviso 'No existe carpeta uploads local: comprobar si el entorno de demostracion tiene fotos persistidas.'
}

if (-not $Production) {
    Aviso 'Revision LOCAL: no confirma disponibilidad de SQL Server, SMTP, ni permisos en produccion.'
} else {
    # Solo inspecciona presencia y formato; no expone el contenido de variables.
    foreach ($nombre in @('ConnectionStrings__Conexion','Jwt__Key','Jwt__Issuer','Jwt__Audience',
       'Api__BaseUrl','ASPNETCORE_ENVIRONMENT')) {
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($nombre))) {
            ErrorCheck "Falta variable de ambiente: $nombre"
        } else { OK "Variable presente: $nombre" }
    }
    $clave = [Environment]::GetEnvironmentVariable('Jwt__Key')
    if ($clave -and [Text.Encoding]::UTF8.GetByteCount($clave) -lt 32) {
        ErrorCheck 'Jwt__Key tiene menos de 32 bytes.'
    }
    $url = [Environment]::GetEnvironmentVariable('Api__BaseUrl')
    if ($url) {
        $uri = $null
        if (-not [uri]::TryCreate($url, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -ne 'https') {
            ErrorCheck 'Api__BaseUrl debe usar HTTPS en produccion.'
        }
    }
    if ([Environment]::GetEnvironmentVariable('ASPNETCORE_ENVIRONMENT') -ne 'Production') {
        ErrorCheck 'ASPNETCORE_ENVIRONMENT debe ser Production.'
    }
    Aviso 'Comprobar certificados TLS validos, confianza SQL, secretos SMTP, permisos de archivos, backups y restauracion.'
    Aviso 'Antes de exponer Internet, se necesita revison de seguridad y pruebas con base dedicada.'
}
Write-Host "Resultado: $errores error(es), $avisos aviso(s)."
if ($errores -gt 0) { exit 1 }
exit 0
