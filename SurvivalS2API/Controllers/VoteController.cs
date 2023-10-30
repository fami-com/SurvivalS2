using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurvivalS2API.Models;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SurvivalS2API.Controllers;

public record VoteInfo(int Choice, bool? IsActive);
public record ManualVoteInfo(int Points, string? Description);
public record ChangeManualVote(int? Points, string? Description);

public record Vote(long By, long For, string ByName, string ForName, int Night, int Choice, int Points, bool IsActive)
{
    public Vote(Models.Vote vote) : this(vote.ByPlayer.DiscordId, vote.ForPlayer.DiscordId, vote.ByPlayer.Name,
        vote.ForPlayer.Name, vote.OnNight.Id, vote.Choice.Rank, vote.Choice.Points, vote.IsActive)
    { }
    public static implicit operator Vote(Models.Vote vote) => new(vote);
}

public record ManualVote(int Id, long For, int Night, int Points, string Description)
{
    public ManualVote(Models.ManualVote manualVote) : this(manualVote.Id, manualVote.ForPlayer.DiscordId, manualVote.OnNight.Id,
        manualVote.Points, manualVote.Description) { }

    public static implicit operator ManualVote(Models.ManualVote vote) => new(vote);
}

public record VoteResponse(Vote Vote);

public record VotesResponse(int Count, List<Vote> Votes)
{
    public VotesResponse(List<Vote> votes) : this(votes.Count, votes) { }
}

public record ManualVoteResponse(ManualVote ManualVote);

public record ManualVotesResponse(int Count, List<ManualVote> ManualVotes)
{
    public ManualVotesResponse(List<ManualVote> manualVotes) : this(manualVotes.Count, manualVotes) { }
}

public record FullVotesResponse(List<Vote> Votes, List<ManualVote> ManualVotes);

[ApiController]
[Route("vote")]
public class VoteController : ControllerBase
{ 
    [NonAction]
    private async Task<((List<Vote> by, List<Vote> @for, List<Vote> all, List<ManualVote> manual)?, IActionResult? err)>
        GetAllVotes(long playerId, int nightId)
    {
        await using var ctx = new Context();
        
        var night = await ctx.Nights.FirstOrDefaultAsync(ng => ng.Id == nightId);
        if (night is null) return (null, NotFound(ErrorResponse.NightNotFound(nightId)));

        var player = await ctx.Players.Include(player => player.VotesFor).ThenInclude(vote => vote.OnNight)
            .Include(player => player.ManualVotes).ThenInclude(manualVote => manualVote.OnNight)
            .Include(player => player.VotesBy).ThenInclude(vote => vote.OnNight)
            .FirstOrDefaultAsync(p => p.DiscordId == playerId);
        if (player is null) return (null, NotFound(ErrorResponse.PlayerNotFound(playerId)));

        var votesBy = player.VotesBy.Where(v => v.OnNight.Id == nightId).Select(v => new Vote(v)).ToList();
        var votesFor = player.VotesFor.Where(v => v.OnNight.Id == nightId).Select(v => new Vote(v)).ToList();
        var votes = ctx.Votes.Where(v => v.OnNight.Id == nightId).Select(v => new Vote(v)).ToList();
        var manualVotes = player.ManualVotes.Where(m => m.OnNight.Id == nightId).Select(v => new ManualVote(v)).ToList();

        return ((votesBy, votesFor, votes, manualVotes), null);
    }
    
