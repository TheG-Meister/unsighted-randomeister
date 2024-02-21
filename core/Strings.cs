using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.core;

public class Strings
{

    private static readonly List<char> charsToKeepInAnnouncements = new() { '.' };

    public static string SnakeToPascalCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        else
        {
            StringBuilder builder = new();

            bool lastUnderscore = true;
            foreach (char c in text)
            {
                if (c == '_') lastUnderscore = true;
                else if (char.IsLetter(c))
                {
                    if (lastUnderscore)
                    {
                        builder.Append(char.ToUpperInvariant(c));
                        lastUnderscore = false;
                    }
                    else builder.Append(char.ToLowerInvariant(c));
                }
                else builder.Append(c);
            }

            return builder.ToString();
        }
    }

    public static string ReplaceSpecialCharsInPascal(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        else
        {
            StringBuilder builder = new();

            bool firstChar = true;
            char lastChar = ' ';
            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    if (!firstChar && !char.IsLetter(lastChar))
                    {
                        builder.Append(' ');
                        builder.Append(char.ToUpperInvariant(c));
                    }
                    else
                    {
                        if (!firstChar && char.IsUpper(c)) builder.Append(' ');
                        builder.Append(c);
                    }
                }
                else if (char.IsNumber(c))
                {
                    if (!firstChar && (!char.IsNumber(lastChar) && lastChar != '.')) builder.Append(' ');
                    builder.Append(c);
                }
                else if (charsToKeepInAnnouncements.Contains(c)) builder.Append(c);

                firstChar = false;
                lastChar = c;
            }

            return builder.ToString();
        }
    }

}
