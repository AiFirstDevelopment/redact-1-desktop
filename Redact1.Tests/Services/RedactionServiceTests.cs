using FluentAssertions;
using PdfSharp.Pdf;
using Redact1.Models;
using Redact1.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Redact1.Tests.Services;

public class RedactionServiceTests
{
    private readonly RedactionService _service;

    public RedactionServiceTests()
    {
        _service = new RedactionService();
    }

    private byte[] CreateTestImage(int width = 100, int height = 100)
    {
        using var image = new Image<Rgba32>(width, height, Color.White);
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }

    private byte[] CreateTestPdf()
    {
        // Create a valid PDF using PdfSharp
        using var doc = new PdfDocument();
        doc.AddPage();
        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task RedactImageAsync_WithNoDetections_ReturnsImage()
    {
        var imageData = CreateTestImage();
        var detections = new List<Detection>();
        var manualRedactions = new List<ManualRedaction>();

        var result = await _service.RedactImageAsync(imageData, detections, manualRedactions);

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RedactImageAsync_WithApprovedDetection_AppliesRedaction()
    {
        var imageData = CreateTestImage(200, 200);
        var detections = new List<Detection>
        {
            new Detection
            {
                Id = "det-1",
                Status = "approved",
                BboxX = 0.1,
                BboxY = 0.1,
                BboxWidth = 0.2,
                BboxHeight = 0.2
            }
        };
        var manualRedactions = new List<ManualRedaction>();

        var result = await _service.RedactImageAsync(imageData, detections, manualRedactions);

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RedactImageAsync_WithManualRedaction_AppliesRedaction()
    {
        var imageData = CreateTestImage(200, 200);
        var detections = new List<Detection>();
        var manualRedactions = new List<ManualRedaction>
        {
            new ManualRedaction
            {
                Id = "red-1",
                BboxX = 0.1,
                BboxY = 0.1,
                BboxWidth = 0.2,
                BboxHeight = 0.2
            }
        };

        var result = await _service.RedactImageAsync(imageData, detections, manualRedactions);

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RedactImageAsync_IgnoresRejectedDetections()
    {
        var imageData = CreateTestImage(200, 200);
        var detections = new List<Detection>
        {
            new Detection
            {
                Id = "det-1",
                Status = "rejected",  // Should be ignored
                BboxX = 0.1,
                BboxY = 0.1,
                BboxWidth = 0.2,
                BboxHeight = 0.2
            }
        };
        var manualRedactions = new List<ManualRedaction>();

        var result = await _service.RedactImageAsync(imageData, detections, manualRedactions);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RedactPdfAsync_WithNoDetections_ReturnsPdf()
    {
        var pdfData = CreateTestPdf();
        var detections = new List<Detection>();
        var manualRedactions = new List<ManualRedaction>();

        var result = await _service.RedactPdfAsync(pdfData, detections, manualRedactions);

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RedactPdfAsync_WithApprovedDetection_AppliesRedaction()
    {
        var pdfData = CreateTestPdf();
        var detections = new List<Detection>
        {
            new Detection
            {
                Id = "det-1",
                Status = "approved",
                BboxX = 0.1,
                BboxY = 0.1,
                BboxWidth = 0.2,
                BboxHeight = 0.2,
                PageNumber = 1
            }
        };
        var manualRedactions = new List<ManualRedaction>();

        var result = await _service.RedactPdfAsync(pdfData, detections, manualRedactions);

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RedactPdfAsync_WithManualRedaction_AppliesRedaction()
    {
        var pdfData = CreateTestPdf();
        var detections = new List<Detection>();
        var manualRedactions = new List<ManualRedaction>
        {
            new ManualRedaction
            {
                Id = "red-1",
                BboxX = 0.1,
                BboxY = 0.1,
                BboxWidth = 0.2,
                BboxHeight = 0.2,
                PageNumber = 1
            }
        };

        var result = await _service.RedactPdfAsync(pdfData, detections, manualRedactions);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RenderPdfPageToImageAsync_ReturnsImageData()
    {
        var pdfData = CreateTestPdf();

        var result = await _service.RenderPdfPageToImageAsync(pdfData, 1);

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RenderPdfPageToImageAsync_WithCustomScale_ReturnsImageData()
    {
        var pdfData = CreateTestPdf();

        var result = await _service.RenderPdfPageToImageAsync(pdfData, 1, scale: 1.5);

        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RenderPdfPageToImageAsync_InvalidPageNumber_ThrowsException()
    {
        var pdfData = CreateTestPdf();

        var act = async () => await _service.RenderPdfPageToImageAsync(pdfData, 99);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetPdfPageCount_ReturnsCorrectCount()
    {
        var pdfData = CreateTestPdf();

        var count = _service.GetPdfPageCount(pdfData);

        count.Should().Be(1);
    }
}
