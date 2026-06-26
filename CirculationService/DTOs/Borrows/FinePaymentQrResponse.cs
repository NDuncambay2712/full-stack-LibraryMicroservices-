namespace CirculationService.DTOs.Borrows;

public class FinePaymentQrResponse
{
    /// <summary>ID phiếu mượn</summary>
    public Guid BorrowRecordId { get; set; }

    /// <summary>Tên độc giả</summary>
    public string ReaderName { get; set; } = string.Empty;

    /// <summary>Tên sách</summary>
    public string BookTitle { get; set; } = string.Empty;

    /// <summary>Số tiền phạt cần thanh toán (VNĐ)</summary>
    public decimal FineAmount { get; set; }

    /// <summary>Nội dung chuyển khoản</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Mã ngân hàng (VD: MB, VCB, TCB...)</summary>
    public string BankId { get; set; } = string.Empty;

    /// <summary>Số tài khoản thư viện</summary>
    public string AccountNo { get; set; } = string.Empty;

    /// <summary>Tên chủ tài khoản thư viện</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>
    /// URL ảnh QR VietQR — nhúng trực tiếp vào img src là hiển thị được.
    /// Ví dụ: &lt;img src="{QrImageUrl}" /&gt;
    /// </summary>
    public string QrImageUrl { get; set; } = string.Empty;
}
