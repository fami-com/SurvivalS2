using System.ComponentModel.DataAnnotations;

namespace SurvivalS2API.Models;

#pragma warning disable CS8618
public class Vote
{
    [Key] public int Id { get; set; }

    public bool IsActive { get; set; } = false;
    
    public int ChoiceRank { get; set; }

    public virtual required Choice Choice { get; set; }
    public virtual required Night OnNight { get; set; }
    public virtual required Player ByPlayer { get; set; }
    public virtual required Player ForPlayer { get; set; }

    public override string ToString()
    {
        return $"{nameof(Id)}: {Id}, {nameof(IsActive)}: {IsActive}, {nameof(Choice)}: {Choice}, {nameof(OnNight)}: {OnNight}, {nameof(ByPlayer)}: {ByPlayer}, {nameof(ForPlayer)}: {ForPlayer}";
    }
}