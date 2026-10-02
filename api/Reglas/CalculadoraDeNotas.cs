namespace Colegio.Api.Reglas;

public static class CalculadoraDeNotas
{
    public static decimal Redondear(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    public static List<decimal> PesosIguales(int cantidadDePeriodos)
    {
        var pesoBase = 100 / cantidadDePeriodos;
        var resto = 100 % cantidadDePeriodos;
        return Enumerable.Range(0, cantidadDePeriodos)
            .Select(indice => (decimal)(pesoBase + (indice < resto ? 1 : 0)))
            .ToList();
    }

    // Si falta alguna actividad, todavía no hay nota de periodo.
    public static decimal? NotaDeActividades(IReadOnlyList<decimal?> valores, IReadOnlyList<decimal> porcentajes)
    {
        if (valores.Count == 0 || valores.Count != porcentajes.Count || valores.Any(valor => valor is null))
            return null;

        var total = 0m;
        for (var i = 0; i < valores.Count; i++)
            total += valores[i]!.Value * porcentajes[i] / 100m;

        return Redondear(total);
    }

    // La nivelación reemplaza la nota calculada. Si no hay nivelación, queda la de las actividades.
    public static decimal? NotaDelPeriodo(decimal? notaDeActividades, decimal? nivelacionMasReciente) =>
        nivelacionMasReciente ?? notaDeActividades;

    public static decimal? NotaDelAnio(IReadOnlyList<(decimal nota, decimal peso)> periodosConNota)
    {
        if (periodosConNota.Count == 0)
            return null;

        var pesoTotal = periodosConNota.Sum(periodo => periodo.peso);
        if (pesoTotal == 0)
            return null;

        var total = periodosConNota.Sum(periodo => periodo.nota * periodo.peso / pesoTotal);
        return Redondear(total);
    }

    public static string Desempeno(decimal nota, IReadOnlyList<(string nombre, decimal desde, decimal hasta)> rangos)
    {
        foreach (var rango in rangos)
        {
            if (nota >= rango.desde && nota <= rango.hasta)
                return rango.nombre;
        }

        return "Sin equivalencia";
    }

    public static bool Aprueba(decimal nota, decimal notaMinima) => nota >= notaMinima;

    public static void ExigirPorcentajes(IReadOnlyList<decimal> porcentajes)
    {
        if (porcentajes.Count == 0)
            throw new ReglaDeNegocioException("Define al menos una actividad.");

        if (porcentajes.Any(porcentaje => porcentaje <= 0))
            throw new ReglaDeNegocioException("Cada porcentaje tiene que ser mayor que 0.");

        var suma = porcentajes.Sum(Redondear);
        if (suma != 100m)
            throw new ReglaDeNegocioException($"Los porcentajes suman {suma:0.##} y deben sumar 100.");
    }

    public static void ExigirPesos(IReadOnlyList<decimal> pesos)
    {
        if (pesos.Any(peso => peso <= 0))
            throw new ReglaDeNegocioException("Cada periodo tiene que pesar más que 0.");

        var suma = pesos.Sum(Redondear);
        if (suma != 100m)
            throw new ReglaDeNegocioException($"Los pesos de los periodos suman {suma:0.##} y deben sumar 100.");
    }

    public static void ExigirNota(decimal nota)
    {
        if (nota < 0 || nota > 5)
            throw new ReglaDeNegocioException("La nota debe estar entre 0 y 5.00.");

        if (nota != Redondear(nota))
            throw new ReglaDeNegocioException("La nota solo puede tener dos decimales.");
    }

    public static void ExigirCantidadDePeriodos(int cantidad)
    {
        if (cantidad < 2 || cantidad > 6)
            throw new ReglaDeNegocioException("Un año lectivo tiene entre 2 y 6 periodos. Lo habitual es 4.");
    }
}

public class ReglaDeNegocioException : Exception
{
    public ReglaDeNegocioException(string mensaje) : base(mensaje)
    {
    }
}

public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje)
    {
    }
}
