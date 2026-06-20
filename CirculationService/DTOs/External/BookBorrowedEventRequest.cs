namespace CirculationService.DTOs.External;

public class BookBorrowedEventRequest
{
    public Guid BorrowId { get; set; }

    public Guid BookId { get; set; }

    public string BookTitle { get; set; } = string.Empty;

    public Guid ReaderId { get; set; }

    public string ReaderName { get; set; } = string.Empty;

    public DateTime BorrowDate { get; set; }

    public DateTime DueDate { get; set; }
}