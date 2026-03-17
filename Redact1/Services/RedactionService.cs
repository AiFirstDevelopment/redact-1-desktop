using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Redact1.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Redact1.Services
{
    public class RedactionService : IRedactionService
    {
        public async Task<byte[]> RedactImageAsync(byte[] imageData, List<Detection> detections, List<ManualRedaction> manualRedactions)
        {
            return await Task.Run(() =>
            {
                using var image = Image.Load<Rgba32>(imageData);
                var width = image.Width;
                var height = image.Height;

                image.Mutate(ctx =>
                {
                    // Draw approved detections as black boxes
                    foreach (var detection in detections.Where(d => d.Status == "approved" && d.HasBoundingBox))
                    {
                        var rect = new RectangleF(
                            (float)(detection.BboxX!.Value * width),
                            (float)(detection.BboxY!.Value * height),
                            (float)(detection.BboxWidth!.Value * width),
                            (float)(detection.BboxHeight!.Value * height)
                        );
                        ctx.Fill(Color.Black, rect);
                    }

                    // Draw manual redactions as black boxes
                    foreach (var redaction in manualRedactions.Where(r => r.BboxX.HasValue))
                    {
                        var rect = new RectangleF(
                            (float)(redaction.BboxX!.Value * width),
                            (float)(redaction.BboxY!.Value * height),
                            (float)(redaction.BboxWidth!.Value * width),
                            (float)(redaction.BboxHeight!.Value * height)
                        );
                        ctx.Fill(Color.Black, rect);
                    }
                });

                using var output = new MemoryStream();
                image.Save(output, new JpegEncoder { Quality = 90 });
                return output.ToArray();
            });
        }

        public async Task<byte[]> RedactPdfAsync(byte[] pdfData, List<Detection> detections, List<ManualRedaction> manualRedactions)
        {
            return await Task.Run(() =>
            {
                using var inputStream = new MemoryStream(pdfData);
                var inputDoc = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
                var outputDoc = new PdfDocument();

                for (int pageIndex = 0; pageIndex < inputDoc.PageCount; pageIndex++)
                {
                    var pageNumber = pageIndex + 1;
                    var inputPage = inputDoc.Pages[pageIndex];
                    var outputPage = outputDoc.AddPage(inputPage);

                    using var gfx = XGraphics.FromPdfPage(outputPage);

                    var pageWidth = outputPage.Width.Point;
                    var pageHeight = outputPage.Height.Point;

                    // Draw approved detections for this page
                    var pageDetections = detections.Where(d =>
                        d.Status == "approved" &&
                        d.HasBoundingBox &&
                        (d.PageNumber == null || d.PageNumber == pageNumber));

                    foreach (var detection in pageDetections)
                    {
                        var rect = new XRect(
                            detection.BboxX!.Value * pageWidth,
                            detection.BboxY!.Value * pageHeight,
                            detection.BboxWidth!.Value * pageWidth,
                            detection.BboxHeight!.Value * pageHeight
                        );
                        gfx.DrawRectangle(XBrushes.Black, rect);
                    }

                    // Draw manual redactions for this page
                    var pageRedactions = manualRedactions.Where(r =>
                        r.BboxX.HasValue &&
                        (r.PageNumber == null || r.PageNumber == pageNumber));

                    foreach (var redaction in pageRedactions)
                    {
                        var rect = new XRect(
                            redaction.BboxX!.Value * pageWidth,
                            redaction.BboxY!.Value * pageHeight,
                            redaction.BboxWidth!.Value * pageWidth,
                            redaction.BboxHeight!.Value * pageHeight
                        );
                        gfx.DrawRectangle(XBrushes.Black, rect);
                    }
                }

                using var outputStream = new MemoryStream();
                outputDoc.Save(outputStream);
                return outputStream.ToArray();
            });
        }

        public async Task<byte[]> RenderPdfPageToImageAsync(byte[] pdfData, int pageNumber, double scale = 2.0)
        {
            return await Task.Run(() =>
            {
                using var inputStream = new MemoryStream(pdfData);
                var doc = PdfReader.Open(inputStream, PdfDocumentOpenMode.ReadOnly);

                if (pageNumber < 1 || pageNumber > doc.PageCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(pageNumber));
                }

                var page = doc.Pages[pageNumber - 1];
                var width = (int)(page.Width.Point * scale);
                var height = (int)(page.Height.Point * scale);

                // Create a placeholder image (PDF rendering requires platform-specific libraries)
                // In production, use PDFium or similar for actual PDF rendering
                using var image = new Image<Rgba32>(width, height, Color.White);

                image.Mutate(ctx =>
                {
                    // Draw placeholder text
                    // In production, this would render actual PDF content
                });

                using var output = new MemoryStream();
                image.SaveAsPng(output);
                return output.ToArray();
            });
        }

        public int GetPdfPageCount(byte[] pdfData)
        {
            using var stream = new MemoryStream(pdfData);
            var doc = PdfReader.Open(stream, PdfDocumentOpenMode.InformationOnly);
            return doc.PageCount;
        }
    }
}
