using Colegio.Api.Reglas;

namespace Colegio.Api.Tests;

public class CalculadoraDeNotasTests
{
    private static readonly List<(string nombre, decimal desde, decimal hasta)> Rangos =
    [
        ("Bajo", 0m, 2.99m),
        ("Básico", 3.00m, 3.99m),
        ("Alto", 4.00m, 4.59m),
        ("Superior", 4.60m, 5.00m)
    ];

    [Fact]
    public void La_nota_del_periodo_es_el_promedio_ponderado()
    {
        var nota = CalculadoraDeNotas.NotaDeActividades([4m, 3m, 5m], [30m, 30m, 40m]);

        Assert.Equal(4.10m, nota);
    }

    [Fact]
    public void Sin_una_actividad_no_hay_nota_de_periodo()
    {
        var nota = CalculadoraDeNotas.NotaDeActividades([4m, null, 5m], [30m, 30m, 40m]);

        Assert.Null(nota);
    }

    [Fact]
    public void La_nivelacion_reemplaza_la_nota_calculada()
    {
        var nota = CalculadoraDeNotas.NotaDelPeriodo(3.20m, 3.80m);

        Assert.Equal(3.80m, nota);
    }

    [Fact]
    public void La_nota_del_anio_respeta_el_peso_de_cada_periodo()
    {
        var nota = CalculadoraDeNotas.NotaDelAnio([(3m, 30m), (4m, 30m), (5m, 40m)]);

        Assert.Equal(4.10m, nota);
    }

    [Fact]
    public void Cuatro_periodos_pesan_lo_mismo()
    {
        Assert.Equal([25m, 25m, 25m, 25m], CalculadoraDeNotas.PesosIguales(4));
    }

    [Fact]
    public void La_nota_se_traduce_al_desempeno_del_decreto_1290()
    {
        Assert.Equal("Bajo", CalculadoraDeNotas.Desempeno(2.99m, Rangos));
        Assert.Equal("Básico", CalculadoraDeNotas.Desempeno(3.00m, Rangos));
        Assert.Equal("Alto", CalculadoraDeNotas.Desempeno(4.59m, Rangos));
        Assert.Equal("Superior", CalculadoraDeNotas.Desempeno(4.60m, Rangos));
    }

    [Fact]
    public void Aprueba_desde_la_nota_minima()
    {
        Assert.True(CalculadoraDeNotas.Aprueba(3.00m, 3.00m));
        Assert.False(CalculadoraDeNotas.Aprueba(2.99m, 3.00m));
    }

    [Fact]
    public void Rechaza_una_nota_con_mas_de_dos_decimales()
    {
        var error = Assert.Throws<ReglaDeNegocioException>(() => CalculadoraDeNotas.ExigirNota(4.555m));

        Assert.Contains("dos decimales", error.Message);
    }

    [Fact]
    public void Los_porcentajes_tienen_que_sumar_100()
    {
        var error = Assert.Throws<ReglaDeNegocioException>(() => CalculadoraDeNotas.ExigirPorcentajes([40m, 40m]));

        Assert.Contains("100", error.Message);
    }
}
