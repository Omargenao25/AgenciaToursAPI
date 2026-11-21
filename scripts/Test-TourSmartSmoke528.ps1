#requires -Version 5.1
<#
.SYNOPSIS
  TourSmart RD - comprobacion HTTP de solo lectura antes de una demostracion.
.DESCRIPTION
  Consulta API y Web en localhost. No crea usuarios, reservas ni pagos.
  No accede a SQL ni solicita credenciales. Los 401 esperados comprueban
  bloqueo anonimo, NO la matriz completa de permisos 403 entre roles.
#>
[CmdletBinding()]
param(
    [string]$ApiUrl = "http://localhost:5290",
    [string]$WebUrl = "http://localhost:5291",
    [int]$TourId = 0
)
$ErrorActionPreference = "Stop"
$fallos = 0
$avisos = 0
$exitos = 0

function OK([string]$mensaje) {
    $script:exitos++
    Write-Host "[OK] $mensaje" -ForegroundColor Green
}
function Fallo([string]$mensaje) {
    $script:fallos++
    Write-Host "[ERROR] $mensaje" -ForegroundColor Red
}
function Aviso([string]$mensaje) {
    $script:avisos++
    Write-Host "[AVISO] $mensaje" -ForegroundColor Yellow
}
function Consultar([string]$url) {
    $status = 0
    $texto = ""
    try {
        $respuesta = Invoke-WebRequest -UseBasicParsing -Uri $url -Method Get -TimeoutSec 15 -MaximumRedirection 0
        $status = [int]$respuesta.StatusCode
        $texto = [string]$respuesta.Content
    } catch {
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
        } else {
            throw "No fue posible consultar $url. Comprueba que API y Web estan iniciadas."
        }
    }
    return @{ Codigo = $status; Contenido = $texto }
}

try {
    $baseApi = $ApiUrl.TrimEnd('/')
    $baseWeb = $WebUrl.TrimEnd('/')
    if ($TourId -lt 0) { throw "TourId no puede ser negativo." }
    Write-Host "TourSmart RD - smoke test HTTP (sin escrituras)"

    $listado = Consultar "$baseApi/api/Tours"
    if ($listado.Codigo -ne 200) {
        Fallo "GET /api/Tours: esperado 200, obtenido $($listado.Codigo)."
    } else {
        OK "GET /api/Tours responde HTTP 200."
        $tours = @(ConvertFrom-Json -InputObject $listado.Contenido)
        if ($tours.Count -eq 0) { Aviso "Catalogo sin tours: no es posible probar detalle." }

        if ($TourId -gt 0) { $id = $TourId }
        else {
            $configurado = @($tours | Where-Object {
                -not [string]::IsNullOrWhiteSpace($_.puntoEncuentro) -or
                -not [string]::IsNullOrWhiteSpace($_.instruccionesSalida) -or
                -not [string]::IsNullOrWhiteSpace($_.queLlevar)
            })
            if ($configurado.Count -gt 0) { $id = [int]$configurado[0].id }
            elseif ($tours.Count -gt 0) { $id = [int]$tours[0].id }
            else { $id = 0 }
        }

        if ($id -gt 0) {
            $detalle = Consultar "$baseApi/api/Tours/$id"
            if ($detalle.Codigo -ne 200) {
                Fallo "GET /api/Tours/$id: esperado 200, obtenido $($detalle.Codigo)."
            } else {
                OK "Detalle API de tour #$id disponible."
                $tour = ConvertFrom-Json -InputObject $detalle.Contenido
                foreach ($campo in @('puntoEncuentro','instruccionesSalida','queLlevar')) {
                    if ($null -eq $tour.PSObject.Properties[$campo]) {
                        Fallo "La API no publica el campo $campo."
                    }
                }
                if ($null -ne $tour.PSObject.Properties['puntoEncuentro'] -and
                    $null -ne $tour.PSObject.Properties['instruccionesSalida'] -and
                    $null -ne $tour.PSObject.Properties['queLlevar']) {
                    OK "Contrato de informacion de salida presente en API."
                }

                $pagina = Consultar "$baseWeb/tours/$id"
                if ($pagina.Codigo -ne 200) {
                    Fallo "GET /tours/$id: esperado 200, obtenido $($pagina.Codigo)."
                } else {
                    OK "Detalle publico Web del tour #$id disponible."
                    $hayDatos = -not [string]::IsNullOrWhiteSpace($tour.puntoEncuentro) -or
                        -not [string]::IsNullOrWhiteSpace($tour.instruccionesSalida) -or
                        -not [string]::IsNullOrWhiteSpace($tour.queLlevar)
                    if ($hayDatos) {
                        if ($pagina.Contenido -match 'Información para tu salida') {
                            OK "La Web muestra la seccion de informacion de salida."
                        } else {
                            Fallo "El tour tiene datos de salida, pero no se encontro su seccion en HTML."
                        }
                    } else {
                        Aviso "El tour #$id no tiene punto de encuentro ni instrucciones; configuralos para comprobar su presentacion."
                    }
                }
            }
        }
    }

    foreach ($privado in @(
        '/api/Usuarios',
        '/api/Reservas',
        '/api/MiCuenta/perfil',
        '/api/comprobantes-transferencia/pendientes'
    )) {
        $respuestaPrivada = Consultar ($baseApi + $privado)
        if ($respuestaPrivada.Codigo -eq 401) {
            OK "Sin token: $privado denegado con HTTP 401."
        } else {
            Fallo "Sin token: $privado devolvio HTTP $($respuestaPrivada.Codigo), se esperaba 401."
        }
    }

    $inicioWeb = Consultar $baseWeb
    if ($inicioWeb.Codigo -eq 200) {
        OK "Portada publica Web HTTP 200."
    } else {
        Fallo "Portada Web: esperado HTTP 200, obtenido $($inicioWeb.Codigo)."
    }
} catch {
    Fallo $_.Exception.Message
}
Write-Host "Resultado: $exitos correctos; $avisos aviso(s); $fallos error(es)."
if ($fallos -gt 0) { exit 1 }
exit 0
