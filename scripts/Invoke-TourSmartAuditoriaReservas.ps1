#requires -Version 5.1
<#
.SYNOPSIS
  Ejecuta la auditoria de reservas iniciando sesion de forma interactiva.
.DESCRIPTION
  Solicita correo y contrasena en consola, obtiene JWT desde api/Auth/login
  y lo utiliza solo durante la prueba. No imprime ni guarda credenciales.
  Solo admite API local; usar exclusivamente usuarios de pruebas.
.EXAMPLE
  .\scripts\Invoke-TourSmartAuditoriaReservas.ps1
#>
param([string]$ApiBase = "http://localhost:5290")
$ErrorActionPreference = "Stop"

$uri = $null
if (-not [Uri]::TryCreate($ApiBase, [UriKind]::Absolute, [ref]$uri) -or
    $uri.Host -notin @("localhost", "127.0.0.1") -or
    $uri.Scheme -notin @("http", "https") -or
    $uri.AbsolutePath -ne "/" -or
    -not [string]::IsNullOrEmpty($uri.Query) -or
    -not [string]::IsNullOrEmpty($uri.Fragment)) {
    Write-Host "[BLOQUEADO] La API debe estar en localhost o 127.0.0.1." -ForegroundColor Red
    exit 2
}
if (-not (Test-Path (Join-Path $PSScriptRoot "Test-TourSmartReservas.ps1"))) {
    Write-Host "[ERROR] No se encontro el script de auditoria de reservas." -ForegroundColor Red
    exit 1
}

$correo = Read-Host "Correo del Administrador de PRUEBAS"
if ([string]::IsNullOrWhiteSpace($correo)) {
    Write-Host "[BLOQUEADO] Escribe un correo valido." -ForegroundColor Yellow
    exit 2
}
$contrasenaSegura = Read-Host "Contrasena (oculta)" -AsSecureString
$bstr = [IntPtr]::Zero
$contrasena = $null
$jwtPrevio = $env:TOURSMART_TOKEN_ADMIN
$codigo = 1

try {
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($contrasenaSegura)
    $contrasena = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    # Windows PowerShell 5.1 may parse UTF-8 without BOM as ANSI.
    # Construct the JSON key using an ASCII-only script so the API still
    # receives the expected Unicode property name.
    $campoClave = "contrase" + [char]0x00F1 + "a"
    $datosLogin = @{ correo = $correo.Trim() }
    $datosLogin[$campoClave] = $contrasena
    $body = $datosLogin | ConvertTo-Json -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($body)
    $respuesta = Invoke-RestMethod -Method Post -Uri "$($ApiBase.TrimEnd('/'))/api/Auth/login" -Body $bytes -ContentType "application/json; charset=utf-8" -TimeoutSec 20
    $body = $null
    $bytes = $null
    $contrasena = $null
    $datosLogin = $null

    if ($respuesta.rol -ne "Administrador") {
        Write-Host "[BLOQUEADO] La auditoria requiere un usuario Administrador de pruebas." -ForegroundColor Yellow
        $codigo = 2
    }
    elseif ([string]::IsNullOrWhiteSpace([string]$respuesta.token)) {
        Write-Host "[ERROR] El inicio de sesion no devolvio JWT." -ForegroundColor Red
        $codigo = 1
    }
    else {
        Write-Host "[OK] Sesion de pruebas autenticada. Ejecutando auditoria de solo lectura..." -ForegroundColor Green
        $env:TOURSMART_TOKEN_ADMIN = [string]$respuesta.token
        & (Join-Path $PSScriptRoot "Test-TourSmartReservas.ps1") -ApiBase $ApiBase
        $codigo = $LASTEXITCODE
    }
}
catch {
    Write-Host "[ERROR] No se pudo autenticar o completar la auditoria. Comprueba que la API este disponible y las credenciales sean validas." -ForegroundColor Red
    $codigo = 1
}
finally {
    $contrasena = $null
    $datosLogin = $null
    $body = $null
    $bytes = $null
    if ($bstr -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
    if ($null -eq $jwtPrevio) {
        Remove-Item Env:TOURSMART_TOKEN_ADMIN -ErrorAction SilentlyContinue
    }
    else {
        $env:TOURSMART_TOKEN_ADMIN = $jwtPrevio
    }
    $respuesta = $null
}
exit $codigo
