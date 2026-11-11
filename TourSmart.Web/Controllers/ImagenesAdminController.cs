using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TourSmart.Web.Controllers;

[Authorize(Roles = "Administrador,Operador")]
[Route("panel/imagenes")]
public sealed class ImagenesAdminController(IWebHostEnvironment environment, ILogger<ImagenesAdminController> logger) : Controller
{
    private const int MaxBytes = 5 * 1024 * 1024;

    [HttpPost("subir")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxBytes + 1024 * 32)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBytes + 1024 * 32)]
    public async Task<IActionResult> Subir(IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { error = "Selecciona una imagen JPG, PNG o WebP." });

        if (archivo.Length > MaxBytes)
            return BadRequest(new { error = "La imagen no puede superar 5 MB." });

        // No confiamos en ContentType, nombre ni extension del archivo enviado.
        // Analizamos la firma antes de escribir datos en el directorio publico.
        byte[] bytes;
        await using (var memory = new MemoryStream())
        {
            await archivo.CopyToAsync(memory, ct);
            if (memory.Length > MaxBytes)
                return BadRequest(new { error = "La imagen supera los 5 MB permitidos." });
            bytes = memory.ToArray();
        }

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
            return BadRequest(new { error = "Archivo inválido: utiliza una imagen JPG, PNG o WebP auténtica." });

        var root = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
            return StatusCode(500, new { error = "El servidor no tiene almacenamiento de imágenes configurado." });

        var folder = Path.Combine(root, "uploads", "tours");
        Directory.CreateDirectory(folder);

        var nombre = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant() + extension;
        var path = Path.Combine(folder, nombre);
        try
        {
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await stream.WriteAsync(bytes, ct);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "No fue posible guardar la imagen del catalogo.");
            return StatusCode(500, new { error = "No se pudo guardar la imagen. Comprueba el almacenamiento." });
        }
        return Ok(new { url = "/uploads/tours/" + nombre });
    }
}
