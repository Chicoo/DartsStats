namespace DartsStats.Api.DTOs;

public record PlayerDto(
    int Id,
    string Name,
    string Nickname,
    string Country,
    int MatchesPlayed,
    int MatchesWon,
    int MatchesLost,
    int LegsWon,
    int LegsLost,
    int PointsFor,
    int PointsAgainst,
    decimal AvgPoints,
    decimal AvgLegDarts,
    decimal CheckoutPercentage,
    int Position
);
