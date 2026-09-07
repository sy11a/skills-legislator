using System.Text;

namespace Legislator.Hooks;

/// <summary>
/// A POSIX shell word splitter - `shlex.split(command, posix=True)`, transliterated. The git
/// conduct guard judges what the agent actually typed, so it must see the same words the shell
/// would: quotes removed, escapes applied, and an unbalanced quote refused rather than guessed
/// at. Improving on the Python here would be a divergence, not a fix (R-8205).
/// </summary>
internal static class CommandLine
{
    /// <summary>The control operators a shell keeps as words of their own; a quoted occurrence stays inside its word and never breaks a segment.</summary>
    private static readonly HashSet<string> SegmentBreaks =
        new(StringComparer.Ordinal) { "&&", "||", ";", "|", "&", ";;", "|&" };

    /// <summary>The command line as the segments a shell would run, or an empty list when it cannot be parsed at all.</summary>
    public static List<List<string>> Segments(string command)
    {
        var words = Split(command);
        if (words is null)
        {
            return [];
        }

        var segments = new List<List<string>>();
        var current = new List<string>();
        foreach (var word in words)
        {
            if (SegmentBreaks.Contains(word))
            {
                if (current.Count > 0)
                {
                    segments.Add(current);
                }

                current = [];
                continue;
            }

            current.Add(word);
        }

        if (current.Count > 0)
        {
            segments.Add(current);
        }

        return segments;
    }

    /// <summary>The words, or null where a quote is left open - the `ValueError` the Python turns into "judged by nobody".</summary>
    private static List<string>? Split(string command)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        var started = false;
        var quote = '\0';

        for (var i = 0; i < command.Length; i++)
        {
            var c = command[i];

            if (quote == '\'')
            {
                // Single quotes are literal all the way to their partner, escapes included.
                if (c == '\'')
                {
                    quote = '\0';
                }
                else
                {
                    word.Append(c);
                }

                continue;
            }

            if (quote == '"')
            {
                // Inside double quotes a backslash escapes only the quote and itself; before
                // anything else it stays the character it is, which is what keeps a Windows
                // path spelled with backslashes intact.
                if (c == '\\' && i + 1 < command.Length && (command[i + 1] == '"' || command[i + 1] == '\\'))
                {
                    word.Append(command[++i]);
                }
                else if (c == '"')
                {
                    quote = '\0';
                }
                else
                {
                    word.Append(c);
                }

                continue;
            }

            if (c is '\'' or '"')
            {
                quote = c;
                started = true;
                continue;
            }

            if (c == '\\' && i + 1 < command.Length)
            {
                word.Append(command[++i]);
                started = true;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (started)
                {
                    words.Add(word.ToString());
                    word.Clear();
                    started = false;
                }

                continue;
            }

            word.Append(c);
            started = true;
        }

        if (quote != '\0')
        {
            return null;
        }

        if (started)
        {
            words.Add(word.ToString());
        }

        return words;
    }
}
