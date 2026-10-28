namespace CinemaAdoNet.Models;

public class ScreeningScheduleItem
{
    public int ScreeningId { get; set; }
    public string FilmTitle { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string HallName { get; set; } = string.Empty;
    public decimal TicketPrice { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
}
