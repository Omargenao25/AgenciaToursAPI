namespace AgenciaToursAPI.DTOs
{
    public class LoginRespuestaDto
    {
        public string Token { get; set; } = string.Empty;

        public DateTime Expiracion { get; set; }

        public string Nombre { get; set; } = string.Empty;
    }
}