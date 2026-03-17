using Redact1.Models;
using System.Text.RegularExpressions;

namespace Redact1.Services
{
    public class DetectionService : IDetectionService
    {
        // PII Detection Patterns (matching iOS implementation)
        private static readonly Regex SsnPattern = new(@"\d{3}-\d{2}-\d{4}", RegexOptions.Compiled);
        private static readonly Regex PhonePattern = new(@"(\(\d{3}\)\s?|\d{3}[-.])\d{3}[-.]?\d{4}", RegexOptions.Compiled);
        private static readonly Regex EmailPattern = new(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);
        private static readonly Regex DobPattern = new(@"(0[1-9]|1[0-2])[/\-](0[1-9]|[12]\d|3[01])[/\-](19|20)\d{2}", RegexOptions.Compiled);
        private static readonly Regex LicensePlatePattern = new(@"\b[A-Z0-9]{5,8}\b", RegexOptions.Compiled);

        public async Task<List<CreateDetectionRequest>> DetectInImageAsync(byte[] imageData)
        {
            var detections = new List<CreateDetectionRequest>();

            await Task.Run(() =>
            {
                // Face detection would require a cross-platform ML library
                // For now, we'll focus on PII detection via OCR
                // In production, consider using ML.NET or ONNX Runtime

                // Placeholder for OCR - in production use Tesseract or cloud OCR
                var text = ""; // PerformOcr would go here

                var piiDetections = DetectPiiInText(text);
                detections.AddRange(piiDetections);
            });

            return detections;
        }

        public async Task<List<CreateDetectionRequest>> DetectInPdfPageAsync(byte[] pageImageData, int pageNumber)
        {
            var detections = await DetectInImageAsync(pageImageData);

            foreach (var detection in detections)
            {
                detection.PageNumber = pageNumber;
            }

            return detections;
        }

        public Task<string> ExtractTextAsync(byte[] imageData)
        {
            // Placeholder - would use Tesseract OCR in production
            return Task.FromResult(string.Empty);
        }

        private List<CreateDetectionRequest> DetectPiiInText(string text)
        {
            var detections = new List<CreateDetectionRequest>();

            // SSN Detection
            foreach (Match match in SsnPattern.Matches(text))
            {
                detections.Add(CreateTextDetection("ssn", match, text));
            }

            // Phone Detection
            foreach (Match match in PhonePattern.Matches(text))
            {
                detections.Add(CreateTextDetection("phone", match, text));
            }

            // Email Detection
            foreach (Match match in EmailPattern.Matches(text))
            {
                detections.Add(CreateTextDetection("email", match, text));
            }

            // DOB Detection
            foreach (Match match in DobPattern.Matches(text))
            {
                detections.Add(CreateTextDetection("dob", match, text));
            }

            // License Plate Detection
            foreach (Match match in LicensePlatePattern.Matches(text))
            {
                var value = match.Value;
                bool hasLetter = value.Any(char.IsLetter);
                bool hasDigit = value.Any(char.IsDigit);

                if (hasLetter && hasDigit && value.Length >= 5 && value.Length <= 8)
                {
                    detections.Add(CreateTextDetection("plate", match, text));
                }
            }

            return detections;
        }

        private CreateDetectionRequest CreateTextDetection(string type, Match match, string fullText)
        {
            return new CreateDetectionRequest
            {
                DetectionType = type,
                TextContent = match.Value,
                TextStart = match.Index,
                TextEnd = match.Index + match.Length,
                Confidence = 0.95
            };
        }
    }
}
