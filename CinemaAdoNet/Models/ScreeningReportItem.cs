namespace CinemaAdoNet.Models;

public class ScreeningReportItem
{
    public int ScreeningId { get; set; }
    public string FilmTitle { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string HallName { get; set; } = string.Empty;
    public decimal TicketPrice { get; set; }
    public int SoldTicketsCount { get; set; }
    public decimal TotalRevenue { get; set; }
}
