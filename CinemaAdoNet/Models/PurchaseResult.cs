namespace CinemaAdoNet.Models;

public class PurchaseResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? TicketId { get; set; }
    public string? FilmTitle { get; set; }
    public DateTime? StartsAt { get; set; }
    public int? SeatNumber { get; set; }
    public decimal? Price { get; set; }
    public string? HallName { get; set; }

    public static PurchaseResult Ok(int ticketId, string filmTitle, DateTime startsAt, int seatNumber, decimal price, string hallName) =>
        new()
        {
            Success = true,
            Message = "Билет успешно оформлен!",
            TicketId = ticketId,
            FilmTitle = filmTitle,
            StartsAt = startsAt,
            SeatNumber = seatNumber,
            Price = price,
            HallName = hallName
        };

    public static PurchaseResult Fail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}
