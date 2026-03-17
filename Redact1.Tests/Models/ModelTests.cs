using FluentAssertions;
using Redact1.Models;

namespace Redact1.Tests.Models;

public class RecordsRequestTests
{
    [Fact]
    public void StatusDisplay_New_ReturnsNew()
    {
        var request = new RecordsRequest { Status = "new" };
        request.StatusDisplay.Should().Be("New");
    }

    [Fact]
    public void StatusDisplay_InProgress_ReturnsInProgress()
    {
        var request = new RecordsRequest { Status = "in_progress" };
        request.StatusDisplay.Should().Be("In Progress");
    }

    [Fact]
    public void StatusDisplay_Completed_ReturnsCompleted()
    {
        var request = new RecordsRequest { Status = "completed" };
        request.StatusDisplay.Should().Be("Completed");
    }

    [Fact]
    public void StatusDisplay_Archived_ReturnsArchived()
    {
        var request = new RecordsRequest { Status = "archived" };
        request.StatusDisplay.Should().Be("Archived");
    }

    [Fact]
    public void StatusDisplay_Unknown_ReturnsRaw()
    {
        var request = new RecordsRequest { Status = "custom_status" };
        request.StatusDisplay.Should().Be("custom_status");
    }

    [Fact]
    public void IsArchived_WithArchivedAt_ReturnsTrue()
    {
        var request = new RecordsRequest { ArchivedAt = DateTimeOffset.Now.ToUnixTimeMilliseconds() };
        request.IsArchived.Should().BeTrue();
    }

    [Fact]
    public void IsArchived_WithoutArchivedAt_ReturnsFalse()
    {
        var request = new RecordsRequest { ArchivedAt = null };
        request.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void RequestDateTime_ConvertsFromUnixTimestamp()
    {
        var timestamp = new DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var request = new RecordsRequest { RequestDate = timestamp };

        request.RequestDateTime.Should().Be(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc).ToLocalTime());
    }
}

public class EvidenceFileTests
{
    [Fact]
    public void IsImage_WithImageType_ReturnsTrue()
    {
        var file = new EvidenceFile { FileType = "image" };
        file.IsImage.Should().BeTrue();
    }

    [Fact]
    public void IsImage_WithPdfType_ReturnsFalse()
    {
        var file = new EvidenceFile { FileType = "pdf" };
        file.IsImage.Should().BeFalse();
    }

    [Fact]
    public void IsPdf_WithPdfType_ReturnsTrue()
    {
        var file = new EvidenceFile { FileType = "pdf" };
        file.IsPdf.Should().BeTrue();
    }

    [Fact]
    public void IsPdf_WithImageType_ReturnsFalse()
    {
        var file = new EvidenceFile { FileType = "image" };
        file.IsPdf.Should().BeFalse();
    }

    [Fact]
    public void FileSizeDisplay_Bytes_FormatsCorrectly()
    {
        var file = new EvidenceFile { FileSize = 500 };
        file.FileSizeDisplay.Should().Be("500 B");
    }

    [Fact]
    public void FileSizeDisplay_Kilobytes_FormatsCorrectly()
    {
        var file = new EvidenceFile { FileSize = 2048 };
        file.FileSizeDisplay.Should().Be("2.0 KB");
    }

    [Fact]
    public void FileSizeDisplay_Megabytes_FormatsCorrectly()
    {
        var file = new EvidenceFile { FileSize = 2 * 1024 * 1024 };
        file.FileSizeDisplay.Should().Be("2.0 MB");
    }
}

public class DetectionTests
{
    [Fact]
    public void DisplayName_Face_ReturnsFace()
    {
        var detection = new Detection { DetectionType = "face" };
        detection.DisplayName.Should().Be("Face");
    }

    [Fact]
    public void DisplayName_Plate_ReturnsLicensePlate()
    {
        var detection = new Detection { DetectionType = "plate" };
        detection.DisplayName.Should().Be("License Plate");
    }

    [Fact]
    public void DisplayName_Ssn_ReturnsSSN()
    {
        var detection = new Detection { DetectionType = "ssn" };
        detection.DisplayName.Should().Be("SSN");
    }

    [Fact]
    public void DisplayName_Phone_ReturnsPhoneNumber()
    {
        var detection = new Detection { DetectionType = "phone" };
        detection.DisplayName.Should().Be("Phone Number");
    }

    [Fact]
    public void DisplayName_Email_ReturnsEmailAddress()
    {
        var detection = new Detection { DetectionType = "email" };
        detection.DisplayName.Should().Be("Email Address");
    }

    [Fact]
    public void DisplayName_Address_ReturnsAddress()
    {
        var detection = new Detection { DetectionType = "address" };
        detection.DisplayName.Should().Be("Address");
    }

    [Fact]
    public void DisplayName_Dob_ReturnsDateOfBirth()
    {
        var detection = new Detection { DetectionType = "dob" };
        detection.DisplayName.Should().Be("Date of Birth");
    }

