using System;
using System.Collections.Generic;

namespace PdfToolbox;

internal static class PageRangeParser
{
    /// <summary>"1-3,5" や "all" を0始まりのページインデックス列に変換する。</summary>
    public static List<int> Parse(string text, int pageCount)
    {
        var result = new List<int>();
        text = text.Trim();
        if (text.Length == 0)
        {
            return result;
        }

        if (text.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            for (var i = 0; i < pageCount; i++)
            {
                result.Add(i);
            }
            return result;
        }

        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Contains('-'))
            {
                var bounds = part.Split('-', StringSplitOptions.TrimEntries);
                if (bounds.Length != 2 || !int.TryParse(bounds[0], out var start) || !int.TryParse(bounds[1], out var end))
                {
                    throw new FormatException($"ページ範囲の指定が不正です: {part}");
                }
                for (var p = start; p <= end; p++)
                {
                    AddIfValid(result, p, pageCount);
                }
            }
            else
            {
                if (!int.TryParse(part, out var page))
                {
                    throw new FormatException($"ページ範囲の指定が不正です: {part}");
                }
                AddIfValid(result, page, pageCount);
            }
        }

        return result;
    }

    private static void AddIfValid(List<int> result, int oneBasedPage, int pageCount)
    {
        if (oneBasedPage < 1 || oneBasedPage > pageCount)
        {
            throw new FormatException($"ページ番号が範囲外です: {oneBasedPage}");
        }
        result.Add(oneBasedPage - 1);
    }
}
