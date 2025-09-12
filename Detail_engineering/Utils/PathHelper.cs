using Detail_engineering;
using System;
using System.IO;
using System.Linq;

public static class PathHelper
{
    // ست کن: مثلا @"\\filesrv01\Projects"
    public static string BaseDir { get; set; } = @"C:\ProjectDocs";

    public static string BuildRelatedPath(DocumentRecord doc,bool fm,string rev)
    {
        string disc = SafeSeg(doc?.Dicipline);
        string dtype = SafeSeg(doc?.Document_type);
        string dnum = SafeSeg(doc?.Document_number);
        string dname = SafeSeg(doc?.Document_name);
        string combo = string.IsNullOrWhiteSpace(dnum) ? dname : (string.IsNullOrWhiteSpace(dname) ? dnum : $"{dnum}-{dname}");
        combo = SafeSeg(combo);
        string last = (fm) ? SafeSeg(GetLastRevision(doc)) : rev;
        // Path.Combine با UNC هم اوکی است اگر BaseDir با \\ شروع کند
        var parts = new[] { BaseDir, disc, dtype, combo, last }.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        return Path.Combine(parts);
    }

    public static string GetLastRevision(DocumentRecord doc)
    {
        var revs = doc?.Revisions ?? new System.Collections.Generic.List<string>();
        if (revs.Count == 0) return "";
        return (revs[^1] ?? "").Trim();
    }

    private static string SafeSeg(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(s.Select(ch => invalid.Contains(ch) ? ' ' : ch).ToArray());
        cleaned = cleaned.Trim();
        while (cleaned.EndsWith(".")) cleaned = cleaned[..^1];
        return cleaned;
    }
}
