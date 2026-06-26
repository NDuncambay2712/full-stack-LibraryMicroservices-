namespace CirculationService.DTOs.External;

/// <summary>
/// Thông tin độc giả lấy từ Nhóm 3 (Identity Service) để hiển thị khi tạo phiếu mượn
/// </summary>
public class ReaderSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public bool IsCardExpired { get; set; }
}
