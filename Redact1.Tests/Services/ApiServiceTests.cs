using FluentAssertions;
using Moq;
using Moq.Protected;
using Redact1.Models;
using Redact1.Services;
using System.Net;
using System.Text.Json;

namespace Redact1.Tests.Services;

public class ApiServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly ApiService _apiService;

    public ApiServiceTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://test.api.com")
        };
        _apiService = new ApiService(_httpClient);
    }

    private void SetupResponse<T>(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json)
            });
    }

    [Fact]
    public void SetAuthToken_SetsAuthorizationHeader()
    {
        _apiService.SetAuthToken("test-token");

        _httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        _httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        _httpClient.DefaultRequestHeaders.Authorization.Parameter.Should().Be("test-token");
    }

    [Fact]
    public void SetAuthToken_WithNull_ClearsHeader()
    {
        _apiService.SetAuthToken("test-token");
        _apiService.SetAuthToken(null);

        _httpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ReturnsLoginResponse()
    {
        var expectedResponse = new LoginResponse
        {
            Token = "jwt-token",
            User = new User { Id = "user-1", Email = "test@test.com" }
        };
        SetupResponse(expectedResponse);

        var result = await _apiService.LoginAsync(new LoginRequest
        {
            Email = "test@test.com",
            Password = "password"
        });

        result.Token.Should().Be("jwt-token");
        result.User.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsUser()
    {
        var expectedUser = new User { Id = "user-1", Name = "Test User" };
        SetupResponse(expectedUser);

        var result = await _apiService.GetCurrentUserAsync();

        result.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task GetRequestsAsync_ReturnsRequests()
    {
        var response = new RequestsListResponse
        {
            Requests = new List<RecordsRequest>
            {
                new RecordsRequest { Id = "req-1", Title = "Request 1" },
                new RecordsRequest { Id = "req-2", Title = "Request 2" }
            }
        };
        SetupResponse(response);

        var result = await _apiService.GetRequestsAsync();

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Request 1");
    }

    [Fact]
    public async Task GetRequestsAsync_WithStatusFilter_SendsCorrectQuery()
    {
        SetupResponse(new RequestsListResponse());

        await _apiService.GetRequestsAsync(status: "in_progress");

        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.RequestUri!.ToString().Contains("status=in_progress")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetRequestAsync_ReturnsRequest()
    {
        var response = new RequestResponse
        {
            Request = new RecordsRequest { Id = "req-1", Title = "Test Request" }
        };
        SetupResponse(response);

        var result = await _apiService.GetRequestAsync("req-1");

        result.Title.Should().Be("Test Request");
    }

    [Fact]
    public async Task CreateRequestAsync_ReturnsCreatedRequest()
    {
        var expectedRequest = new RecordsRequest { Id = "new-req", Title = "New Request" };
        SetupResponse(expectedRequest);

        var result = await _apiService.CreateRequestAsync(new CreateRequestPayload
        {
            Title = "New Request",
            RequestNumber = "FOIA-001"
        });

        result.Title.Should().Be("New Request");
    }

    [Fact]
    public async Task GetFilesAsync_ReturnsFiles()
    {
        var response = new FilesListResponse
        {
            Files = new List<EvidenceFile>
            {
                new EvidenceFile { Id = "file-1", Filename = "test.pdf" }
            }
        };
        SetupResponse(response);

        var result = await _apiService.GetFilesAsync("req-1");

        result.Should().HaveCount(1);
        result[0].Filename.Should().Be("test.pdf");
    }

    [Fact]
    public async Task GetDetectionsAsync_ReturnsDetections()
    {
        var response = new DetectionListResponse
        {
            Detections = new List<Detection>
            {
                new Detection { Id = "det-1", DetectionType = "ssn" }
            },
            ManualRedactions = new List<ManualRedaction>()
        };
        SetupResponse(response);

        var result = await _apiService.GetDetectionsAsync("file-1");

        result.Detections.Should().HaveCount(1);
        result.Detections[0].DetectionType.Should().Be("ssn");
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsUsers()
    {
        var users = new List<User>
        {
            new User { Id = "user-1", Name = "User 1" },
            new User { Id = "user-2", Name = "User 2" }
        };
        SetupResponse(users);

        var result = await _apiService.GetUsersAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateRequestAsync_ReturnsUpdatedRequest()
    {
        var updatedRequest = new RecordsRequest { Id = "req-1", Title = "Updated Title" };
        SetupResponse(updatedRequest);

        var result = await _apiService.UpdateRequestAsync("req-1", new UpdateRequestPayload
        {
            Title = "Updated Title"
        });

        result.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsCreatedUser()
    {
        var newUser = new User { Id = "new-user", Name = "New User" };
        SetupResponse(newUser);

        var result = await _apiService.CreateUserAsync(new CreateUserRequest
        {
            Name = "New User",
            Email = "new@test.com"
        });

        result.Name.Should().Be("New User");
    }

    [Fact]
    public async Task UpdateDetectionAsync_ReturnsUpdatedDetection()
    {
        var updated = new Detection { Id = "det-1", Status = "approved" };
        SetupResponse(updated);

        var result = await _apiService.UpdateDetectionAsync("det-1", new UpdateDetectionRequest
        {
            Status = "approved"
        });

        result.Status.Should().Be("approved");
    }

    [Fact]
    public async Task GetExportsAsync_ReturnsExports()
    {
        var exports = new List<Export>
        {
            new Export { Id = "exp-1", Filename = "export.zip" }
        };
        SetupResponse(exports);

        var result = await _apiService.GetExportsAsync("req-1");

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateExportAsync_ReturnsExport()
    {
        var export = new Export { Id = "exp-1", Filename = "export.zip" };
        SetupResponse(export);

        var result = await _apiService.CreateExportAsync("req-1");

        result.Filename.Should().Be("export.zip");
    }
}
