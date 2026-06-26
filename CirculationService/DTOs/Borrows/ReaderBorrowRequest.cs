namespace CirculationService.DTOs.Borrows;

public class ReaderBorrowRequest
{
    public Guid BookId { get; set; }

    public int RequestedDays { get; set; } = 14;
}
