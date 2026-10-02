using System.Globalization;
using Colegio.Api.Notas;
using Colegio.Api.Reglas;
using ClosedXML.Excel;

namespace Colegio.Api.Servicios;

public record NotaCargada(string NombreUsuario, string Actividad, decimal Valor);

public static class PlanillaExcel
{
    public static byte[] Crear(Planilla planilla)
    {
        if (planilla.Actividades.Count == 0)
            throw new ReglaDeNegocioException("Primero define las actividades y sus porcentajes. Deben sumar 100.");

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Planilla");
        hoja.Cell(1, 1).Value = "AsignacionId";
        hoja.Cell(1, 2).Value = planilla.AsignacionId;
        hoja.Cell(1, 3).Value = "PeriodoId";
        hoja.Cell(1, 4).Value = planilla.PeriodoId;
        hoja.Cell(2, 1).Value = "No cambies los encabezados ni los porcentajes. Escribe notas de 0 a 5.00.";
        hoja.Cell(3, 1).Value = "Usuario";
        hoja.Cell(3, 2).Value = "Nombre";
        hoja.Cell(4, 1).Value = "";
        hoja.Cell(4, 2).Value = "";

        for (var i = 0; i < planilla.Actividades.Count; i++)
        {
            hoja.Cell(3, i + 3).Value = planilla.Actividades[i].Nombre;
            hoja.Cell(4, i + 3).Value = planilla.Actividades[i].Porcentaje;
        }

        for (var fila = 0; fila < planilla.Estudiantes.Count; fila++)
        {
            var estudiante = planilla.Estudiantes[fila];
            hoja.Cell(fila + 5, 1).Value = estudiante.NombreUsuario;
            hoja.Cell(fila + 5, 2).Value = estudiante.Nombre;
            for (var i = 0; i < estudiante.Notas.Count; i++)
            {
                if (estudiante.Notas[i].Valor is decimal valor)
                    hoja.Cell(fila + 5, i + 3).Value = valor;
            }
        }

        hoja.Row(3).Style.Font.Bold = true;
        hoja.Columns().AdjustToContents();

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    public static byte[] CrearListado(IReadOnlyList<EstudianteDelListado> estudiantes)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Estudiantes");
        hoja.Cell(1, 1).Value = "Grupo";
        hoja.Cell(1, 2).Value = "Nombre";
        hoja.Cell(1, 3).Value = "Usuario";
        hoja.Row(1).Style.Font.Bold = true;

        for (var i = 0; i < estudiantes.Count; i++)
        {
            hoja.Cell(i + 2, 1).Value = estudiantes[i].Grupo;
            hoja.Cell(i + 2, 2).Value = estudiantes[i].Nombre;
            hoja.Cell(i + 2, 3).Value = estudiantes[i].NombreUsuario;
        }

        hoja.Columns().AdjustToContents();
        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    public static List<NotaCargada> Leer(Stream archivo, Planilla planilla)
    {
        XLWorkbook libro;
        try
        {
            libro = new XLWorkbook(archivo);
        }
        catch
        {
            throw new ReglaDeNegocioException("El archivo no es una planilla de Excel válida.");
        }

        using (libro)
        {
            if (!libro.Worksheets.TryGetWorksheet("Planilla", out var hoja))
                throw new ReglaDeNegocioException("La planilla no tiene el formato esperado. Descarga de nuevo la plantilla.");

            var errores = new List<string>();
            if (LeerEntero(hoja.Cell(1, 2)) != planilla.AsignacionId || LeerEntero(hoja.Cell(1, 4)) != planilla.PeriodoId)
                errores.Add("Esta planilla no corresponde a este grupo, materia y periodo.");

            if (!Texto(hoja.Cell(3, 1)).Equals("Usuario", StringComparison.OrdinalIgnoreCase)
                || !Texto(hoja.Cell(3, 2)).Equals("Nombre", StringComparison.OrdinalIgnoreCase))
                errores.Add("Faltan las columnas Usuario y Nombre.");

            var ultimaColumna = hoja.Row(3).LastCellUsed()?.Address.ColumnNumber ?? 2;
            if (ultimaColumna != planilla.Actividades.Count + 2)
                errores.Add("Las columnas de actividades no coinciden con los porcentajes definidos.");

            for (var i = 0; i < planilla.Actividades.Count && errores.Count == 0; i++)
            {
                var nombre = Texto(hoja.Cell(3, i + 3));
                var porcentaje = LeerDecimal(hoja.Cell(4, i + 3));
                if (!nombre.Equals(planilla.Actividades[i].Nombre, StringComparison.OrdinalIgnoreCase)
                    || porcentaje != planilla.Actividades[i].Porcentaje)
                    errores.Add("Los encabezados o los porcentajes fueron modificados. Descarga de nuevo la plantilla.");
            }

            if (errores.Count > 0)
                throw new ReglaDeNegocioException(string.Join(" ", errores));

            var esperados = planilla.Estudiantes.Select(item => item.NombreUsuario).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var notas = new List<NotaCargada>();
            var fila = 5;
            while (!hoja.Cell(fila, 1).IsEmpty())
            {
                var usuario = Texto(hoja.Cell(fila, 1)).ToLowerInvariant();
                if (!esperados.Contains(usuario))
                    errores.Add($"El usuario {usuario} no está en este grupo.");
                else if (!vistos.Add(usuario))
                    errores.Add($"El usuario {usuario} está repetido.");

                for (var i = 0; i < planilla.Actividades.Count; i++)
                {
                    var celda = hoja.Cell(fila, i + 3);
                    if (!TryNota(celda, out var valor, out var detalle))
                        errores.Add($"La nota de {usuario} en {planilla.Actividades[i].Nombre} {detalle}.");
                    else
                        notas.Add(new NotaCargada(usuario, planilla.Actividades[i].Nombre, valor));
                }

                fila++;
            }

            foreach (var faltante in esperados.Where(usuario => !vistos.Contains(usuario)))
                errores.Add($"Falta el estudiante {faltante}.");

            if (errores.Count > 0)
                throw new ReglaDeNegocioException(string.Join(" ", errores.Take(8)));

            return notas;
        }
    }

    private static bool TryNota(IXLCell celda, out decimal nota, out string detalle)
    {
        nota = 0;
        if (celda.IsEmpty())
        {
            detalle = "está vacía";
            return false;
        }

        var texto = celda.GetFormattedString().Trim().Replace(" ", "");
        if (texto.Count(caracter => caracter == ',') == 1 && !texto.Contains('.'))
            texto = texto.Replace(',', '.');

        if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out nota))
        {
            detalle = "no es un número";
            return false;
        }

        try
        {
            CalculadoraDeNotas.ExigirNota(nota);
        }
        catch (ReglaDeNegocioException excepcion)
        {
            detalle = excepcion.Message.ToLowerInvariant();
            return false;
        }

        detalle = "";
        return true;
    }

    private static string Texto(IXLCell celda) => celda.GetString().Trim();

    private static int LeerEntero(IXLCell celda)
    {
        if (celda.TryGetValue<int>(out var entero))
            return entero;
        if (celda.TryGetValue<double>(out var numero))
            return (int)numero;
        return int.TryParse(celda.GetString(), out var texto) ? texto : -1;
    }

    private static decimal LeerDecimal(IXLCell celda)
    {
        var texto = celda.GetFormattedString().Trim().Replace(" ", "");
        if (texto.Count(caracter => caracter == ',') == 1 && !texto.Contains('.'))
            texto = texto.Replace(',', '.');

        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor)
            ? valor
            : -1;
    }
}
