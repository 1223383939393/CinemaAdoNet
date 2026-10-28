namespace CinemaAdoNet.Models;

public class TopFilmReportItem
{
    public int FilmId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int SoldTicketsCount { get; set; }
    public decimal TotalRevenue { get; set; }
}
