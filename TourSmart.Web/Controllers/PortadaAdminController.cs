using System.Security.Cryptography;
using TourSmart.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TourSmart.Web.Controllers;

[Authorize(Roles = "Administrador,Operador")]
[Route("panel/portada")]
public sealed class PortadaAdminController(IWebHostEnvironment environment, ILogger<PortadaAdminController> logger) : Controller
{
    private const int LimiteBytes = 5 * 1024 * 1024;

    [HttpGet("")]
    public IActionResult Index()
    {
        Response.Headers.CacheControl = "private, no-store";
        ViewBag.ImagenActual = PortadaImagen.Obtener(environment);
        return View();
    }

    [HttpPost("guardar")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LimiteBytes + 32768)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteBytes + 32768)]
    public async Task<IActionResult> Guardar(IFormFile? imagen, CancellationToken ct)
    {
        if (imagen is null || imagen.Length is <= 0 or > LimiteBytes)
        {
            TempData["Error"] = "Selecciona una imagen JPG, PNG o WebP de hasta 5 MB.";
            return RedirectToAction(nameof(Index));
        }

        byte[] bytes;
        await using (var ms = new MemoryStream())
        {
            await imagen.CopyToAsync(ms, ct);
            if (ms.Length > LimiteBytes)
            {
                TempData["Error"] = "La imagen supera el tamaño permitido.";
                return RedirectToAction(nameof(Index));
            }
            bytes = ms.ToArray();
        }

        // Validación de firma real; no confiar en la extensión o MIME provistos por el cliente.
        string? extension = null;
        if (bytes.Length >= 4 &&
            bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff &&
            bytes[^2] == 0xff && bytes[^1] == 0xd9)
            extension = ".jpg";
        else if (bytes.Length >= 24 &&
                 bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137, 80, 78, 71, 13, 10, 26, 10}) &&
                 bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8) &&
                 System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4)) > 0 &&
                 System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4)) > 0)
            extension = ".png";
        else if (bytes.Length >= 16 &&
                 bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                 bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8) &&
                 (bytes.AsSpan(12, 4).SequenceEqual("VP8 "u8) ||
                  bytes.AsSpan(12, 4).SequenceEqual("VP8L"u8) ||
                  bytes.AsSpan(12, 4).SequenceEqual("VP8X"u8)))
            extension = ".webp";

        if (extension is null)
        {
            TempData["Error"] = "El archivo no tiene un formato JPG, PNG o WebP válido.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(environment.WebRootPath))
        {
            TempData["Error"] = "No está configurado el directorio público del servidor web.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var carpeta = PortadaImagen.Carpeta(environment);
            Directory.CreateDirectory(carpeta);
            var nombre = "portada-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant() + extension;
            var destino = Path.Combine(carpeta, nombre);
            await using (var stream = new FileStream(destino, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                await stream.WriteAsync(bytes, ct);

            // El marcador se reemplaza sólo después de que la imagen se haya escrito exitosamente.
            var temporal = Path.Combine(carpeta, "actual-" + Guid.NewGuid().ToString("N") + ".tmp");
            await System.IO.File.WriteAllTextAsync(temporal, nombre, ct);
            System.IO.File.Move(temporal, Path.Combine(carpeta, "actual.txt"), overwrite: true);
            TempData["Exito"] = "La imagen de portada se actualizó correctamente. Ya puedes verla en Inicio.";
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "No se pudo almacenar la foto de portada.");
            TempData["Error"] = "No se pudo guardar la portada. Verifica los permisos de escritura del servidor.";
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Sin acceso al almacenamiento de la portada.");
            TempData["Error"] = "No tienes acceso al directorio de imágenes del servidor.";
        }
        return RedirectToAction(nameof(Index));
    }
}
