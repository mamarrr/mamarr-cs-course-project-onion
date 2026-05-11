using System.Net;
using System.Text.RegularExpressions;

namespace WebApp.Tests.Helpers;

public static partial class AntiforgeryFormHelper
{
    public static async Task<string> ExtractTokenAsync(HttpResponseMessage response)
    {
        var html = await response.Content.ReadAsStringAsync();
        var match = TokenRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException("The response did not contain an anti-forgery token.");
        }

        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    [GeneratedRegex("""<input(?=[^>]*name="__RequestVerificationToken")(?=[^>]*value="(?<token>[^"]+)")[^>]*>""", RegexOptions.IgnoreCase)]
    private static partial Regex TokenRegex();
}
