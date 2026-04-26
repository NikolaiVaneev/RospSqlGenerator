using RospSqlGenerator.Models;
using System.Collections.Generic;

namespace RospSqlGenerator.Abstractions;

/// <summary>
/// Формирует SQL-скрипт обновления справочника РОСП/ОСП.
/// </summary>
public interface IRospSqlGenerator
{
    string Generate(IReadOnlyCollection<RospPreviewRow> rows);
}