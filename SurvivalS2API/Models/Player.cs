using System.ComponentModel.DataAnnotations;

namespace SurvivalS2API.Models;

public class Player
{
    [Key] public long Id { get; set; }
    
    public required long DiscordId { get; set; }
    public required string Name { get; set; }
    
    public int? DiedOn { get; set; }
    public virtual Night? DiedOnNight { get; set; }

    public virtual List<Vote> VotesFor { get; set; } = new();
    public virtual List<Vote> VotesBy { get; set; } = new();
    public  virtual List<ManualVote> ManualVotes { get; set; } = new();

    public override string ToString()
    {
        return $"{nameof(Id)}: {Id}, {nameof(DiscordId)}: {DiscordId}, {nameof(Name)}: {Name}, {nameof(DiedOnNight)}: {DiedOnNight}";
    }
}