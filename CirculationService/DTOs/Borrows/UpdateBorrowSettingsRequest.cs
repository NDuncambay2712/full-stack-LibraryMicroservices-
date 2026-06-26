using System.ComponentModel.DataAnnotations;

namespace CirculationService.DTOs.Borrows;

public class UpdateBorrowSettingsRequest
{
    /// <summary>Số sách tối đa một độc giả được mượn cùng lúc (1–20)</summary>
    [Range(1, 20, ErrorMessage = "MaxBorrowingBooks phải từ 1 đến 20")]
    public int MaxBorrowingBooks { get; set; }

    /// <summary>Tiền phạt mỗi ngày trễ hạn tính bằng VNĐ (0–500000)</summary>
    [Range(0, 500_000, ErrorMessage = "FinePerLateDay phải từ 0 đến 500.000 VNĐ")]
    public decimal FinePerLateDay { get; set; }
}
