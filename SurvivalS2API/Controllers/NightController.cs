using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurvivalS2API.Models;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SurvivalS2API.Controllers;

public record Night(int Number, int State, string Status)
{
    public Night(Models.Night night) : this(night.Id, (int)night.Status, night.StatusDesc) {}
    public static implicit operator Night(Models.Night night) => new(night);
}

public record NightResponse(Night Night);
public record NightsResponse(List<Night> Nights);

public record NightPlayersResponse
{
    public record NightPlayersPart(int Count, List<Player> Players);

    public record NightPlayers(int Count, NightPlayersPart Alive, NightPlayersPart Dead);

    public int Night { get; }
    public NightPlayers Players { get; }

    public NightPlayersResponse(int night, List<Player> alive, List<Player> dead)
    {
        Night = night;
        var alivePlayers = new NightPlayersPart(alive.Count, alive);
        var deadPlayers = new NightPlayersPart(dead.Count, dead);
        Players = new NightPlayers(alive.Count + dead.Count, alivePlayers, deadPlayers);
    }
}


public class NightSummaryResponse
{
    public record TotalSummary(List<int> Today, List<int> Manual, List<int> NoVotes, Dictionary<int, List<int>> Choices,
        List<int> Players, List<int> Total);
    
    // ReSharper disable once CollectionNeverQueried.Global
    public Dictionary<string, PlayerSummary> Summary { get; init; }
    public TotalSummary Total { get; init; }
    public List<string> Dead { get; init; }

    public NightSummaryResponse(Models.Night night, List<Models.Player> players, List<Models.Vote> votes,
        List<Models.ManualVote> manualVotes, List<Models.Night> nights)
    {
        Dead = new List<string>(1);
        Summary = new Dictionary<string, PlayerSummary>();

        var cnt = players.Count;

        var aToday = new List<int>(cnt);
        var aManual = new List<int>(cnt);
        var aNoVotes = new List<int>(cnt);
        var aChoices = new Dictionary<int, List<int>>();
        var aPlayers = new List<int>(cnt);
        var aTotal = new List<int>(cnt);
        
        foreach (var player in players.OrderBy(p => p.Name))
        {
            var sum = new PlayerSummary(player, night, votes, manualVotes, nights);
            
            foreach (var (k, v) in sum.Choices)
            {
                if (!aChoices.ContainsKey(k))
                {
                    aChoices[k] = new List<int>(cnt);
                }
                aChoices[k].Add(v);
            }
            
            aToday.Add(sum.Today);
            aManual.Add(sum.Manual);
            aNoVotes.Add(sum.NoVotes);
            aPlayers.Add(sum.Players);
            aTotal.Add(sum.Total);

            Summary[player.Name] = sum;
        }
        
        Total = new TotalSummary(Utils.NLargest(aToday, 3), Utils.NLargest(aManual, 3), Utils.NLargest(aNoVotes, 3),
            aChoices.ToDictionary(p => p.Key, v => Utils.NLargest(v.Value, 3)), Utils.NLargest(aPlayers, 3),
            Utils.NLargest(aTotal, 3));
    }
}

