using Colegio.Api.Servicios;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Colegio.Api.Servicios;

public static class GeneradorDeBoletin
{
    public static byte[] Periodo(BoletinPeriodo boletin) =>
        Documento(pagina =>
        {
            Encabezado(pagina, boletin.Colegio, boletin.Estudiante, boletin.Grado, boletin.Grupo, $"{boletin.NombrePeriodo} de {boletin.Anio}");
            pagina.Item().Table(tabla => TablaDeMaterias(tabla, boletin.Materias, boletin.MostrarDistanciaAlPromedio, boletin.MostrarDistanciaALaNotaMasAlta));
            pagina.Item().PaddingTop(12).Text("Comentario del orientador").Bold();
            pagina.Item().Text(string.IsNullOrWhiteSpace(boletin.ComentarioOrientador) ? "Sin comentario." : boletin.ComentarioOrientador);
        });

    public static byte[] Anio(BoletinAnio boletin) =>
        Documento(pagina =>
        {
            Encabezado(pagina, boletin.Colegio, boletin.Estudiante, boletin.Grado, boletin.Grupo, $"Año {boletin.Anio}");
            pagina.Item().Text("Nota final del año").Bold();
            pagina.Item().Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(3);
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                    columnas.RelativeColumn();
                });
                Encabezados(tabla, "Materia", "Nota", "Desempeño", "Resultado");
                foreach (var materia in boletin.Materias)
                {
                    Celda(tabla, materia.Materia);
                    Celda(tabla, Nota(materia.Nota));
                    Celda(tabla, materia.Desempeno ?? "—");
                    Celda(tabla, materia.Aprueba is null ? "—" : materia.Aprueba.Value ? "Aprueba" : "No aprueba");
                }
            });

            foreach (var periodo in boletin.Periodos)
            {
                pagina.Item().PaddingTop(14).Text(periodo.Nombre).Bold();
                pagina.Item().Table(tabla => TablaDeMaterias(tabla, periodo.Materias, boletin.MostrarDistanciaAlPromedio, boletin.MostrarDistanciaALaNotaMasAlta));
                pagina.Item().PaddingTop(4).Text($"Orientador: {(string.IsNullOrWhiteSpace(periodo.ComentarioOrientador) ? "Sin comentario." : periodo.ComentarioOrientador)}");
            }
        });

    private static byte[] Documento(Action<ColumnDescriptor> contenido)
    {
        return Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(1.5f, Unit.Centimetre);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(10));
                pagina.Content().Column(columna =>
                {
                    columna.Spacing(6);
                    contenido(columna);
                });
            });
        }).GeneratePdf();
    }

    private static void Encabezado(ColumnDescriptor pagina, string colegio, string estudiante, string grado, string grupo, string momento)
    {
        pagina.Item().Text("Boletín de notas").FontSize(18).Bold();
        pagina.Item().Text(colegio).FontSize(13);
        pagina.Item().Text($"{estudiante}  ·  {grado} {grupo}  ·  {momento}");
    }

    private static void TablaDeMaterias(TableDescriptor tabla, IReadOnlyList<MateriaDelPeriodo> materias, bool promedio, bool notaAlta)
    {
        tabla.ColumnsDefinition(columnas =>
        {
            columnas.RelativeColumn(3);
            columnas.RelativeColumn();
            columnas.RelativeColumn();
            columnas.RelativeColumn(3);
        });
        Encabezados(tabla, "Materia", "Nota", "Desempeño", "Comentario");
        foreach (var materia in materias)
        {
            Celda(tabla, materia.Materia);
            Celda(tabla, Nota(materia.Nota));
            Celda(tabla, materia.Desempeno ?? "—");
            Celda(tabla, string.IsNullOrWhiteSpace(materia.Comentario) ? "—" : materia.Comentario);
            if (promedio || notaAlta)
            {
                tabla.Cell().ColumnSpan(4).Padding(3).Text(Comparacion(materia, promedio, notaAlta)).FontSize(8).FontColor(Colors.Grey.Darken2);
            }
        }
    }

    private static string Comparacion(MateriaDelPeriodo materia, bool promedio, bool notaAlta)
    {
        var partes = new List<string>();
        if (promedio && materia.PromedioDelGrupo is decimal media && materia.DistanciaAlPromedio is decimal distanciaMedia)
            partes.Add($"Promedio del grupo {media:0.00}. Distancia: {distanciaMedia:0.00}.");
        if (notaAlta && materia.NotaMasAlta is decimal alta && materia.DistanciaALaNotaMasAlta is decimal distanciaAlta)
            partes.Add($"Nota más alta {alta:0.00}. Distancia: {distanciaAlta:0.00}.");
        return partes.Count == 0 ? "" : string.Join(" ", partes);
    }

    private static void Encabezados(TableDescriptor tabla, params string[] titulos)
    {
        foreach (var titulo in titulos)
            tabla.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(titulo).Bold();
    }

    private static void Celda(TableDescriptor tabla, string texto) =>
        tabla.Cell().BorderBottom(0.4f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(texto);

    private static string Nota(decimal? nota) => nota?.ToString("0.00") ?? "—";
}
