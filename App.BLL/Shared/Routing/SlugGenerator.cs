using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace App.BLL.Shared.Routing;

public static partial class SlugGenerator
{
    public const int MaxSlugLength = 128;

    public static string GenerateSlug(string? source)
    {
        var normalized = NormalizeToSlug(source);
        return string.IsNullOrWhiteSpace(normalized) ? "item" : normalized;
    }

    public static string EnsureUniqueSlug(string baseSlug, IEnumerable<string> existingSlugs)
    {
        var normalizedBaseSlug = GenerateSlug(baseSlug);
        var slugSet = new HashSet<string>(existingSlugs
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant()), StringComparer.Ordinal);

        if (!slugSet.Contains(normalizedBaseSlug))
        {
            return normalizedBaseSlug;
        }

        for (var suffix = 2; suffix < int.MaxValue; suffix++)
        {
            var candidate = AppendSuffix(normalizedBaseSlug, suffix);
            if (!slugSet.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique slug.");
    }

    private static string NormalizeToSlug(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var normalized = source.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var previousWasSeparator = false;

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                previousWasSeparator = false;
                continue;
            }

            if (previousWasSeparator)
            {
                continue;
            }

            builder.Append('-');
            previousWasSeparator = true;
        }

        var slug = DuplicateSeparatorRegex().Replace(builder.ToString(), "-").Trim('-');
        if (slug.Length <= MaxSlugLength)
        {
            return slug;
        }

        return slug[..MaxSlugLength].Trim('-');
    }

    private static string AppendSuffix(string baseSlug, int suffix)
    {
        var suffixText = $"-{suffix}";
        var maxBaseLength = Math.Max(1, MaxSlugLength - suffixText.Length);
        var trimmedBase = baseSlug.Length > maxBaseLength
            ? baseSlug[..maxBaseLength].Trim('-')
            : baseSlug;

        if (string.IsNullOrWhiteSpace(trimmedBase))
        {
            trimmedBase = "item";
        }

        return $"{trimmedBase}{suffixText}";
    }

    [GeneratedRegex("-+")]
    private static partial Regex DuplicateSeparatorRegex();
}
