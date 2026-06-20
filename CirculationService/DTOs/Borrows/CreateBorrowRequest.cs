namespace CirculationService.DTOs.Borrows;

public class CreateBorrowRequest
{
    public Guid ReaderId { get; set; }

    public Guid BookId { get; set; }

    public DateTime? BorrowDate { get; set; }

    public DateTime DueDate { get; set; }
}