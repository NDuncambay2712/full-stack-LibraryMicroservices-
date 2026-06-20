namespace CirculationService.DTOs.External;

public class BookAvailabilityResponse
{
    public Guid BookId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int AvailableCopies { get; set; }

    public bool IsAvailable { get; set; }

    public string Status { get; set; } = string.Empty;
}