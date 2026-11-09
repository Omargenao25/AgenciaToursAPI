#requires -Version 5.1
<#
.SYNOPSIS
  Auditoría de integridad geográfica de TourSmart RD sin modificar registros.
.DESCRIPTION
  Consulta GET públicos de países, destinos y tours. No envía credenciales ni peticiones de escritura.
  Comparaciones de nombres ignoran mayúsculas y acentos, como la prevalidación SQL propuesta.
.EXAMPLE
  .\scripts\Test-TourSmartGeografia.ps1
#>
param([string]$ApiBase = "http://localhost:5290")
$ErrorActionPreference = "Stop"
$errores = [System.Collections.Generic.List[string]]::new()
$culture = [System.Globalization.CultureInfo]::InvariantCulture
$compare = $culture.CompareInfo
$options = [System.Globalization.CompareOptions]::IgnoreCase -bor [System.Globalization.CompareOptions]::IgnoreNonSpace

function ClaveNombre([string]$nombre) {
    if ($null -eq $nombre) { return "" }
    $normalizado = $nombre.Trim().Normalize([Text.NormalizationForm]::FormD)
    $sb = [Text.StringBuilder]::new()
    foreach ($ch in $normalizado.ToCharArray()) {
        if ([Globalization.CharUnicodeInfo]::GetUnicodeCategory($ch) -ne
            [Globalization.UnicodeCategory]::NonSpacingMark) { [void]$sb.Append($ch) }
    }
    return $sb.ToString().Normalize([Text.NormalizationForm]::FormC).ToUpperInvariant()
}
function ErrorDeDatos([string]$mensaje) { [void]$errores.Add($mensaje) }
function ComprobarNombre($registro, [string]$tipo) {
    $nombre = [string]$registro.nombre
    if ($nombre.Trim().Length -lt 2 -or $nombre -cne $nombre.Trim()) {
        ErrorDeDatos "$tipo ID $($registro.id): nombre vacío, muy corto o con espacios sobrantes."
    }
}
$base = $ApiBase.TrimEnd('/')
try {
    $paises = @(Invoke-RestMethod -Method Get -Uri "$base/api/Paises" -TimeoutSec 15)
    $destinos = @(Invoke-RestMethod -Method Get -Uri "$base/api/Destinos" -TimeoutSec 15)
    $tours = @(Invoke-RestMethod -Method Get -Uri "$base/api/Tours" -TimeoutSec 15)
}
catch {
    Write-Host "[ERROR] No se pudo consultar la API en $base. $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
$paises = @($paises | Where-Object { $null -ne $_ })
$destinos = @($destinos | Where-Object { $null -ne $_ })
$tours = @($tours | Where-Object { $null -ne $_ })
$paisPorId = @{}
$destinoPorId = @{}
$nombresPaises = @{}
$nombresDestinos = @{}
foreach ($pais in $paises) {
    ComprobarNombre $pais "País"
    $id = [int]$pais.id
    if ($paisPorId.ContainsKey($id)) { ErrorDeDatos "País: ID repetido $id." }
    $paisPorId[$id] = $pais
    $clave = ClaveNombre ([string]$pais.nombre)
    if ($nombresPaises.ContainsKey($clave)) {
        ErrorDeDatos "País duplicado: '$($pais.nombre)' (IDs $($nombresPaises[$clave]) y $id)."
    } else { $nombresPaises[$clave] = $id }
}
foreach ($destino in $destinos) {
    ComprobarNombre $destino "Destino"
    $id = [int]$destino.id
    $paisId = [int]$destino.paisId
    if ($destinoPorId.ContainsKey($id)) { ErrorDeDatos "Destino: ID repetido $id." }
    $destinoPorId[$id] = $destino
    if (!$paisPorId.ContainsKey($paisId)) {
        ErrorDeDatos "Destino ${id}: país $paisId inexistente."
    }
    $clave = "$paisId|$(ClaveNombre ([string]$destino.nombre))"
    if ($nombresDestinos.ContainsKey($clave)) {
        ErrorDeDatos "Destino duplicado '$($destino.nombre)' en país $paisId (IDs $($nombresDestinos[$clave]) y $id)."
    } else { $nombresDestinos[$clave] = $id }
    $dias = [int]$destino.duracionDias
    $horas = [int]$destino.duracionHoras
    if ($dias -lt 0 -or $dias -gt 365 -or $horas -lt 0 -or $horas -gt 23 -or ($dias -eq 0 -and $horas -eq 0)) {
        ErrorDeDatos "Destino ${id}: duración inválida ($dias días, $horas horas)."
    }
}
foreach ($tour in $tours) {
    $id = [int]$tour.id
    $paisId = [int]$tour.paisId
    $destinoId = [int]$tour.destinoId
    if (!$paisPorId.ContainsKey($paisId)) {
        ErrorDeDatos "Tour ${id}: país $paisId inexistente."
    }
    if (!$destinoPorId.ContainsKey($destinoId)) {
        ErrorDeDatos "Tour ${id}: destino $destinoId inexistente."
        continue
    }
    $destino = $destinoPorId[$destinoId]
    if ([int]$destino.paisId -ne $paisId) {
        ErrorDeDatos "Tour ${id}: el destino $destinoId no pertenece al país $paisId."
    }
    if ([int]$tour.duracionDias -ne [int]$destino.duracionDias -or
        [int]$tour.duracionHoras -ne [int]$destino.duracionHoras) {
        ErrorDeDatos "Tour ${id}: duración distinta a la del destino $destinoId."
    }
    if ($paisPorId.ContainsKey($paisId) -and
        (ClaveNombre ([string]$tour.pais)) -ne (ClaveNombre ([string]$paisPorId[$paisId].nombre))) {
        ErrorDeDatos "Tour ${id}: nombre de país distinto al catálogo."
    }
    if ((ClaveNombre ([string]$tour.destino)) -ne (ClaveNombre ([string]$destino.nombre))) {
        ErrorDeDatos "Tour ${id}: nombre de destino distinto al catálogo."
    }
}
Write-Host "Auditoría de geografía: $($paises.Count) países, $($destinos.Count) destinos, $($tours.Count) tours."
if ($errores.Count -gt 0) {
    foreach ($mensaje in $errores) { Write-Host "[ERROR] $mensaje" -ForegroundColor Red }
    Write-Host "Resultado: $($errores.Count) incidencias. No se modificaron datos." -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Países, destinos y relaciones de tours consistentes según las respuestas de la API." -ForegroundColor Green
Write-Host "Prueba de solo lectura: NO valida índices SQL, concurrencia ni migraciones aplicadas." -ForegroundColor Yellow
exit 0
