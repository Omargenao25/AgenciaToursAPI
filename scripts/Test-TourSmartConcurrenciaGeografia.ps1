#requires -Version 5.1
<#
.SYNOPSIS
  Prueba de concurrencia: dos intentos simultaneos de crear el mismo pais.
.DESCRIPTION
  SOLO para una base de pruebas aislada, con respaldo y credenciales de Administrador u Operador.
  Usa una clave temporal unica y elimina el registro al finalizar.
  Puede dejar un registro temporal si falla la limpieza; nunca usar en produccion.
.EXAMPLE
  $env:TOURSMART_TEST_TOKEN = "<JWT de pruebas>"
  .\scripts\Test-TourSmartConcurrenciaGeografia.ps1 -BaseAisladaConfirmada
#>
param(
    [string]$ApiBase = "http://localhost:5290",
    [switch]$BaseAisladaConfirmada
)
$ErrorActionPreference = "Stop"
$global:LASTEXITCODE = 0
if (-not $BaseAisladaConfirmada) {
    Write-Host "[BLOQUEADO] Usa una base de pruebas AISLADA y respaldo. Para habilitar: -BaseAisladaConfirmada" -ForegroundColor Yellow
    exit 2
}
$uri = $null
if (-not [Uri]::TryCreate($ApiBase, [UriKind]::Absolute, [ref]$uri) -or
    $uri.Host -notin @("localhost", "127.0.0.1") -or
    $uri.AbsolutePath -ne "/" -or
    $uri.Scheme -notin @("http", "https")) {
    Write-Host "[BLOQUEADO] Solo se permite una API localhost sin rutas extras." -ForegroundColor Red
    exit 2
}
$jwt = $env:TOURSMART_TEST_TOKEN
if ([string]::IsNullOrWhiteSpace($jwt)) {
    Write-Host "[BLOQUEADO] Configura TOURSMART_TEST_TOKEN con JWT de una cuenta de PRUEBAS." -ForegroundColor Yellow
    exit 2
}
# Carga .NET HttpClient y solicitudes asincronas compatibles con Windows PowerShell 5.1
Add-Type -AssemblyName System.Net.Http
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(20)
$client.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $jwt)
$client.DefaultRequestHeaders.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new("application/json"))
$nombre = "PruebaUnicaGeo" + [Guid]::NewGuid().ToString("N").Substring(0, 16)
$idCreado = $null
$enviar1 = $null
$enviar2 = $null
try {
    $json = @{ nombre = $nombre } | ConvertTo-Json -Compress
    $req1 = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$($ApiBase.TrimEnd('/'))/api/Paises")
    $req2 = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$($ApiBase.TrimEnd('/'))/api/Paises")
    $req1.Content = [System.Net.Http.StringContent]::new($json, [Text.Encoding]::UTF8, "application/json")
    $req2.Content = [System.Net.Http.StringContent]::new($json, [Text.Encoding]::UTF8, "application/json")
    try {
        $enviar1 = $client.SendAsync($req1)
        $enviar2 = $client.SendAsync($req2)
        [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($enviar1, $enviar2))
        $respuestas = @($enviar1.Result, $enviar2.Result)
        $codigos = @($respuestas | ForEach-Object { [int]$_.StatusCode })
        Write-Host "Solicitudes POST simultaneas: HTTP $($codigos -join ' y HTTP ')"
        $creadas = @($respuestas | Where-Object { [int]$_.StatusCode -eq 201 })
        $conflictos = @($respuestas | Where-Object { [int]$_.StatusCode -eq 409 })
        if ($creadas.Count -eq 1) {
            $contenido = $creadas[0].Content.ReadAsStringAsync().GetAwaiter().GetResult()
            $obj = $contenido | ConvertFrom-Json
            $idCreado = [int]$obj.id
        }
        if ($creadas.Count -ne 1 -or $conflictos.Count -ne 1) {
            throw "Se esperaba un HTTP 201 y un HTTP 409. La prueba no paso."
        }
        Write-Host "[OK] Solo una solicitud pudo crear el pais. La otra recibio HTTP 409." -ForegroundColor Green
    }
    finally {
        if ($req1) { $req1.Dispose() }
        if ($req2) { $req2.Dispose() }
        if ($enviar1 -and $enviar1.Status -eq [Threading.Tasks.TaskStatus]::RanToCompletion) { $enviar1.Result.Dispose() }
        if ($enviar2 -and $enviar2.Status -eq [Threading.Tasks.TaskStatus]::RanToCompletion) { $enviar2.Result.Dispose() }
    }
}
catch {
    Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
    $global:LASTEXITCODE = 1
}
finally {
    # Buscar el registro por nombre para limpiar incluso si no se recibio su Id.
    try {
        $listar = $client.GetAsync("$($ApiBase.TrimEnd('/'))/api/Paises").GetAwaiter().GetResult()
        try {
            $listar.EnsureSuccessStatusCode()
            $datos = $listar.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
            $encontrados = @($datos | Where-Object { $_.nombre -eq $nombre })
            if ($encontrados.Count -ne 1) {
                Write-Host "[ALERTA] Registros con ese nombre: $($encontrados.Count). Revisar la base de pruebas." -ForegroundColor Yellow
                $global:LASTEXITCODE = 1
            }
            foreach ($pais in $encontrados) {
                $id = [int]$pais.id
                $borrado = $client.DeleteAsync("$($ApiBase.TrimEnd('/'))/api/Paises/$id").GetAwaiter().GetResult()
                try {
                    if ([int]$borrado.StatusCode -ne 204) {
                        Write-Host "[ALERTA] No se pudo limpiar el pais temporal ID $id (HTTP $([int]$borrado.StatusCode))." -ForegroundColor Red
                        $global:LASTEXITCODE = 1
                    } else {
                        Write-Host "[OK] Registro temporal $id eliminado de la base aislada." -ForegroundColor Green
                    }
                } finally { $borrado.Dispose() }
            }
        }
        finally { $listar.Dispose() }
    }
    catch {
        Write-Host "[ALERTA] Limpieza no verificada: $($_.Exception.Message). Nombre temporal: $nombre" -ForegroundColor Red
        $global:LASTEXITCODE = 1
    }
    $client.Dispose()
}
if ($global:LASTEXITCODE -eq 1) { exit 1 }
exit 0
