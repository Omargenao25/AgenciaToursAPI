#requires -Version 5.1
<#
.SYNOPSIS
  Comprobaciones de seguridad de solo lectura para TourSmart RD.
.DESCRIPTION
  Arrancar API y Web antes de ejecutar. No crea ni modifica datos.
  Los tokens son opcionales; definir variables de entorno si se quiere
  probar la matriz completa de roles, nunca pasar claves al repositorio.
#>
param(
    [string]$ApiBase = "http://localhost:5290",
    [string]$WebBase = "http://localhost:5291",
    [int]$TourId = 1,
    [int]$ReservaId = 1
)
$ErrorActionPreference = "Stop"
$fallos = 0
$ejecutadas = 0
function EstadoHttp([string]$Metodo, [string]$Url, [string]$Token = "") {
    $headers = @{}
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    try {
        $resp = Invoke-WebRequest -Method $Metodo -Uri $Url -Headers $headers -UseBasicParsing -MaximumRedirection 0 -TimeoutSec 12 -ErrorAction Stop
        return [int]$resp.StatusCode
    }
    catch {
        if ($_.Exception.Response) {
            return [int]$_.Exception.Response.StatusCode
        }
        throw "Error de conexión o TLS: $Url. $($_.Exception.Message)"
    }
}
function Comprobar([string]$Nombre, [string]$Metodo, [string]$Url, [int[]]$Esperados, [string]$Token = "") {
    $script:ejecutadas++
    try {
        $estado = EstadoHttp $Metodo $Url $Token
        if ($Esperados -contains $estado) {
            Write-Host "[OK] $Nombre -> HTTP $estado" -ForegroundColor Green
        }
        else {
            $script:fallos++
            Write-Host "[ERROR] $Nombre -> HTTP $estado (esperado: $($Esperados -join ', '))" -ForegroundColor Red
        }
    }
    catch {
        $script:fallos++
        Write-Host "[ERROR] $Nombre -> $($_.Exception.Message)" -ForegroundColor Red
    }
}
$api = $ApiBase.TrimEnd('/')
$web = $WebBase.TrimEnd('/')

Write-Host "TourSmart RD - controles HTTP de solo lectura"
Comprobar "Manifiesto anónimo" GET "$api/api/tours/$TourId/manifiesto" @(401)
Comprobar "Check-in anónimo (sin mutar)" GET "$api/api/tours/$TourId/check-in/pasajeros/1" @(401,405)
Comprobar "Cierre anónimo" GET "$api/api/tours/$TourId/cierre-operativo" @(401)
Comprobar "Historial anónimo" GET "$api/api/reportes/historial-excursiones" @(401)
Comprobar "Rendimiento anónimo" GET "$api/api/reportes/rendimiento-excursiones" @(401)
Comprobar "Finanzas anónimas" GET "$api/api/reportes/finanzas" @(401)
Comprobar "Cuentas por cobrar anónimo" GET "$api/api/reportes/cuentas-por-cobrar" @(401)
Comprobar "Lista de pasajeros anónima" GET "$api/api/reservas/$ReservaId/pasajeros" @(401)
Comprobar "Panel de historial anónimo" GET "$web/panel/reportes/historial-excursiones" @(302,401,403)
Comprobar "Panel de rendimiento anónimo" GET "$web/panel/reportes/rendimiento-excursiones" @(302,401,403)

if ($env:TOURSMART_TOKEN_CLIENTE) {
    $jwt = $env:TOURSMART_TOKEN_CLIENTE
    Comprobar "Cliente bloqueado del historial" GET "$api/api/reportes/historial-excursiones" @(403) $jwt
    Comprobar "Cliente bloqueado del rendimiento" GET "$api/api/reportes/rendimiento-excursiones" @(403) $jwt
    Comprobar "Cliente bloqueado del manifiesto" GET "$api/api/tours/$TourId/manifiesto" @(403) $jwt
    Comprobar "Cliente bloqueado del cierre" GET "$api/api/tours/$TourId/cierre-operativo" @(403) $jwt
}
else { Write-Host "[OMITIDO] Pruebas Cliente: definir TOURSMART_TOKEN_CLIENTE (JWT de prueba)." -ForegroundColor Yellow }

if ($env:TOURSMART_TOKEN_OPERADOR) {
    $jwt = $env:TOURSMART_TOKEN_OPERADOR
    Comprobar "Operador bloqueado del historial" GET "$api/api/reportes/historial-excursiones" @(403) $jwt
    Comprobar "Operador bloqueado del rendimiento" GET "$api/api/reportes/rendimiento-excursiones" @(403) $jwt
    Comprobar "Operador puede consultar manifiesto existente" GET "$api/api/tours/$TourId/manifiesto" @(200) $jwt
    Comprobar "Operador puede consultar cierre existente" GET "$api/api/tours/$TourId/cierre-operativo" @(200) $jwt
}
else { Write-Host "[OMITIDO] Pruebas Operador: definir TOURSMART_TOKEN_OPERADOR." -ForegroundColor Yellow }

if ($env:TOURSMART_TOKEN_ADMIN) {
    $jwt = $env:TOURSMART_TOKEN_ADMIN
    Comprobar "Administrador consulta historial" GET "$api/api/reportes/historial-excursiones" @(200) $jwt
    Comprobar "Administrador consulta rendimiento" GET "$api/api/reportes/rendimiento-excursiones" @(200) $jwt
    Comprobar "Administrador consulta manifiesto" GET "$api/api/tours/$TourId/manifiesto" @(200) $jwt
    Comprobar "Administrador consulta cierre" GET "$api/api/tours/$TourId/cierre-operativo" @(200) $jwt
}
else { Write-Host "[OMITIDO] Pruebas Administrador: definir TOURSMART_TOKEN_ADMIN." -ForegroundColor Yellow }

Write-Host "Total: $ejecutadas verificaciones; fallos: $fallos"
if ($fallos -gt 0) { exit 1 }
exit 0
