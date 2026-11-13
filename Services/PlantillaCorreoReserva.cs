using System.Globalization;
using System.Net;

namespace AgenciaToursAPI.Services;

/// <summary>
/// Plantilla compatible con clientes de correo: tablas e inline CSS.
/// Todos los valores provenientes de la base de datos se codifican para HTML.
/// No utiliza contenido remoto, enlaces con tokens ni imágenes de tours no públicas.
/// </summary>
public static class PlantillaCorreoReserva
{
    private static string E(string? valor) => WebUtility.HtmlEncode(valor ?? "");
    private static readonly CultureInfo Moneda = CultureInfo.GetCultureInfo("es-DO");

    public static string Crear(
        string cliente, string tour, int numeroReserva, string evento,
        DateTime? fechaTour, TimeSpan? horaTour, int pasajeros, decimal total, string estado)
    {
        var (titulo, mensaje, etiqueta, color) = evento switch
        {
            "Confirmada" => ("¡Tu aventura está confirmada!",
                "Tu reserva fue confirmada por la agencia. Ya tienes tu lugar asegurado.",
                "CONFIRMADA", "#147D52"),
            "Cancelada" => ("Tu reserva fue cancelada",
                "La reserva fue cancelada. Si corresponde una devolución, consulta con la agencia.",
                "CANCELADA", "#B43E43"),
            _ => ("¡Recibimos tu solicitud!",
                "Tu solicitud está pendiente de confirmación. La agencia verificará el adelanto antes de confirmar tu lugar.",
                "PENDIENTE", "#A66D12")
        };

        var fecha = fechaTour?.ToString("dd/MM/yyyy") ?? "Por confirmar";
        var hora = horaTour?.ToString(@"hh\:mm") ?? "Por confirmar";
        var importe = "RD$ " + total.ToString("N2", Moneda);

        return $"""
        <!doctype html>
        <html lang="es">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <title>TourSmart RD - {E(titulo)}</title>
        </head>
        <body style="margin:0;padding:0;background:#F0F5F7;font-family:Arial,Helvetica,sans-serif;color:#163543;">
          <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" style="background:#F0F5F7;">
            <tr><td align="center" style="padding:36px 14px;">
              <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
                     style="max-width:580px;background:#FFFFFF;border-radius:18px;overflow:hidden;">
                <tr><td style="padding:28px 32px;background:#0B3948;color:#FFFFFF;">
                  <div style="font-size:26px;font-weight:800;letter-spacing:-1px;">✦ TourSmart <span style="color:#89E5AD;">RD</span></div>
                  <div style="font-size:11px;letter-spacing:2px;margin-top:6px;color:#DAEBEC;">EXPERIENCIAS &amp; EXCURSIONES</div>
                </td></tr>
                <tr><td style="padding:34px 32px 12px;">
                  <div style="color:{color};font-size:12px;font-weight:700;letter-spacing:1px;">{etiqueta}</div>
                  <h1 style="font-size:28px;line-height:1.2;color:#0B3948;margin:14px 0;">{E(titulo)}</h1>
                  <p style="font-size:15px;line-height:1.7;color:#405C68;margin:0;">Hola, {E(cliente)}. {E(mensaje)}</p>
                </td></tr>
                <tr><td style="padding:22px 32px;">
                  <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
                         style="border:1px solid #DCE8EB;border-radius:12px;background:#F7FAFB;">
                    <tr><td colspan="2" style="padding:20px 20px 12px;border-bottom:1px solid #DCE8EB;">
                      <div style="font-size:12px;color:#617D88;">TU EXCURSIÓN</div>
                      <div style="font-size:19px;font-weight:700;color:#0B3948;margin-top:6px;">{E(tour)}</div>
                    </td></tr>
                    <tr><td style="padding:15px 20px;color:#617D88;font-size:13px;">N.º de reserva</td>
                        <td align="right" style="padding:15px 20px;font-size:14px;font-weight:bold;">#{numeroReserva}</td></tr>
                    <tr><td style="padding:8px 20px;color:#617D88;font-size:13px;">Fecha</td>
                        <td align="right" style="padding:8px 20px;font-size:14px;">{E(fecha)}</td></tr>
                    <tr><td style="padding:8px 20px;color:#617D88;font-size:13px;">Hora</td>
                        <td align="right" style="padding:8px 20px;font-size:14px;">{E(hora)}</td></tr>
                    <tr><td style="padding:8px 20px;color:#617D88;font-size:13px;">Pasajeros</td>
                        <td align="right" style="padding:8px 20px;font-size:14px;">{pasajeros}</td></tr>
                    <tr><td style="padding:16px 20px;border-top:1px solid #DCE8EB;font-weight:bold;color:#0B3948;">Importe total</td>
                        <td align="right" style="padding:16px 20px;border-top:1px solid #DCE8EB;font-size:18px;font-weight:bold;color:#0B3948;">{E(importe)}</td></tr>
                  </table>
                </td></tr>
                <tr><td style="padding:8px 32px 32px;">
                  <p style="font-size:13px;line-height:1.6;color:#607A84;">Este correo es informativo y no constituye un recibo de pago. Si tienes dudas, contacta a la agencia.</p>
                  <p style="font-size:13px;color:#607A84;margin-top:18px;">Gracias por elegir TourSmart RD. 🌍</p>
                </td></tr>
                <tr><td style="background:#E8F1F3;padding:20px 32px;text-align:center;color:#54717C;font-size:12px;">
                  TourSmart RD · Tu próxima aventura comienza aquí.
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }
}
