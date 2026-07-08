using System.IO;
using System.Windows.Media.Imaging;
using PdfSharpCore.Pdf;
using SkiaSharp;

namespace PdfToolbox;

internal static class PdfRenderer
{
    public static byte[] SaveDocumentToBytes(PdfDocument document)
    {
        using var ms = new MemoryStream();
        document.Save(ms, false);
        return ms.ToArray();
    }

    public static SKBitmap RenderPageBitmap(byte[] pdfBytes, int pageIndex)
    {
        return PDFtoImage.Conversion.ToImage(pdfBytes, password: null, page: pageIndex);
    }

    public static BitmapSource ToBitmapSource(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream();
        data.SaveTo(ms);
        ms.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = ms;
        bitmapImage.EndInit();
        bitmapImage.Freeze();
        return bitmapImage;
    }

    public static void SavePng(SKBitmap bitmap, string path)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var fs = File.Create(path);
        data.SaveTo(fs);
    }
}
