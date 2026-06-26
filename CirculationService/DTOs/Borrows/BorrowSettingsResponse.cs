namespace CirculationService.DTOs.Borrows;

public class BorrowSettingsResponse
{
    /// <summary>Số sách tối đa một độc giả được mượn cùng lúc</summary>
    public int MaxBorrowingBooks { get; set; }

    /// <summary>Tiền phạt mỗi ngày trễ hạn (VNĐ)</summary>
    public decimal FinePerLateDay { get; set; }
}
