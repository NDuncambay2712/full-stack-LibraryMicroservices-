namespace CirculationService.Models;

public class BorrowRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ReaderId { get; set; }

    public string ReaderName { get; set; } = string.Empty;

    public Guid BookId { get; set; }

    public string BookTitle { get; set; } = string.Empty;

    public DateTime BorrowDate { get; set; } = DateTime.UtcNow;

    public DateTime DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public string Status { get; set; } = "Borrowed";
    // Borrowed, Returned

    public decimal FineAmount { get; set; } = 0;

    public bool IsFinePaid { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}