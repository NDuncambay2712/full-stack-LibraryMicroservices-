namespace CirculationService.DTOs.Borrows;

public class BorrowResponse
{
    public Guid Id { get; set; }

    public Guid ReaderId { get; set; }

    public string ReaderName { get; set; } = string.Empty;

    public Guid BookId { get; set; }

    public string BookTitle { get; set; } = string.Empty;

    public DateTime BorrowDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal FineAmount { get; set; }

    public bool IsFinePaid { get; set; }

    public bool IsOverdue { get; set; }
}