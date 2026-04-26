using System;
using System.Globalization;

namespace RospSqlGenerator.Services;

/// <summary>
/// Нормализует код РОСП/ОСП из пары "код региона" + "код территориального органа".
/// </summary>
public static class RospCodeNormalizer
{
    /// <summary>
    /// Формирует итоговый пятизначный код РОСП/ОСП.
    /// </summary>
    public static string Normalize(short regionCode, string agencyCode)
    {
        if (string.IsNullOrWhiteSpace(agencyCode))
        {
            throw new ArgumentException("Код территориального органа не заполнен.", nameof(agencyCode));
        }

        var normalizedAgencyCode = agencyCode.Trim();

        if (!int.TryParse(normalizedAgencyCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericAgencyCode))
        {
            throw new ArgumentException($"Код территориального органа \"{agencyCode}\" не является числом.", nameof(agencyCode));
        }

        var regionPrefix = regionCode.ToString("00", CultureInfo.InvariantCulture);

        if (normalizedAgencyCode.Length == 5 &&
            normalizedAgencyCode.StartsWith(regionPrefix, StringComparison.Ordinal))
        {
            return normalizedAgencyCode;
        }

        return $"{regionPrefix}{numericAgencyCode:000}";
    }
}