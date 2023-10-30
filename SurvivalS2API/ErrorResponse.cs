using System.Text.Json.Serialization;

namespace SurvivalS2API;

public class ErrorResponse
{
    public enum ErrorCode
    {
        UnknownError = -1, 
        NoError = 0,
        ChoiceNotFound,
        NightNotFound,
        NoCurrentNight,
        PlayerNotFound,
        MalformedParameter,
        VoteNotFound,
        DuplicateVoteRank,
        NonSequentialVote,
        ManualVoteNotFound,
        DuplicatePlayerId,
        MissingParameter,
    }

    public class HttpError
    {
        public ErrorCode Code { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public object? Metadata { get; init; }

        // ReSharper disable once UnusedMember.Global
        public string Description => Code switch
        {
            ErrorCode.NoError => "No Error",
            ErrorCode.ChoiceNotFound => "Choice Not Found",
            ErrorCode.NightNotFound => "Night Not Found",
            ErrorCode.NoCurrentNight => "No Current Night",
            ErrorCode.PlayerNotFound => "Player Not Found",
            ErrorCode.MalformedParameter => "Malformed Parameter",
            ErrorCode.VoteNotFound => "Vote Not Found",
            ErrorCode.DuplicateVoteRank => "Duplicate Vote Rank",
            ErrorCode.NonSequentialVote => "Non-Sequential Vote",
            ErrorCode.ManualVoteNotFound => "Manual Vote Not Found",
            ErrorCode.DuplicatePlayerId => "Duplicate Player Id",
            ErrorCode.MissingParameter => "Missing Parameter",
            ErrorCode.UnknownError => "Unknown Error",
            _ => "Unknown"
        };
    }

    public HttpError Error { get; init; }

    public ErrorResponse(ErrorCode code, object? metadata = null)
    {
        Error = new HttpError { Code = code, Metadata = metadata };
    }

    public static ErrorResponse NoError() => new(ErrorCode.NoError);

    public static ErrorResponse ChoiceNotFound(int choiceId) => new(ErrorCode.ChoiceNotFound, new { choiceId });

    public static ErrorResponse NightNotFound(int nightId) => new(ErrorCode.NightNotFound, new { nightId });

    public static ErrorResponse NoCurrentNight() => new(ErrorCode.NoCurrentNight);

    public static ErrorResponse PlayerNotFound(long playerId) => new(ErrorCode.PlayerNotFound, new { playerId });

    public static ErrorResponse MalformedParameter(string parameter) =>
        new(ErrorCode.MalformedParameter, new { parameter });

    public static ErrorResponse VoteNotFound(long forId, long byId, int night) =>
        new(ErrorCode.VoteNotFound, new { forId, byId, night });

    public static ErrorResponse DuplicateVoteRank(long byId, long forId, int choice, int otherChoice) =>
        new(ErrorCode.DuplicateVoteRank, new { byId, forId, choice, otherChoice });

    public static ErrorResponse NonSequentialVote(long byId, int rank, int prevRank) =>
        new(ErrorCode.NonSequentialVote, new { byId, rank, prevRank });

    public static ErrorResponse ManualVoteNotFound(int manualVoteId) =>
        new(ErrorCode.ManualVoteNotFound, new { manualVoteId });
    
    public static ErrorResponse DuplicatePlayerId(long id) =>
        new(ErrorCode.DuplicatePlayerId, new { id });
    
    public static ErrorResponse MissingParameter(string paramName) =>
        new(ErrorCode.MissingParameter, new { paramName });

    public static ErrorResponse UnknownError(object? metadata = null) =>
        new(ErrorCode.MissingParameter, metadata);
}