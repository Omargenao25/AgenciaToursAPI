namespace AgenciaToursAPI.DTOs
{
    public class UsuarioRespuestaDto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}