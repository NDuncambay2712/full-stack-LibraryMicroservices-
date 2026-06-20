namespace CirculationService.DTOs.Borrows;

public class InvoiceResponse
{
    public Guid Id { get; set; }
    public Guid BorrowRecordId { get; set; }
    public Guid ReaderId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
