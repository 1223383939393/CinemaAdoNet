namespace CinemaAdoNet.Models;

public class CustomerTicketItem
{
    public int TicketId { get; set; }
    public string FilmTitle { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public string HallName { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; }
}
