using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Xml;

namespace GestorAmbiental.Infrastructure.Export;

public static class XlsxExporter
{
    private const string SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    public static void Export<T>(
        string filePath,
        string sheetName,
        IReadOnlyList<XlsxColumn<T>> columns,
        IEnumerable<T> rows,
        CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (columns.Count == 0)
        {
            throw new ArgumentException("Informe pelo menos uma coluna para exportar.", nameof(columns));
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);
        WriteTextEntry(archive, "[Content_Types].xml", CreateContentTypesXml());
        WriteTextEntry(archive, "_rels/.rels", CreatePackageRelationshipsXml());
        WriteTextEntry(archive, "xl/workbook.xml", CreateWorkbookXml(SanitizeSheetName(sheetName)));
        WriteTextEntry(archive, "xl/_rels/workbook.xml.rels", CreateWorkbookRelationshipsXml());
        WriteWorksheet(archive, columns, rows, culture);
    }

    private static void WriteWorksheet<T>(
        ZipArchive archive,
        IReadOnlyList<XlsxColumn<T>> columns,
        IEnumerable<T> rows,
        CultureInfo culture)
    {
        var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, CreateXmlWriterSettings());

        writer.WriteStartDocument();
        writer.WriteStartElement("worksheet", SpreadsheetNamespace);
        writer.WriteStartElement("cols");

        for (var index = 1; index <= columns.Count; index++)
        {
            writer.WriteStartElement("col");
            writer.WriteAttributeString("min", index.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("max", index.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("width", "22");
            writer.WriteAttributeString("customWidth", "1");
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteStartElement("sheetData");
        WriteRow(writer, 1, columns.Select(column => column.Header));

        var rowIndex = 2;
        foreach (var row in rows)
        {
            WriteRow(writer, rowIndex, columns.Select(column => FormatValue(column.ValueSelector(row), culture)));
            rowIndex++;
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteRow(XmlWriter writer, int rowIndex, IEnumerable<string> values)
    {
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", rowIndex.ToString(CultureInfo.InvariantCulture));

        var columnIndex = 1;
        foreach (var value in values)
        {
            writer.WriteStartElement("c");
            writer.WriteAttributeString("r", $"{GetColumnName(columnIndex)}{rowIndex}");
            writer.WriteAttributeString("t", "inlineStr");
            writer.WriteStartElement("is");
            writer.WriteStartElement("t");
            writer.WriteAttributeString("xml", "space", XmlNamespace, "preserve");
            writer.WriteString(value);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
            columnIndex++;
        }

        writer.WriteEndElement();
    }

    private static string FormatValue(object? value, CultureInfo culture)
    {
        return value switch
        {
            null => string.Empty,
            DateTime date => date.ToString("dd/MM/yyyy", culture),
            DateTimeOffset date => date.ToString("dd/MM/yyyy", culture),
            decimal number => number.ToString("N2", culture),
            double number => number.ToString("N2", culture),
            float number => number.ToString("N2", culture),
            IFormattable formattable => formattable.ToString(null, culture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string GetColumnName(int index)
    {
        var dividend = index;
        var columnName = string.Empty;

        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    private static string SanitizeSheetName(string sheetName)
    {
        var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };
        var sanitized = string.Join(string.Empty, sheetName.Select(character => invalidChars.Contains(character) ? ' ' : character)).Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "Dados";
        }

        return sanitized.Length <= 31 ? sanitized : sanitized[..31];
    }

    private static void WriteTextEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write(content);
    }

    private static XmlWriterSettings CreateXmlWriterSettings()
    {
        return new XmlWriterSettings
        {
            Async = false,
            CloseOutput = false,
            Indent = false
        };
    }

    private static string CreateContentTypesXml()
    {
        return $$"""
               <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
               <Types xmlns="{{ContentTypesNamespace}}">
                 <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                 <Default Extension="xml" ContentType="application/xml"/>
                 <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                 <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
               </Types>
               """;
    }

    private static string CreatePackageRelationshipsXml()
    {
        return $$"""
               <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
               <Relationships xmlns="{{PackageRelationshipsNamespace}}">
                 <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
               </Relationships>
               """;
    }

    private static string CreateWorkbookXml(string sheetName)
    {
        return $$"""
               <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
               <workbook xmlns="{{SpreadsheetNamespace}}" xmlns:r="{{RelationshipsNamespace}}">
                 <sheets>
                   <sheet name="{{SecurityElement.Escape(sheetName)}}" sheetId="1" r:id="rId1"/>
                 </sheets>
               </workbook>
               """;
    }

    private static string CreateWorkbookRelationshipsXml()
    {
        return $$"""
               <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
               <Relationships xmlns="{{PackageRelationshipsNamespace}}">
                 <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
               </Relationships>
               """;
    }
}
