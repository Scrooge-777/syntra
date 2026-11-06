using System.Text.RegularExpressions;

namespace Syntra.AI.Security;

/// <summary>
/// Hardened prompt fencing utility that wraps user-provided documents, syllabi,
/// and student answers inside strict security delimiters to neutralize prompt injection attacks.
/// Also provides defensive sanitizers for raw JSON outputs from language models.
/// </summary>
public static class PromptFencing
{
    public const string ContentStartDelimiter = "<<<UNTRUSTED_STUDENT_CONTENT_START>>>";
    public const string ContentEndDelimiter = "<<<UNTRUSTED_STUDENT_CONTENT_END>>>";

    private static readonly Regex DelimiterEscapeRegex = new(
        @"(<<<|>>>|\{\{\{|\}\}\}|SYSTEM:|ASSISTANT:|USER:)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MarkdownJsonCodeBlockRegex = new(
        @"^```(?:json)?\s*([\s\S]*?)\s*```$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

    /// <summary>
    /// Extracts a pure JSON payload from an LLM response string by stripping markdown code fences
    /// and locating outermost braces/brackets defensively.
    /// </summary>
    public static string ExtractJsonPayload(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
            return "{}";

        var trimmed = rawOutput.Trim();

        // 1. Check for markdown code fences
        var match = MarkdownJsonCodeBlockRegex.Match(trimmed);
        if (match.Success)
        {
            trimmed = match.Groups[1].Value.Trim();
        }

        // 2. Defensive scan: locate first '{' or '[' and last '}' or ']'
        var firstObjectBrace = trimmed.IndexOf('{');
        var firstArrayBracket = trimmed.IndexOf('[');

        int startIndex;
        int endIndex;

        if (firstObjectBrace >= 0 && (firstArrayBracket < 0 || firstObjectBrace < firstArrayBracket))
        {
            startIndex = firstObjectBrace;
            endIndex = trimmed.LastIndexOf('}');
        }
        else if (firstArrayBracket >= 0)
        {
            startIndex = firstArrayBracket;
            endIndex = trimmed.LastIndexOf(']');
        }
        else
        {
            return trimmed;
        }

        if (startIndex >= 0 && endIndex > startIndex)
        {
            return trimmed.Substring(startIndex, endIndex - startIndex + 1);
        }

        return trimmed;
    }
}
