using System.ComponentModel.DataAnnotations;

namespace SurvivalS2API.Models;

public class Night
{
    public enum State
    {
        NotStarted,
        Current,
        Ended
    }
    
    [Key] public int Id { get; set; }
    
    public State Status { get; set; }
    
    public string StatusDesc => Status switch
    {
        State.NotStarted => "Not Started",
        State.Current => "Current",
        State.Ended => "Ended",
        _ => "Unknown"
    };
    
    public virtual List<Player> Died { get; set; } = new();
    public virtual List<Vote> Votes { get; set; } = new();
    public virtual List<ManualVote> ManualVotes { get; set; } = new();

    public override string ToString()
    {
        return $"{nameof(Id)}: {Id}, {nameof(Status)}: {Status}, {nameof(StatusDesc)}: {StatusDesc}";
    }
}