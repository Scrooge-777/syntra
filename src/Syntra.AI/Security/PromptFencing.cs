using System.Text.RegularExpressions;

namespace Syntra.AI.Security;

/// <summary>
/// Hardened prompt fencing utility that wraps user-provided documents, syllabi,
/// and student answers inside strict security delimiters to neutralize prompt injection attacks.
/// </summary>
public static class PromptFencing
{
    public const string ContentStartDelimiter = "<<<UNTRUSTED_STUDENT_CONTENT_START>>>";
    public const string ContentEndDelimiter = "<<<UNTRUSTED_STUDENT_CONTENT_END>>>";

    private static readonly Regex DelimiterEscapeRegex = new(
        @"(<<<|>>>|\{\{\{|\}\}\}|SYSTEM:|ASSISTANT:|USER:)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes untrusted user content by neutralizing delimiter escapes and prompt spoofing tokens.
    /// </summary>
    public static string Sanitize(string untrustedInput)
    {
        if (string.IsNullOrWhiteSpace(untrustedInput))
            return string.Empty;

        // Neutralize attempts to close delimiters or inject roles
        var sanitized = DelimiterEscapeRegex.Replace(untrustedInput, "[SANITIZED]");
        return sanitized.Trim();
    }

    /// <summary>
    /// Wraps sanitized content into an immutable security fence.
    /// </summary>
    public static string Fence(string untrustedInput)
    {
        var sanitized = Sanitize(untrustedInput);
        return $"{ContentStartDelimiter}\n{sanitized}\n{ContentEndDelimiter}";
    }
}
