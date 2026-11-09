#requires -Version 5.1
<#
.SYNOPSIS
  Valida los invariantes de un tour de PRUEBA sin modificar datos.
.EXAMPLE
  $env:TOURSMART_TOKEN_ADMIN = "<JWT del administrador de pruebas>"
  .\scripts\Test-TourSmartIntegridad.ps1 -TourId 1
#>
param(
    [Parameter(Mandatory = $true)][int]$TourId,
    [string]$ApiBase = "http://localhost:5290"
)
$ErrorActionPreference = "Stop"
if ($TourId -le 0) { throw "TourId debe ser un entero positivo." }
if ([string]::IsNullOrWhiteSpace($env:TOURSMART_TOKEN_ADMIN)) {
    throw "Define TOURSMART_TOKEN_ADMIN con el JWT de un Administrador de pruebas."
}
$headers = @{ Authorization = "Bearer $env:TOURSMART_TOKEN_ADMIN" }
$base = $ApiBase.TrimEnd('/')
try {
    $manifiesto = Invoke-RestMethod -Uri "$base/api/tours/$TourId/manifiesto" -Headers $headers -TimeoutSec 12
    $estado = Invoke-RestMethod -Uri "$base/api/tours/$TourId/cierre-operativo" -Headers $headers -TimeoutSec 12
}
catch {
    throw "No fue posible consultar la API o el token no tiene permisos: $($_.Exception.Message)"
}
$fallos = [System.Collections.Generic.List[string]]::new()
$confirmadas = @($manifiesto.confirmadas)
$pendientes = @($manifiesto.pendientes)
$plazasConfirmadas = 0
$plazasPendientes = 0
$llegaron = 0
foreach ($reserva in $confirmadas) {
    $plazasConfirmadas += [int]$reserva.cantidadPersonas
    $asistentes = @($reserva.asistentes)
    if ($asistentes.Count -gt [int]$reserva.cantidadPersonas) {
        $fallos.Add("Reserva $($reserva.reservaId): más nombres que plazas.")
    }
    foreach ($persona in $asistentes) {
        if ($null -ne $persona.llegadaUtc -and "$($persona.llegadaUtc)" -ne "") { $llegaron++ }
    }
}
foreach ($reserva in $pendientes) { $plazasPendientes += [int]$reserva.cantidadPersonas }
if ($plazasConfirmadas -ne [int]$manifiesto.cuposConfirmados) {
    $fallos.Add("CuposConfirmados no coincide con el total de reservas confirmadas.")
}
if ($plazasPendientes -ne [int]$manifiesto.cuposPendientes) {
    $fallos.Add("CuposPendientes no coincide con el total de reservas pendientes.")
}
if ($llegaron -ne [int]$manifiesto.llegaron) {
    $fallos.Add("Llegaron no coincide con marcas individuales.")
}
if ([int]$manifiesto.faltan -ne ($plazasConfirmadas - $llegaron)) {
    $fallos.Add("Faltan no coincide con plazas confirmadas menos llegadas.")
}
if ($estado.cerrado -eq $true) {
    if ($null -eq $estado.cierre) {
        $fallos.Add("El cierre está marcado sin detalle.")
    }
    else {
        $cierre = $estado.cierre
        if ([int]$cierre.plazasConfirmadas -ne [int]$cierre.pasajerosPresentes + [int]$cierre.pasajerosAusentes) {
            $fallos.Add("El cierre no cumple confirmados = presentes + ausentes.")
        }
        if ([int]$cierre.plazasConfirmadas -ne $plazasConfirmadas -or
            [int]$cierre.pasajerosPresentes -ne $llegaron) {
            $fallos.Add("El manifiesto actual no coincide con la instantánea cerrada.")
        }
    }
}
Write-Host "Tour $TourId -> confirmadas: $plazasConfirmadas; pendientes: $plazasPendientes; presentes: $llegaron; cerrado: $($estado.cerrado)"
if ($fallos.Count -gt 0) {
    foreach ($fallo in $fallos) { Write-Host "[ERROR] $fallo" -ForegroundColor Red }
    exit 1
}
Write-Host "[OK] Integridad básica de asistencia y cierre verificada; no se modificaron datos." -ForegroundColor Green
exit 0
