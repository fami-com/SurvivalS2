using System.ComponentModel.DataAnnotations;

namespace SurvivalS2API.Models;

public class ManualVote
{
    [Key] public int Id { get; set; }
    
    public required int Points { get; set; }
    public string Description { get; set; } = "";
    
    public virtual required Night OnNight { get; set; }
    public virtual required Player ForPlayer { get; set; }

    public override string ToString()
    {
        return $"{nameof(Id)}: {Id}, {nameof(Points)}: {Points}, {nameof(Description)}: {Description}, {nameof(OnNight)}: {OnNight}, {nameof(ForPlayer)}: {ForPlayer}";
    }
}