[ApiController]
[Route("night")]
public class NightController : ControllerBase
{
    [HttpGet(Name = "GetNights")]
    [ProducesResponseType(typeof(NightResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNights([FromQuery] int? status)
    {
        await using var context = new Context();

        List<Night> nights;
        if (status is { } s)
        {
            nights = await context.Nights.Where(n => n.Status == (Models.Night.State)s).Select(n => new Night(n))
                .ToListAsync();
        }
        else
        {
            nights = await context.Nights.Select(n => new Night(n)).ToListAsync();
        }

        return Ok(new NightsResponse(nights));
    }
    
    [HttpGet("{nightIndex:int}", Name = "GetSpecificNight")]
    [ProducesResponseType(typeof(NightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpecificNight(int nightIndex)
    {
        await using var context = new Context();

        var night = await context.Nights.FirstOrDefaultAsync(ng => ng.Id == nightIndex);
        if (night == null) return NotFound(ErrorResponse.NightNotFound(nightIndex));

        return Ok(new NightResponse(night));
    }
    
    [HttpGet("current", Name = "GetCurrentNight")]
    [ProducesResponseType(typeof(NightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCurrentNight()
    {
        await using var context = new Context();

        var night = await context.Nights.FirstOrDefaultAsync(ng => ng.Status == Models.Night.State.Current);
        if (night is null) return BadRequest(ErrorResponse.NoCurrentNight());

        return Ok(new NightResponse(night));
    }
    
    [HttpPost("next", Name = "NextNight")]
    [ProducesResponseType(typeof(NightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NightResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> NextNight()
    {
        await using var context = new Context();
        
        var night = await context.Nights.FirstOrDefaultAsync(ng => ng.Status == Models.Night.State.Current);
        
        var nextId = 1;
        if (night is not null)
        {
            night.Status = Models.Night.State.Ended;
            nextId = night.Id + 1;
        }

        var nextNight = await context.Nights.FirstOrDefaultAsync(n => n.Id == nextId);
        if (nextNight is null)
        {
            nextNight = new Models.Night { Id = nextId, Status = Models.Night.State.Current };
            await context.AddAsync(nextNight);
            await context.SaveChangesAsync();
            return CreatedAtRoute("GetSpecificNight", new { nightIndex = nextId }, new NightResponse(nextNight));
        }

        nextNight.Status = Models.Night.State.Current;
        await context.SaveChangesAsync();
        
        return Ok(new NightResponse(nextNight));
    }
    
    [HttpPost("prev", Name = "PrevNight")]
    [ProducesResponseType(typeof(NightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PrevNight()
    {
        await using var context = new Context();
        
        var night = await context.Nights.FirstOrDefaultAsync(ng => ng.Status == Models.Night.State.Current);
        if (night is null) return BadRequest(ErrorResponse.NoCurrentNight());
        
        night.Status = Models.Night.State.NotStarted;

        var prevNight = await context.Nights.FirstOrDefaultAsync(ng => ng.Id == night.Id - 1);
        if (prevNight is null)
        {
            await context.SaveChangesAsync();
            return NoContent();
        }
        
        prevNight.Status = Models.Night.State.Current;
        await context.SaveChangesAsync();
        return Ok(new NightResponse(prevNight));
    }

    [NonAction]
    private static async Task<object> GetPlayersLogic(IQueryable<Models.Player> players, Models.Night night)
    {
        Console.WriteLine(night.Id);
        var alive = await players.Where(p => p.DiedOn == null || p.DiedOn >= night.Id).Select(p => new Player(p)).ToListAsync();
        var dead = await players.Where(p => p.DiedOn != null && p.DiedOn < night.Id).Select(p => new Player(p)).ToListAsync();

        return new NightPlayersResponse(night.Id, alive, dead);
    }
    
    [HttpGet("{nightIndex:int}/players", Name = "GetNightPlayers")]
    [ProducesResponseType(typeof(NightPlayersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayers(int nightIndex)
    {
        await using var context = new Context();
        
        var night = await context.Nights.FirstOrDefaultAsync(ng => ng.Id == nightIndex);
        if (night == null) return NotFound(ErrorResponse.NoCurrentNight());

        var players = context.Players;

        return Ok(await GetPlayersLogic(players, night));
    }
    
    [HttpGet("current/players", Name = "GetCurrentNightPlayers")]
    [ProducesResponseType(typeof(NightPlayersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCurrentPlayers()
    {
        await using var context = new Context();
        
        var night = await context.Nights.FirstOrDefaultAsync(ng => ng.Status == Models.Night.State.Current);
        if (night == null) return BadRequest(ErrorResponse.NoCurrentNight());

        var players = context.Players.Where(p => p.DiedOn == null || p.DiedOn > night.Id);

        return Ok(await GetPlayersLogic(players, night));
    }
    
    [HttpGet("{nightId:int}/summary")]
    [ProducesResponseType(typeof(NightSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary(int nightId)
    {
        await using var ctx = new Context();

        var night = await ctx.Nights.FirstOrDefaultAsync(ng => ng.Id == nightId);
        if (night is null) return NotFound(ErrorResponse.NightNotFound(nightId));

        var nights = await ctx.Nights.ToListAsync();

        var votes = await ctx.Votes.Include(v => v.Choice).Include(v => v.OnNight)
            .Include(v => v.ByPlayer).Include(v => v.ForPlayer).ToListAsync();
        var manualVotes = await ctx.ManualVotes.Where(m => m.OnNight.Id == nightId).ToListAsync();
        var players =  await ctx.Players.Include(p => p.VotesFor).ThenInclude(v => v.Choice)
            .Where(p => p.DiedOn == null || p.DiedOn >= night.Id).ToListAsync();

        var summary = new NightSummaryResponse(night, players, votes, manualVotes, nights);
        return Ok(summary);
    }
}