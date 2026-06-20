namespace CirculationService.Models;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BorrowRecordId { get; set; }

    public Guid ReaderId { get; set; }

    public decimal Amount { get; set; } = 0;

    public string Type { get; set; } = string.Empty; 
    // "Borrow", "Return", "FinePayment"

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public BorrowRecord? BorrowRecord { get; set; }
}
