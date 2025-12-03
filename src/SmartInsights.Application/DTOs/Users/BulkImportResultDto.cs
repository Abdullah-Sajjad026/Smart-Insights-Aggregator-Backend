namespace SmartInsights.Application.DTOs.Users;

public class BulkImportResultDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<ImportResultDetailDto> Results { get; set; } = new();
}

public class ImportResultDetailDto
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Success" or "Failure"
    public string? ErrorMessage { get; set; }
}