    [HttpGet("{nightId:int}/{playerId:long}/by")]
    [ProducesResponseType(typeof(VotesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBy(int nightId, long playerId)
    {
        var (maybe, err) = await GetAllVotes(playerId, nightId);
        if (maybe is { } v)
            return Ok(new VotesResponse(v.by));
        return err ?? BadRequest(ErrorResponse.UnknownError());
    }

    [HttpGet("{nightId:int}/{playerId:long}/for")]
    [ProducesResponseType(typeof(VotesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFor(int nightId, long playerId)
    {
        var (maybe, err) = await GetAllVotes(playerId, nightId);
        if (maybe is { } v)
            return Ok(new VotesResponse(v.@for));
        return err ?? BadRequest(ErrorResponse.UnknownError());
    }

    [HttpGet("{nightId:int}/{playerId:long}/manual")]
    [ProducesResponseType(typeof(ManualVotesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManual(int nightId, long playerId)
    {
        var (maybe, err) = await GetAllVotes(playerId, nightId);
        if (maybe is { } v)
            return Ok(new ManualVotesResponse(v.manual));
        return err ?? BadRequest(ErrorResponse.UnknownError());
    }

    [HttpGet("{nightId:int}/{playerId:long}/all")]
    [ProducesResponseType(typeof(FullVotesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(int nightId, long playerId)
    {
        var (maybe, err) = await GetAllVotes(playerId, nightId);
        if (maybe is { } v)
            return Ok(new VotesResponse(v.all));
        return err ?? BadRequest(ErrorResponse.UnknownError());
    }
    
    [HttpGet("{nightId:int}/{playerId:long}/full")]
    [ProducesResponseType(typeof(FullVotesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFull(int nightId, long playerId)
    {
        var (maybe, err) = await GetAllVotes(playerId, nightId);
        if (maybe is { } v)
            return Ok(new FullVotesResponse(v.all, v.manual));
        return err ?? BadRequest(ErrorResponse.UnknownError());
    }
    
    [HttpGet("{nightId:int}/{byId:long}/{forId:long}", Name = "GetVote")]
    [ProducesResponseType(typeof(VoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVote(int nightId, long byId, long forId)
    {
        await using var ctx = new Context();

        var night = await ctx.Nights.FirstOrDefaultAsync(ng => ng.Id == nightId);
        if (night is null) return NotFound(ErrorResponse.NightNotFound(nightId));

        var byPlayer = await ctx.Players.FirstOrDefaultAsync(p => p.DiscordId == byId);
        if (byPlayer is null) return NotFound(ErrorResponse.PlayerNotFound(byId));
        
        var forPlayer = await ctx.Players.FirstOrDefaultAsync(p => p.DiscordId == forId);
        if (forPlayer is null) return NotFound(ErrorResponse.PlayerNotFound(forId));

        var vote = await ctx.Votes.FirstOrDefaultAsync(v =>
            v.OnNight.Id == night.Id &&
            v.ByPlayer.DiscordId == byId &&
            v.ForPlayer.DiscordId == forId);

        if (vote is null) return NotFound(ErrorResponse.VoteNotFound(byId, forId, night.Id));
        
        return Ok(new VoteResponse(vote));
    }
    
    [HttpPut("{nightId:int}/{byId:long}/{forId:long}", Name = "VoteFor")]
    [ProducesResponseType(typeof(VoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(VoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VoteFor(int nightId, long byId, long forId, [FromBody] VoteInfo voteInfo)
    {
        await using var ctx = new Context();

        var night = await ctx.Nights.FirstOrDefaultAsync(ng => ng.Id == nightId);
        if (night is null) return NotFound(ErrorResponse.NightNotFound(nightId));
        
        var byPlayer = await ctx.Players.FirstOrDefaultAsync(p => p.DiscordId == byId);
        if (byPlayer is null) return NotFound(ErrorResponse.PlayerNotFound(byId));

        var forPlayer = await ctx.Players.FirstOrDefaultAsync(p => p.DiscordId == forId);
        if (forPlayer is null) return NotFound(ErrorResponse.PlayerNotFound(forId));

        var choice = await ctx.Choices.FirstOrDefaultAsync(c => c.Rank == voteInfo.Choice);
        if (choice is null) return NotFound(ErrorResponse.ChoiceNotFound(voteInfo.Choice));

        var vote = await ctx.Votes.FirstOrDefaultAsync(v =>
            v.Choice.Rank == voteInfo.Choice &&
            v.OnNight.Id == night.Id &&
            v.ByPlayer.DiscordId == byId);

        var votes = await ctx.Votes.Include(v => v.ForPlayer).Include(v => v.Choice)
            .Where(v => v.OnNight.Id == night.Id && v.ByPlayer.DiscordId == byId)
            .ToListAsync();

        var otherVote = votes.FirstOrDefault(v => v.ForPlayer.DiscordId == forId && v.Choice.Rank != voteInfo.Choice);
        if (otherVote is not null)
            return BadRequest(ErrorResponse.DuplicateVoteRank(byId, forId, voteInfo.Choice, otherVote.ChoiceRank));

        if (vote is not null)
        {
            vote.ForPlayer = forPlayer;
            if (voteInfo.IsActive is { } a) vote.IsActive = a;
            await ctx.SaveChangesAsync();
            return Ok(new VoteResponse(vote));
        }

        var maxRank = votes.Select(v => v.Choice.Rank).DefaultIfEmpty().Max();
        if (maxRank < voteInfo.Choice - 1) return BadRequest(ErrorResponse.NonSequentialVote(byId, voteInfo.Choice, maxRank));

        vote = new Models.Vote
        {
            OnNight = night,
            Choice = choice,
            ByPlayer = byPlayer,
            ForPlayer = forPlayer,
            IsActive = voteInfo.IsActive ?? true
        };
        await ctx.Votes.AddAsync(vote);
        await ctx.SaveChangesAsync();
        
        return CreatedAtRoute("GetVote", new { nightId = night.Id, byId, forId }, new { vote = new Vote(vote) });
    }
    
    [HttpPut("{nightId:int}/{forId:long}/manual", Name = "ManualVoteFor")]
    [ProducesResponseType(typeof(ManualVoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ManualVoteFor(int nightId, long forId, [FromBody] ManualVoteInfo voteInfo)
    {
        await using var ctx = new Context();
        
        var night = await ctx.Nights.FirstOrDefaultAsync(ng => ng.Id == nightId);
        if (night is null) return NotFound(ErrorResponse.NightNotFound(nightId));

        var forPlayer = await ctx.Players.FirstOrDefaultAsync(p => p.DiscordId == forId);
        if (forPlayer is null) return NotFound(ErrorResponse.PlayerNotFound(forId));
        
        var vote = new Models.ManualVote
        {
            OnNight = night,
            ForPlayer = forPlayer,
            Points = voteInfo.Points,
            Description = voteInfo.Description ?? ""
        };
        await ctx.ManualVotes.AddAsync(vote);
        await ctx.SaveChangesAsync();

        return CreatedAtRoute("GetManual", new { id = vote.Id }, new ManualVoteResponse(vote));
    }
    
    [HttpDelete("{nightId:int}/{byId:long}/{forId:long}", Name = "Unvote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unvote(int nightId, long byId, long forId)
    {
        await using var ctx = new Context();
        
        var night = await ctx.Nights.FirstOrDefaultAsync(ng => ng.Id == nightId);
        if (night is null) return NotFound(ErrorResponse.NightNotFound(nightId));

        var byPlayer = await ctx.Players.FirstOrDefaultAsync(p => p.DiscordId == byId);
        if (byPlayer is null) return NotFound(ErrorResponse.PlayerNotFound(byId));

        var forPlayer = await ctx.Players.FirstOrDefaultAsync(p => p.DiscordId == forId);
        if (forPlayer is null) return NotFound(ErrorResponse.PlayerNotFound(forId));
        
        var vote = await ctx.Votes.Include(v => v.Choice).FirstOrDefaultAsync(v =>
            v.ByPlayer.DiscordId == byId &&
            v.ForPlayer.DiscordId == forId &&
            v.OnNight.Id == night.Id);

        if (vote is null) return NotFound(ErrorResponse.VoteNotFound(forId, byId, night.Id));
        
        await ctx.Votes.Where(v =>
            v.ByPlayer.DiscordId == byId &&
            v.Choice.Rank > vote.Choice.Rank).ForEachAsync(v => v.ChoiceRank -= 1);
        
        ctx.Votes.Remove(vote);
        await ctx.SaveChangesAsync();
        
        return NoContent();
    }
    
    [HttpGet("manual/{id:int}", Name = "GetManual")]
    [ProducesResponseType(typeof(ManualVoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManual(int id)
    {
        await using var context = new Context();

        var vote = await context.ManualVotes.FirstOrDefaultAsync(v => v.Id == id);
        if (vote is null) return NotFound(ErrorResponse.ManualVoteNotFound(id));

        return Ok(new ManualVoteResponse(vote));
    }
    
    [HttpDelete("manual/{id:int}", Name = "UnvoteManual")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnvoteManual(int id)
    {
        await using var context = new Context();

        var vote = await context.ManualVotes.FirstOrDefaultAsync(v => v.Id == id);

        if (vote is null) return NotFound(ErrorResponse.ManualVoteNotFound(id));

        context.ManualVotes.Remove(vote);
        await context.SaveChangesAsync();
        
        return NoContent();
    }
    
    [HttpPatch("manual/{id:int}", Name = "ChangeManual")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeManual(int id, [FromBody] ChangeManualVote info)
    {
        await using var context = new Context();

        var vote = await context.ManualVotes.FirstOrDefaultAsync(v => v.Id == id);

        if (vote is null) return NotFound(ErrorResponse.ManualVoteNotFound(id));

        if (info.Points is { } p) vote.Points = p;
        if (info.Description is { } d) vote.Description = d;
        
        await context.SaveChangesAsync();
        
        return NoContent();
    }
}