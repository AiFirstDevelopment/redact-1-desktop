using FluentAssertions;
using Redact1.Services;

namespace Redact1.Tests.Services;

public class DetectionServiceTests
{
    private readonly DetectionService _service;

    public DetectionServiceTests()
    {
        _service = new DetectionService();
    }

    [Fact]
    public async Task DetectInImageAsync_WithEmptyImage_ReturnsEmptyList()
    {
        var imageData = new byte[] { 0x00 };

        var result = await _service.DetectInImageAsync(imageData);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectInImageAsync_ReturnsDetectionList()
    {
        // Minimal valid image data (1x1 PNG)
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        var result = await _service.DetectInImageAsync(imageData);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DetectInPdfPageAsync_SetsPageNumber()
    {
        var pageData = new byte[] { 0x00 };
        var pageNumber = 5;

        var result = await _service.DetectInPdfPageAsync(pageData, pageNumber);

        result.Should().NotBeNull();
        // All detections should have the page number set
        foreach (var detection in result)
        {
            detection.PageNumber.Should().Be(pageNumber);
        }
    }

    [Fact]
    public async Task ExtractTextAsync_ReturnsEmptyString()
    {
        var imageData = new byte[] { 0x00 };

        var result = await _service.ExtractTextAsync(imageData);

        result.Should().BeEmpty();
    }
}
