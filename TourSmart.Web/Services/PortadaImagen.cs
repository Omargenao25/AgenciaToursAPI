using System.Text.RegularExpressions;

namespace TourSmart.Web.Services;

// La portada se configura con un archivo pequeño, sin cambiar la estructura de SQL Server.
// Los archivos se mantienen en wwwroot/uploads/portada para facilitar su copia de seguridad.
public static class PortadaImagen
{
    private static readonly Regex NombreSeguro = new(
        @"^portada-[a-f0-9]{32}\.(?:jpg|png|webp)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Carpeta(IWebHostEnvironment env)
        => Path.Combine(env.WebRootPath, "uploads", "portada");

    public static string? Obtener(IWebHostEnvironment env)
    {
        if (string.IsNullOrWhiteSpace(env.WebRootPath)) return null;
        var carpeta = Carpeta(env);
        var marcador = Path.Combine(carpeta, "actual.txt");
        if (!File.Exists(marcador)) return null;
        var nombre = File.ReadAllText(marcador).Trim();
        if (!NombreSeguro.IsMatch(nombre)) return null;
        var ruta = Path.Combine(carpeta, nombre);
        if (!File.Exists(ruta)) return null;

        // Cambia al actualizar el archivo para invalidar cachés del navegador.
        var version = File.GetLastWriteTimeUtc(ruta).Ticks;
        return "/uploads/portada/" + nombre + "?v=" + version;
    }
}