    [Fact]
    public void DisplayName_Unknown_ReturnsRaw()
    {
        var detection = new Detection { DetectionType = "custom_type" };
        detection.DisplayName.Should().Be("custom_type");
    }

    [Fact]
    public void HasBoundingBox_WithAllValues_ReturnsTrue()
    {
        var detection = new Detection
        {
            BboxX = 10,
            BboxY = 20,
            BboxWidth = 100,
            BboxHeight = 50
        };
        detection.HasBoundingBox.Should().BeTrue();
    }

    [Fact]
    public void HasBoundingBox_WithMissingValue_ReturnsFalse()
    {
        var detection = new Detection
        {
            BboxX = 10,
            BboxY = 20,
            BboxWidth = 100
            // Missing BboxHeight
        };
        detection.HasBoundingBox.Should().BeFalse();
    }

    [Fact]
    public void ConfidenceDisplay_WithValue_FormatsAsPercentage()
    {
        var detection = new Detection { Confidence = 0.95 };
        detection.ConfidenceDisplay.Should().Be("95%");
    }

    [Fact]
    public void ConfidenceDisplay_WithoutValue_ReturnsNA()
    {
        var detection = new Detection { Confidence = null };
        detection.ConfidenceDisplay.Should().Be("N/A");
    }
}

public class UserTests
{
    [Fact]
    public void IsSupervisor_WithSupervisorRole_ReturnsTrue()
    {
        var user = new User { Role = "supervisor" };
        user.IsSupervisor.Should().BeTrue();
    }

    [Fact]
    public void IsSupervisor_WithAdminRole_ReturnsTrue()
    {
        var user = new User { Role = "admin" };
        user.IsSupervisor.Should().BeTrue();
    }

    [Fact]
    public void IsSupervisor_WithClerkRole_ReturnsFalse()
    {
        var user = new User { Role = "clerk" };
        user.IsSupervisor.Should().BeFalse();
    }

    [Fact]
    public void RoleDisplay_Clerk_ReturnsClerk()
    {
        var user = new User { Role = "clerk" };
        user.RoleDisplay.Should().Be("Clerk");
    }

    [Fact]
    public void RoleDisplay_Supervisor_ReturnsSupervisor()
    {
        var user = new User { Role = "supervisor" };
        user.RoleDisplay.Should().Be("Supervisor");
    }

    [Fact]
    public void RoleDisplay_Admin_ReturnsAdministrator()
    {
        var user = new User { Role = "admin" };
        user.RoleDisplay.Should().Be("Administrator");
    }
}

public class ExportTests
{
    [Fact]
    public void CreatedDateTime_ConvertsFromUnixTimestamp()
    {
        var timestamp = new DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var export = new Export { CreatedAt = timestamp };

        export.CreatedDateTime.Should().Be(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc).ToLocalTime());
    }
}

public class AuditLogTests
{
    [Fact]
    public void ActionDisplay_Create_ReturnsCreated()
    {
        var log = new AuditLog { Action = "create" };
        log.ActionDisplay.Should().Be("Created");
    }

    [Fact]
    public void ActionDisplay_Update_ReturnsUpdated()
    {
        var log = new AuditLog { Action = "update" };
        log.ActionDisplay.Should().Be("Updated");
    }

    [Fact]
    public void ActionDisplay_Delete_ReturnsDeleted()
    {
        var log = new AuditLog { Action = "delete" };
        log.ActionDisplay.Should().Be("Deleted");
    }

    [Fact]
    public void ActionDisplay_Upload_ReturnsUploaded()
    {
        var log = new AuditLog { Action = "upload" };
        log.ActionDisplay.Should().Be("Uploaded");
    }

    [Fact]
    public void ActionDisplay_Download_ReturnsDownloaded()
    {
        var log = new AuditLog { Action = "download" };
        log.ActionDisplay.Should().Be("Downloaded");
    }

    [Fact]
    public void ActionDisplay_Export_ReturnsExported()
    {
        var log = new AuditLog { Action = "export" };
        log.ActionDisplay.Should().Be("Exported");
    }

    [Fact]
    public void ActionDisplay_Approve_ReturnsApproved()
    {
        var log = new AuditLog { Action = "approve" };
        log.ActionDisplay.Should().Be("Approved");
    }

    [Fact]
    public void ActionDisplay_Reject_ReturnsRejected()
    {
        var log = new AuditLog { Action = "reject" };
        log.ActionDisplay.Should().Be("Rejected");
    }

    [Fact]
    public void ActionDisplay_Archive_ReturnsArchived()
    {
        var log = new AuditLog { Action = "archive" };
        log.ActionDisplay.Should().Be("Archived");
    }

    [Fact]
    public void ActionDisplay_Unarchive_ReturnsUnarchived()
    {
        var log = new AuditLog { Action = "unarchive" };
        log.ActionDisplay.Should().Be("Unarchived");
    }

    [Fact]
    public void ActionDisplay_Unknown_ReturnsRaw()
    {
        var log = new AuditLog { Action = "custom_action" };
        log.ActionDisplay.Should().Be("custom_action");
    }
}
