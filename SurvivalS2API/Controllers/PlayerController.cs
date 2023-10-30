using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurvivalS2API.Models;

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SurvivalS2API.Controllers;

public record NewPlayer(long Id, [Required] string Name);
public record ChangePlayer(string Name);

public record Player(long Id, string Name, int TotalVotes, int TotalManualVotes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? DiedOn)
{
    public Player(Models.Player player) : this(
        player.DiscordId,
        player.Name,
        player.VotesFor.Where(v => v.IsActive).Sum(v => v.Choice.Points),
        player.ManualVotes.Sum(v => v.Points),
        player.DiedOn
    ) { }

    public static implicit operator Player(Models.Player player) => new(player);
}

public record PlayerResponse(Player Player);

public record PlayersResponse(int Count, List<Player> Players)
{
    public PlayersResponse(List<Player> players) : this(players.Count, players) { }
}

public class PlayerSummary
{
    public long Id { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DiedOn { get; init; }
    public int Today { get; init; }
    public int Manual { get; init; }
    public int NoVotes { get; init; }
    public Dictionary<int, int> Choices { get; init; }
    public int Players { get; init; }
    public int Total { get; init; }
    public List<Vote> Votes { get; init; }

    public PlayerSummary(Models.Player player, Models.Night night, List<Models.Vote> votes,
        List<Models.ManualVote> manualVotes, List<Models.Night> nights)
    {
        var votesFor = votes.Where(v => v.OnNight.Id == night.Id && v.ForPlayer.Id == player.Id).ToList();
        var votesByNights = votes.Where(v => v.ByPlayer.Id == player.Id).Select(v => v.OnNight.Id).Distinct().ToList();
        var allChoices = votesFor.GroupBy(v => v.Choice.Points, (k, v) => new { Key = k, Value = v.Count() })
            .ToDictionary(v => v.Key, v => v.Value);
        
        var playersCount = votesFor.Select(v => v.ByPlayer.Id).ToHashSet().Count;

        var noVoteNights = nights.Select(n => n.Id).Where(n => n <= night.Id).Except(votesByNights).ToList();
        var noVotesRuns = Utils.GetContinuousRuns(noVoteNights);
        var noVotesNorm = noVotesRuns.Select(r => r.Select(n => n - r[0] - 2));
        var noVotesList = noVotesNorm.Select(r => r.Select(n => (int)Math.Pow(2, n)).ToList()).ToList();
        var nvToday = votesByNights.Contains(night.Id) ? 0 : noVotesList[^1][^1];
        var nvAll = noVotesList.Sum(v => v.Sum());
            
        var manual = manualVotes.Where(v => v.ForPlayer.Id == player.Id).Sum(v => v.Points);
        var todayRaw = votesFor.Where(v => v.IsActive).Sum(v => v.Choice.Points);
        var totalRaw = player.VotesFor.Where(v => v.OnNight.Id <= night.Id).Sum(v => v.Choice.Points);
        var allVotes = votesFor.Select(v => new Vote(v)).ToList();

        var today = todayRaw + manual + nvToday;
        var total = totalRaw + nvAll;
        
        Id = player.DiscordId;
        Today = today;
        Manual = manual;
        NoVotes = nvToday;
        Choices = allChoices;
        Players = playersCount;
        Total = total;
        Votes = allVotes;
        DiedOn = player.DiedOn;
    }
}

[ApiController]
[Route("player")]
public class PlayerController : ControllerBase
{
    [HttpGet(Name = "GetPlayers")]
    [ProducesResponseType(typeof(PlayerResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlayers()
    {
        await using var context = new Context();

        var players = await context.Players.Include(p => p.VotesFor).ThenInclude(v => v.Choice).Include(p => p.ManualVotes)
            .Select(p => new Player(p)).ToListAsync();

        return Ok(new PlayersResponse(players));
    }

    [HttpPost(Name = "AddPlayer")]
    [ProducesResponseType(typeof(PlayerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Player>> AddPlayer([FromBody] NewPlayer plr)
    {
        await using var context = new Context();

        if (plr.Name is null) return BadRequest(ErrorResponse.MissingParameter(nameof(plr.Name)));
        
        var player = await context.Players.FirstOrDefaultAsync(p => p.DiscordId == plr.Id);

        if (player is not null) return Conflict(ErrorResponse.DuplicatePlayerId(plr.Id));
        
        player = new Models.Player { DiscordId = plr.Id, Name = plr.Name, DiedOn = null };
        await context.Players.AddAsync(player);
        await context.SaveChangesAsync();
        return CreatedAtRoute("GetPlayer", new { playerId = plr.Id }, new PlayerResponse(player));
    }
    
    [HttpGet("{playerId:long}", Name = "GetPlayer")]
    [ProducesResponseType(typeof(PlayerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Player>> GetPlayer(long playerId)
    {
        await using var context = new Context();

        var player = await context.Players.Include(p => p.VotesFor).ThenInclude(v => v.Choice).Include(p => p.ManualVotes)
            .FirstOrDefaultAsync(p => p.DiscordId == playerId);
        if (player is null) return NotFound(ErrorResponse.PlayerNotFound(playerId));

        return Ok(new PlayerResponse(player));
    }
    
    [HttpDelete("{playerId:long}", Name = "DeletePlayer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Player>> DeletePlayer(long playerId)
    {
        await using var context = new Context();

        var player = await context.Players.Include(p => p.VotesFor).ThenInclude(v => v.Choice).Include(p => p.ManualVotes)
            .FirstOrDefaultAsync(p => p.DiscordId == playerId);
        if (player is null) return NotFound(ErrorResponse.PlayerNotFound(playerId));

        context.Players.Remove(player);
        await context.SaveChangesAsync();
        
        return Ok(new PlayerResponse(player));
    }
    
    [HttpPatch("{playerId:long}", Name = "PatchPlayer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Player>> PatchPlayer(long playerId, [FromBody] ChangePlayer plr)
    {
        await using var context = new Context();

        var player = await context.Players.FirstOrDefaultAsync(p => p.DiscordId == playerId);

        if (player is null) return NotFound(ErrorResponse.PlayerNotFound(playerId));

        player.Name = plr.Name;
        await context.SaveChangesAsync();
        return NoContent();
    }
    
    
    [HttpGet("{playerId:long}/summary", Name = "PlayerSummary")]
    [ProducesResponseType(typeof(PlayerResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Player>> PlayerSummary(long playerId, [FromQuery(Name = "night")] int? nightId)
    {
        await using var context = new Context();

        var player = await context.Players.FirstOrDefaultAsync(p => p.DiscordId == playerId);

        if (player is null) return NotFound(ErrorResponse.PlayerNotFound(playerId));

        await context.SaveChangesAsync();
        return NoContent();
    }
    
    [HttpPost("{playerId:long}/kill", Name = "KillPlayer")]
    [ProducesResponseType(typeof(PlayerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> KillPlayer(long playerId)
    {
        await using var context = new Context();

        var player = await context.Players.Include(p => p.VotesFor).ThenInclude(v => v.Choice).Include(p => p.ManualVotes)
            .FirstOrDefaultAsync(p => p.DiscordId == playerId);
        if (player is null) return NotFound(ErrorResponse.PlayerNotFound(playerId));

        var night = await context.Nights.FirstOrDefaultAsync(ng => ng.Status == Models.Night.State.Current);
        if (night is null) return BadRequest(ErrorResponse.NoCurrentNight());

        player.DiedOn = night.Id;
        await context.SaveChangesAsync();

        return Ok(new PlayerResponse(player));
    }

    [HttpPost("{playerId:long}/revive", Name = "RevivePlayer")]
    [ProducesResponseType(typeof(PlayerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevivePlayer(long playerId)
    {
        await using var context = new Context();

        var player = await context.Players.Include(p => p.VotesFor).ThenInclude(v => v.Choice).Include(p => p.ManualVotes)
            .FirstOrDefaultAsync(p => p.DiscordId == playerId);
        if (player == null) return NotFound(ErrorResponse.PlayerNotFound(playerId));

        player.DiedOn = null;
        await context.SaveChangesAsync();

        return Ok(new PlayerResponse(player));
    }
}