using AgenciaToursAPI.Security;
using AgenciaToursAPI.DTOs;
using AgenciaToursAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Xunit;

namespace TourSmart.Tests;

public sealed class ReglasNegocioTests
{
    [Theory]
    [InlineData(10, 8, 2, true)]
    [InlineData(10, 8, 3, false)]
    [InlineData(10, 0, 10, true)]
    [InlineData(10, 10, 1, false)]
    [InlineData(10, -1, 1, false)]
    [InlineData(10, 0, 0, false)]
    [InlineData(int.MaxValue, int.MaxValue, 1, false)]
    public void CuposEvitanSobreventa(int capacidad, int ocupados, int solicitados, bool esperado) =>
        Assert.Equal(esperado, ReglasReserva.HayCupos(capacidad, ocupados, solicitados));

    [Theory]
    [InlineData("100.01", 30, "30.01")]
    [InlineData("100.00", 30, "30.00")]
    [InlineData("1.01", 30, "0.31")]
    [InlineData("1500.00", 0, "0.00")]
    [InlineData("1500.00", 100, "1500.00")]
    public void AdelantoSeRedondeaHaciaArriba(string total, int pct, string esperado) =>
        Assert.Equal(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture),
            ReglasReserva.AdelantoMinimo(
                decimal.Parse(total, System.Globalization.CultureInfo.InvariantCulture), pct));

    [Fact]
    public void AdelantoRechazaPorcentajesImposibles()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReglasReserva.AdelantoMinimo(100m, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReglasReserva.AdelantoMinimo(100m, 101));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReglasReserva.AdelantoMinimo(-1m, 30));
    }

    [Fact]
    public void ReembolsoDisminuyeAdelantoNetoYAumentaSaldo()
    {
        Assert.Equal(90m, ReglasReserva.Saldo(100m, 20m, 10m));
        Assert.False(ReglasReserva.TieneAdelanto(100m, 30, 40m, 15m));
        Assert.True(ReglasReserva.TieneAdelanto(100m, 30, 40m, 10m));
    }

    [Fact]
    public void PasajerosNormalizanEspaciosYPreservanOrden()
    {
        Assert.True(ValidacionPasajeros.IntentarNormalizar(2,
            new[] { "  Ana    María ", " Luis  Pérez " }, out var lista, out _));
        Assert.Collection(lista,
            p => { Assert.Equal(1, p.Orden); Assert.Equal("Ana María", p.NombreCompleto); },
            p => { Assert.Equal(2, p.Orden); Assert.Equal("Luis Pérez", p.NombreCompleto); });
    }

    [Theory]
    [InlineData(2, new[] { "Ana" })]
    [InlineData(1, new[] { "A" })]
    [InlineData(1, new[] { "123" })]
    [InlineData(0, new[] { "Ana" })]
    public void PasajerosRechazanEntradasInvalidas(int cantidad, string[] nombres) =>
        Assert.False(ValidacionPasajeros.IntentarNormalizar(cantidad, nombres, out _, out _));

    [Fact]
    public void RegistroAlternativoNoEsAnonimo()
    {
        var tipo = typeof(UsuariosController);
        var method = tipo.GetMethod(nameof(UsuariosController.PostUsuario))!;
        var clase = tipo.GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal(Roles.Administrador, clase?.Roles);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void EndpointsDeReservaConservanRolesEsperados()
    {
        Assert.Equal(Roles.Personal, typeof(ReservasController)
            .GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.Equal(Roles.Personal, typeof(PagosReservaController)
            .GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.Equal(Roles.Cliente, typeof(MiCuentaController)
            .GetMethod(nameof(MiCuentaController.CrearMiReserva))!
            .GetCustomAttribute<AuthorizeAttribute>()?.Roles);
    }
}
