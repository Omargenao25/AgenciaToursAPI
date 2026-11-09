#requires -Version 5.1
<#
.SYNOPSIS
  Audita todos los tours y las reservas del panel (solo lectura).
.DESCRIPTION
  Consulta GET /api/Tours (publico) y GET /api/Reservas (personal autorizado).
  Verifica cupos, estados y referencias sin mostrar datos personales.
.EXAMPLE
  .\scripts\Test-TourSmartReservas.ps1
#>
param([string]$ApiBase = "http://localhost:5290")
$ErrorActionPreference = "Stop"
$fallos = [System.Collections.Generic.List[string]]::new()
$advertencias = [System.Collections.Generic.List[string]]::new()
$base = $ApiBase.TrimEnd("/")
$token = $env:TOURSMART_TOKEN_ADMIN

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host "[BLOQUEADO] Configura TOURSMART_TOKEN_ADMIN con un JWT de Administrador de pruebas." -ForegroundColor Yellow
    exit 2
}

function ObtenerLista([string]$url, [hashtable]$headers) {
    $respuesta = Invoke-RestMethod -Method Get -Uri $url -Headers $headers -TimeoutSec 20
    if ($null -eq $respuesta) { return }
    foreach ($item in $respuesta) {
        if ($null -ne $item) { Write-Output $item }
    }
}
function AnotarError([string]$mensaje) { [void]$fallos.Add($mensaje) }
function AnotarAdvertencia([string]$mensaje) { [void]$advertencias.Add($mensaje) }

try {
    $tours = @(ObtenerLista "$base/api/Tours" @{})
    $reservas = @(ObtenerLista "$base/api/Reservas" @{ Authorization = "Bearer $token" })
}
catch {
    Write-Host "[ERROR] No se pudieron consultar los tours y reservas. Revisa que la API este activa y el JWT sea valido." -ForegroundColor Red
    exit 1
}

$toursPorId = @{}
$ocupacion = @{}
$estadosValidos = @("Pendiente", "Confirmada", "Cancelada")

foreach ($tour in $tours) {
    $id = [int]$tour.id
    if ($id -le 0 -or $toursPorId.ContainsKey($id)) {
        AnotarError "Tour con ID invalido o repetido: $id."
        continue
    }
    $toursPorId[$id] = $tour
    $ocupacion[$id] = 0
    if ([int]$tour.capacidadMaxima -le 0) {
        AnotarError "Tour $($id): capacidad maxima no positiva."
    }
    if ([int]$tour.cuposReservados -lt 0) {
        AnotarError "Tour $($id): cupos reservados negativos."
    }
}

$idsReservas = @{}
foreach ($reserva in $reservas) {
    $id = [int]$reserva.id
    $tourId = [int]$reserva.tourId
    $cantidad = [int]$reserva.cantidadPersonas
    $estado = [string]$reserva.estado

    if ($id -le 0 -or $idsReservas.ContainsKey($id)) {
        AnotarError "Reserva con ID invalido o repetido: $id."
    }
    $idsReservas[$id] = $true

    if (!$toursPorId.ContainsKey($tourId)) {
        AnotarError "Reserva $($id): tour $tourId inexistente."
        continue
    }
    if ($estado -notin $estadosValidos) {
        AnotarError "Reserva $($id): estado no reconocido."
    }
    if ($cantidad -lt 1 -or $cantidad -gt 100) {
        AnotarError "Reserva $($id): cantidad de personas fuera de rango."
    }
    if ([decimal]$reserva.total -lt 0) {
        AnotarError "Reserva $($id): total negativo."
    }
    if ($estado -ne "Cancelada") {
        $ocupacion[$tourId] = [int]$ocupacion[$tourId] + $cantidad
    }
}

foreach ($tour in $tours) {
    $id = [int]$tour.id
    if (!$ocupacion.ContainsKey($id)) { continue }
    $calculado = [int]$ocupacion[$id]
    $registrado = [int]$tour.cuposReservados
    $capacidad = [int]$tour.capacidadMaxima
    $libres = [int]$tour.cuposDisponibles

    if ($calculado -ne $registrado) {
        AnotarError "Tour $($id): ocupacion calculada $calculado pero la API muestra $registrado."
    }
    if ($calculado -gt $capacidad) {
        AnotarError "Tour $($id): sobreventa; $calculado plazas ocupadas sobre $capacidad."
    }
    if ($libres -ne [Math]::Max(0, $capacidad - $registrado)) {
        AnotarError "Tour $($id): cupos libres no coinciden con capacidad y ocupacion."
    }
    if ($tour.cerradoOperativo -eq $true -and $tour.estado -eq "Disponible") {
        AnotarAdvertencia "Tour $($id): cierre operativo registrado; revisar estado publico y restricciones de reserva."
    }
}

Write-Host "Auditoria de reservas: $($tours.Count) tours y $($reservas.Count) reservas (sin mostrar datos personales)."
foreach ($advertencia in $advertencias) {
    Write-Host "[AVISO] $advertencia" -ForegroundColor Yellow
}
if ($fallos.Count -gt 0) {
    foreach ($fallo in $fallos) { Write-Host "[ERROR] $fallo" -ForegroundColor Red }
    Write-Host "[FALLO] $($fallos.Count) incoherencias encontradas. No se modificaron datos." -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Estados, capacidades y plazas de reservas coherentes segun los GET de la API." -ForegroundColor Green
Write-Host "Solo lectura: no prueba pagos, pasajeros individuales ni condiciones bajo solicitudes concurrentes." -ForegroundColor Yellow
exit 0
