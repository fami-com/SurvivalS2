using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurvivalS2API.Models;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SurvivalS2API.Controllers;

public record NewChoice(int Rank, int Points);

public record Choice(int Rank, int Points)
{
    public Choice(Models.Choice choice) : this(choice.Rank, choice.Points) {}
    public static implicit operator Choice(Models.Choice choice) => new(choice);
}

public record ChoiceResponse(Choice Choice);

public record ChoicesResponse(int Count, List<Choice> Choices)
{
    public ChoicesResponse(List<Choice> choices) : this(choices.Count, choices) {}
}

[ApiController]
[Route("choice")]
public class ChoiceController : ControllerBase
{
    [HttpGet(Name = "GetCurrentChoices")]
    [ProducesResponseType(typeof(ChoicesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        await using var context = new Context();
        var choices = await context.Choices.OrderBy(c => c.Rank).Select(c => new Choice(c)).ToListAsync();
        return Ok(new ChoicesResponse(choices));
    }

    [HttpGet("{rank:int}", Name = "GetChoice")]
    [ProducesResponseType(typeof(ChoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChoice(int rank)
    {
        await using var context = new Context();
        var choice = await context.Choices.FirstOrDefaultAsync(c => c.Rank == rank);

        if (choice is null) return NotFound(ErrorResponse.ChoiceNotFound(rank));
        
        return Ok(new ChoiceResponse(choice));
    }

    [HttpPut(Name = "PutChoice")]
    [ProducesResponseType(typeof(ChoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Put([FromBody] NewChoice choice)
    {
        await using var context = new Context();

        var c = await context.Choices.FirstOrDefaultAsync(e => e.Rank == choice.Rank);
        if (c is null)
        {
            c = new Models.Choice { Rank = choice.Rank, Points = choice.Points };
            await context.Choices.AddAsync(c);
            await context.SaveChangesAsync();
            return CreatedAtRoute("GetChoice", new { rank = choice.Rank }, new ChoiceResponse(c));
        }

        c.Points = choice.Points;
        await context.SaveChangesAsync();
        return NoContent();
    }
}