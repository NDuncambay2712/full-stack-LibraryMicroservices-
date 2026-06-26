namespace CirculationService.Services;

/// <summary>
/// Singleton service lưu cấu hình quy tắc mượn trả tại runtime.
/// Được khởi tạo từ appsettings.json, Admin có thể cập nhật qua API mà
/// không cần restart service (tồn tại cho đến khi service được restart).
/// </summary>
public class BorrowSettingsService
{
    public int MaxBorrowingBooks { get; private set; }
    public decimal FinePerLateDay { get; private set; }

    public BorrowSettingsService(IConfiguration configuration)
    {
        // Khởi tạo từ appsettings.json
        MaxBorrowingBooks = int.Parse(
            configuration["BorrowSettings:MaxBorrowingBooks"] ?? "5");
        FinePerLateDay = decimal.Parse(
            configuration["BorrowSettings:FinePerLateDay"] ?? "5000");
    }

    /// <summary>Cập nhật cấu hình tại runtime (chỉ Admin)</summary>
    public void Update(int maxBorrowingBooks, decimal finePerLateDay)
    {
        MaxBorrowingBooks = maxBorrowingBooks;
        FinePerLateDay    = finePerLateDay;
    }
}
