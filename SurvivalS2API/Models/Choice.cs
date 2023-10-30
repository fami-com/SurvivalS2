using System.ComponentModel.DataAnnotations;

namespace SurvivalS2API.Models;

#pragma warning disable CS8618
public class Choice
{
    [Key] public int Rank { get; set; }
    public int Points { get; set; }

    public override string ToString()
    {
        return $"{nameof(Rank)}: {Rank}, {nameof(Points)}: {Points}";
    }
}