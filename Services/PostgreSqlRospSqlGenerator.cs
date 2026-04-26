using RospSqlGenerator.Abstractions;
using RospSqlGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RospSqlGenerator.Services;

/// <summary>
/// Формирует PostgreSQL-скрипт обновления справочника РОСП/ОСП.
/// </summary>
public sealed class PostgreSqlRospSqlGenerator : IRospSqlGenerator
{
    /// <inheritdoc />
    public string Generate(IReadOnlyCollection<RospImportRow> rows)
    {
        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Нет строк для генерации SQL.");
        }

        var orderedRows = rows
            .OrderBy(row => row.Code, StringComparer.Ordinal)
            .ThenBy(row => row.SourceRowNumber)
            .ToArray();

        var duplicateCodes = orderedRows
            .GroupBy(row => row.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateCodes.Length > 0)
        {
            throw new InvalidOperationException(
                $"В выбранных строках есть дубли кодов: {string.Join(", ", duplicateCodes)}.");
        }

        var builder = new StringBuilder();

        builder.AppendLine("BEGIN;");
        builder.AppendLine();
        builder.AppendLine("DROP TABLE IF EXISTS \"TempRosps\";");
        builder.AppendLine("CREATE TEMP TABLE \"TempRosps\"");
        builder.AppendLine("(\"Region\" smallint, \"Code\" text, \"Name\" text, \"Address\" text, \"FsspChiefsFullName\" text, \"FsspTelephoneNumber\" text, \"FsspPhoneOfHelpService\" text, \"FsspPhoneOfHelpService2\" text, \"FsspWorkHours\" text);");
        builder.AppendLine();
        builder.AppendLine("INSERT INTO \"TempRosps\" (\"Region\", \"Code\", \"Name\", \"Address\", \"FsspChiefsFullName\", \"FsspTelephoneNumber\", \"FsspPhoneOfHelpService\", \"FsspPhoneOfHelpService2\", \"FsspWorkHours\")");
        builder.AppendLine("VALUES");

        for (var index = 0; index < orderedRows.Length; index++)
        {
            var row = orderedRows[index];
            var suffix = index == orderedRows.Length - 1 ? ";" : ",";

            builder.Append("    ");
            builder.Append('(');
            builder.Append(row.Region);
            builder.Append(", ");
            builder.Append(ToSqlLiteral(row.Code));
            builder.Append(", ");
            builder.Append(ToSqlLiteral(row.Name));
            builder.Append(", ");
            builder.Append(ToSqlLiteral(row.Address));
            builder.Append(", ");
            builder.Append(ToSqlLiteral(row.FsspChiefsFullName));
            builder.Append(", ");
            builder.Append(ToSqlLiteral(row.FsspTelephoneNumber));
            builder.Append(", ");
            builder.Append(ToSqlLiteral(row.FsspPhoneOfHelpService));
            builder.Append(", ");
            builder.Append(ToSqlLiteral(row.FsspPhoneOfHelpService2));
            builder.Append(", ");
            builder.Append(ToSqlLiteral(row.FsspWorkHours));
            builder.Append(')');
            builder.AppendLine(suffix);
        }

        builder.AppendLine();
        builder.AppendLine("INSERT INTO \"Rosps\"");
        builder.AppendLine("(");
        builder.AppendLine("    \"Code\",");
        builder.AppendLine("    \"Name\",");
        builder.AppendLine("    \"Address\",");
        builder.AppendLine("    \"FsspChiefsFullName\",");
        builder.AppendLine("    \"FsspPhoneOfHelpService\",");
        builder.AppendLine("    \"FsspPhoneOfHelpService2\",");
        builder.AppendLine("    \"FsspTelephoneNumber\",");
        builder.AppendLine("    \"FsspWorkHours\",");
        builder.AppendLine("    \"AdministrationId\"");
        builder.AppendLine(")");
        builder.AppendLine("SELECT");
        builder.AppendLine("    tr.\"Code\",");
        builder.AppendLine("    tr.\"Name\",");
        builder.AppendLine("    tr.\"Address\",");
        builder.AppendLine("    tr.\"FsspChiefsFullName\",");
        builder.AppendLine("    tr.\"FsspPhoneOfHelpService\",");
        builder.AppendLine("    tr.\"FsspPhoneOfHelpService2\",");
        builder.AppendLine("    tr.\"FsspTelephoneNumber\",");
        builder.AppendLine("    tr.\"FsspWorkHours\",");
        builder.AppendLine("    r.\"AdministrationId\"");
        builder.AppendLine("FROM \"TempRosps\" tr");
        builder.AppendLine("LEFT JOIN public.\"Regions\" r");
        builder.AppendLine("    ON tr.\"Region\"::smallint = r.\"RegionCode\"");
        builder.AppendLine("ON CONFLICT (\"Code\")");
        builder.AppendLine("DO UPDATE");
        builder.AppendLine("SET");
        builder.AppendLine("    \"Name\" = EXCLUDED.\"Name\",");
        builder.AppendLine("    \"Address\" = EXCLUDED.\"Address\",");
        builder.AppendLine("    \"FsspChiefsFullName\" = EXCLUDED.\"FsspChiefsFullName\",");
        builder.AppendLine("    \"FsspTelephoneNumber\" = EXCLUDED.\"FsspTelephoneNumber\",");
        builder.AppendLine("    \"FsspPhoneOfHelpService\" = EXCLUDED.\"FsspPhoneOfHelpService\",");
        builder.AppendLine("    \"FsspPhoneOfHelpService2\" = EXCLUDED.\"FsspPhoneOfHelpService2\",");
        builder.AppendLine("    \"FsspWorkHours\" = EXCLUDED.\"FsspWorkHours\",");
        builder.AppendLine("    \"AdministrationId\" = EXCLUDED.\"AdministrationId\";");
        builder.AppendLine();
        builder.AppendLine("DROP TABLE IF EXISTS \"TempRosps\";");
        builder.AppendLine();
        builder.AppendLine("COMMIT;");

        return builder.ToString();
    }

    private static string ToSqlLiteral(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "null";
        }

        return $"'{value.Replace("'", "''")}'";
    }
}