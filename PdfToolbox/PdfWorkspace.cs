using System;
using System.Collections.Generic;
using System.IO;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SkiaSharp;

namespace PdfToolbox;

internal sealed class PdfWorkspace
{
    private PdfDocument _document = new();

    public string? FileName { get; private set; }

    public int PageCount => _document.PageCount;

    public void Load(string path)
    {
        _document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        FileName = Path.GetFileName(path);
    }

    public System.Windows.Media.Imaging.BitmapSource RenderPage(int pageIndex)
    {
        var bytes = PdfRenderer.SaveDocumentToBytes(_document);
        using var bitmap = PdfRenderer.RenderPageBitmap(bytes, pageIndex);
        return PdfRenderer.ToBitmapSource(bitmap);
    }

    public void Rotate(IEnumerable<int> pageIndices, int degrees)
    {
        foreach (var index in pageIndices)
        {
            var page = _document.Pages[index];
            var current = page.Elements.GetInteger("/Rotate");
            var updated = ((current + degrees) % 360 + 360) % 360;
            page.Elements.SetInteger("/Rotate", updated);
        }
    }

    public void SplitCenter(IEnumerable<int> pageIndices)
    {
        var targets = new HashSet<int>(pageIndices);
        if (targets.Count == 0)
        {
            return;
        }

        var bytes = PdfRenderer.SaveDocumentToBytes(_document);
        var newDocument = new PdfDocument();

        for (var i = 0; i < _document.PageCount; i++)
        {
            if (!targets.Contains(i))
            {
                newDocument.AddPage(_document.Pages[i]);
                continue;
            }

            var original = _document.Pages[i];
            var width = original.Width.Point;
            var height = original.Height.Point;
            var halfWidth = width / 2;

            var leftPage = newDocument.AddPage();
            leftPage.Width = XUnit.FromPoint(halfWidth);
            leftPage.Height = XUnit.FromPoint(height);
            using (var form = XPdfForm.FromStream(new MemoryStream(bytes)))
            {
                form.PageNumber = i + 1;
                using var gfx = XGraphics.FromPdfPage(leftPage);
                gfx.DrawImage(form, 0, 0, width, height);
            }

            var rightPage = newDocument.AddPage();
            rightPage.Width = XUnit.FromPoint(halfWidth);
            rightPage.Height = XUnit.FromPoint(height);
            using (var form = XPdfForm.FromStream(new MemoryStream(bytes)))
            {
                form.PageNumber = i + 1;
                using var gfx = XGraphics.FromPdfPage(rightPage);
                gfx.DrawImage(form, -halfWidth, 0, width, height);
            }
        }

        _document = newDocument;
    }

    public List<PdfDocument> SplitByRanges(List<(int Start, int End)> ranges)
    {
        var results = new List<PdfDocument>();
        foreach (var range in ranges)
        {
            var doc = new PdfDocument();
            for (var i = range.Start; i <= range.End; i++)
            {
                doc.AddPage(_document.Pages[i]);
            }
            results.Add(doc);
        }
        return results;
    }

    public void MergeAppend(string otherPdfPath)
    {
        var other = PdfReader.Open(otherPdfPath, PdfDocumentOpenMode.Import);
        for (var i = 0; i < other.PageCount; i++)
        {
            _document.AddPage(other.Pages[i]);
        }
    }

    public void ExportPng(IEnumerable<int> pageIndices, string folderPath)
    {
        var bytes = PdfRenderer.SaveDocumentToBytes(_document);
        var digits = _document.PageCount.ToString().Length;
        foreach (var index in pageIndices)
        {
            using var bitmap = PdfRenderer.RenderPageBitmap(bytes, index);
            var fileName = $"page_{(index + 1).ToString().PadLeft(digits, '0')}.png";
            PdfRenderer.SavePng(bitmap, Path.Combine(folderPath, fileName));
        }
    }

    public void SaveAs(string path)
    {
        _document.Save(path);
    }
}
