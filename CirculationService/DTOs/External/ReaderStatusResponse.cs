namespace CirculationService.DTOs.External;

public class ReaderStatusResponse
{
    public Guid ReaderId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsLocked { get; set; }

    public string UserStatus { get; set; } = string.Empty;

    public string? CardStatus { get; set; }

    public DateTime? CardExpiredDate { get; set; }

    public bool IsCardExpired { get; set; }
}