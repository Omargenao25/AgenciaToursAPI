#requires -Version 5.1
param([int]$TourId = 1, [int]$ReservaId = 1, [int]$ReservaAjenaId = 0)
$ErrorActionPreference = "Stop"
$api = "http://localhost:5290"
$names = @("TOURSMART_TOKEN_ADMIN", "TOURSMART_TOKEN_OPERADOR", "TOURSMART_TOKEN_CLIENTE")
$prior = @{}
foreach ($n in $names) { $prior[$n] = [Environment]::GetEnvironmentVariable($n, "Process") }

function Login-Prueba([string]$rol) {
    $correo = Read-Host "Correo de cuenta de PRUEBA ($rol)"
    $segura = Read-Host "Contrasena (oculta)" -AsSecureString
    $ptr = [IntPtr]::Zero
    try {
        $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura)
        $datos = @{ correo = $correo.Trim() }
        $datos[("contrase" + [char]0x00F1 + "a")] = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
        $bytes = [Text.Encoding]::UTF8.GetBytes(($datos | ConvertTo-Json -Compress))
        $r = Invoke-RestMethod -Method Post -Uri "$api/api/Auth/login" -Body $bytes -ContentType "application/json; charset=utf-8" -TimeoutSec 20
        if ($r.rol -ne $rol -or [string]::IsNullOrWhiteSpace([string]$r.token)) { throw "Rol o token incorrecto." }
        Write-Host "[OK] Sesion $rol autenticada." -ForegroundColor Green
        return [string]$r.token
    }
    finally {
        if ($ptr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
        $datos = $null
        $bytes = $null
        $r = $null
        $segura.Dispose()
    }
}

$codigo = 1
try {
    $env:TOURSMART_TOKEN_ADMIN = Login-Prueba "Administrador"
    $env:TOURSMART_TOKEN_OPERADOR = Login-Prueba "Operador"
    $env:TOURSMART_TOKEN_CLIENTE = Login-Prueba "Cliente"
    & (Join-Path $PSScriptRoot "Test-TourSmartSecurity.ps1") -TourId $TourId -ReservaId $ReservaId
    $seguridad = $LASTEXITCODE
    & (Join-Path $PSScriptRoot "Test-TourSmartReservas.ps1")
    $reservas = $LASTEXITCODE
    $ajena = 0
    if ($ReservaAjenaId -gt 0) {
        # The supplied ID must belong to a different customer in the test database.
        foreach ($url in @("$api/api/MiCuenta/reservas/$ReservaAjenaId/estado-adelanto", "$api/api/comprobantes-transferencia/mis-reservas/$ReservaAjenaId")) {
            $status = 0
            try {
                $r = Invoke-WebRequest -Uri $url -Method Get -Headers @{Authorization = "Bearer $env:TOURSMART_TOKEN_CLIENTE"} -UseBasicParsing -MaximumRedirection 0 -TimeoutSec 12
                $status = [int]$r.StatusCode
            } catch {
                if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
            }
            if ($status -eq 404) { Write-Host "[OK] Foreign reservation denied (404)." -ForegroundColor Green }
            else { Write-Host "[ERROR] Foreign reservation check: HTTP $status." -ForegroundColor Red; $ajena = 1 }
        }
    } else { Write-Host "[OMITIDO] Proporciona -ReservaAjenaId para probar aislamiento." -ForegroundColor Yellow }
    if ($seguridad -eq 0 -and $reservas -eq 0 -and $ajena -eq 0) { $codigo = 0 }
    Write-Host "Resumen: seguridad=$seguridad; reservas=$reservas; aislamiento=$ajena (0=sin fallos ejecutados)."
}
catch { Write-Host "[ERROR] No fue posible completar la auditoria. Revisa cuentas de prueba y servicios locales." -ForegroundColor Red }
finally {
    foreach ($n in $names) { [Environment]::SetEnvironmentVariable($n, $prior[$n], "Process") }
}
exit $codigo
