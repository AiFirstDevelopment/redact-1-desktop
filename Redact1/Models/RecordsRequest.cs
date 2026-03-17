using System.Text.Json.Serialization;

namespace Redact1.Models
{
    public class RecordsRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("request_number")]
        public string RequestNumber { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("request_date")]
        public long RequestDate { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "new";

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("archived_at")]
        public long? ArchivedAt { get; set; }

        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public long UpdatedAt { get; set; }

        public bool IsArchived => ArchivedAt.HasValue;

        public DateTime RequestDateTime => DateTimeOffset.FromUnixTimeMilliseconds(RequestDate).LocalDateTime;
        public DateTime CreatedDateTime => DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt).LocalDateTime;

        public string StatusDisplay => Status switch
        {
            "new" => "New",
            "in_progress" => "In Progress",
            "completed" => "Completed",
            "archived" => "Archived",
            _ => Status
        };
    }

    public enum RequestStatus
    {
        New,
        InProgress,
        Completed
    }

    public class CreateRequestPayload
    {
        [JsonPropertyName("request_number")]
        public string RequestNumber { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("request_date")]
        public long RequestDate { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class UpdateRequestPayload
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public class RequestsListResponse
    {
        [JsonPropertyName("requests")]
        public List<RecordsRequest> Requests { get; set; } = new();
    }

    public class RequestResponse
    {
        [JsonPropertyName("request")]
        public RecordsRequest Request { get; set; } = new();
    }
}
