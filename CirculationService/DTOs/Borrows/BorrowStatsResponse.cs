namespace CirculationService.DTOs.Borrows;

public class BorrowStatsResponse
{
    /// <summary>Tổng số phiếu mượn</summary>
    public int Total { get; set; }

    /// <summary>Số phiếu đang mượn (chưa trả)</summary>
    public int Borrowing { get; set; }

    /// <summary>Số phiếu đã trả</summary>
    public int Returned { get; set; }

    /// <summary>Số phiếu có phí phạt chưa thanh toán</summary>
    public int UnpaidFine { get; set; }

    /// <summary>Số phiếu quá hạn (đang mượn nhưng đã quá DueDate)</summary>
    public int Overdue { get; set; }

    /// <summary>Tổng tiền phạt chưa thu được</summary>
    public decimal TotalUnpaidFineAmount { get; set; }
}
