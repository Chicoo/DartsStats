namespace DartsStats.Api.DTOs;

public record UpdateMatchDto(
    int Player1Id,
    int Player2Id,
    DateTime MatchDate,
    int Player1Score,
    int Player2Score,
    double Player1Average,
    double Player2Average,
    int Player1180s,
    int Player2180s,
    int Player1HighestCheckout,
    int Player2HighestCheckout,
    string Season,
    string Round
);
