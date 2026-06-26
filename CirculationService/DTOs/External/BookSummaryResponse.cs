namespace CirculationService.DTOs.External;

/// <summary>
/// Thông tin sách lấy từ Nhóm 1 (Catalog Service) để hiển thị khi tạo phiếu mượn
/// </summary>
public class BookSummaryResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public int AvailableCopies { get; set; }
    public bool IsAvailable { get; set; }
}
