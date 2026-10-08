using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public enum IdFormat
{
    AdmobAppId,
    AdmobAdUnitId,
    UnityAdsGameId,
    UnityAdsPlacementId
}

public static class IdValidationExtensions
{
    private static readonly Dictionary<IdFormat, Regex> _rules = new()
    {
        { IdFormat.AdmobAppId, new Regex(@"\Aca-app-pub-\d{16}~\d{10}\z", RegexOptions.CultureInvariant | RegexOptions.Compiled) },
        { IdFormat.AdmobAdUnitId, new Regex(@"\Aca-app-pub-\d{16}/\d{10}\z", RegexOptions.CultureInvariant | RegexOptions.Compiled) },
        { IdFormat.UnityAdsGameId, new Regex(@"\A\d{6,12}\z", RegexOptions.CultureInvariant | RegexOptions.Compiled) },
        { IdFormat.UnityAdsPlacementId, new Regex(@"\A[a-zA-Z0-9_-]{1,64}\z", RegexOptions.CultureInvariant | RegexOptions.Compiled) },
    };

    public static string IsThisValid(this string value, IdFormat format)
        => ValidateInternal(value, format, trim: true, returnStringOnSuccess: true);

    public static bool IsThisValid(this string value, IdFormat format, bool trim)
        => ValidateInternal(value, format, trim: trim, returnStringOnSuccess: false) != null;

    // Returns:
    // - trimmed string when returnStringOnSuccess=true and valid
    // - "" when returnStringOnSuccess=true and invalid
    // - null when returnStringOnSuccess=false and invalid/valid is represented by != null
    private static string ValidateInternal(string value, IdFormat format, bool trim, bool returnStringOnSuccess)
    {
        if (value == null)
        {
            Debug.LogError($"Given String is null, doesn't match the given format {format}");
            return returnStringOnSuccess ? string.Empty : null;
        }

        string processed = trim ? value.Trim() : value;

        if (trim ? string.IsNullOrWhiteSpace(processed) : processed.Length == 0)
        {
            Debug.LogError($"Given String is empty or doesn't match the given format {format}");
            return returnStringOnSuccess ? string.Empty : null;
        }

        if (!_rules.TryGetValue(format, out var regex))
        {
            Debug.LogError($"No validation rule found for format {format}");
            return returnStringOnSuccess ? string.Empty : null;
        }

        bool matched = regex.IsMatch(processed);
        if (!matched)
        {
            Debug.LogError($"Given String: {processed}, doesn't match the given format {format}");
            return returnStringOnSuccess ? string.Empty : null;
        }

        return returnStringOnSuccess ? processed : processed; // non-null means true for bool overload
    }
}



