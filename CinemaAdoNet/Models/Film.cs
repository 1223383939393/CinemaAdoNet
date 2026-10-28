namespace CinemaAdoNet.Models;

public class Film
{
    public int FilmId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int AgeRating { get; set; }
    public int ReleaseYear { get; set; }
    public string? Description { get; set; }
}
