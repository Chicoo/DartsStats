using DartsStats.Api.DTOs;
using DartsStats.Api.Entities;

namespace DartsStats.Api.Mappings;

public static class MappingExtensions
{
    // Player mappings
    public static PlayerDto ToDto(this PlayerEntity entity)
    {
        return new PlayerDto(
            entity.Id,
            entity.Name,
            entity.Nickname,
            entity.Country,
            entity.MatchesPlayed,
            entity.MatchesWon,
            entity.MatchesLost,
            entity.LegsWon,
            entity.LegsLost,
            entity.PointsFor,
            entity.PointsAgainst,
            entity.AvgPoints,
            entity.AvgLegDarts,
            entity.CheckoutPercentage,
            entity.Position
        );
    }

    // Match mappings
    public static MatchDto ToDto(this MatchEntity entity)
    {
        return new MatchDto(
            entity.Id,
            entity.Player1Id,
            entity.Player2Id,
            entity.Player1?.ToDto(),
            entity.Player2?.ToDto(),
            entity.MatchDate,
            entity.Player1Score,
            entity.Player2Score,
            entity.Player1Average,
            entity.Player2Average,
            entity.Player1180s,
            entity.Player2180s,
            entity.Player1HighestCheckout,
            entity.Player2HighestCheckout,
            entity.Season,
            entity.Round
        );
    }

    public static void UpdateEntity(this UpdateMatchDto dto, MatchEntity entity)
    {
        entity.Player1Id = dto.Player1Id;
        entity.Player2Id = dto.Player2Id;
        entity.MatchDate = dto.MatchDate;
        entity.Player1Score = dto.Player1Score;
        entity.Player2Score = dto.Player2Score;
        entity.Player1Average = dto.Player1Average;
        entity.Player2Average = dto.Player2Average;
        entity.Player1180s = dto.Player1180s;
        entity.Player2180s = dto.Player2180s;
        entity.Player1HighestCheckout = dto.Player1HighestCheckout;
        entity.Player2HighestCheckout = dto.Player2HighestCheckout;
        entity.Season = dto.Season;
        entity.Round = dto.Round;
    }
}
