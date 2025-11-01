namespace DartsStats.Api.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int MatchesPlayed { get; set; }
        public int MatchesWon { get; set; }
        public int MatchesLost { get; set; }
        public int LegsWon { get; set; }
        public int LegsLost { get; set; }
        public int PointsFor { get; set; }
        public int PointsAgainst { get; set; }
        public decimal AvgPoints { get; set; }
        public decimal AvgLegDarts { get; set; }
        public decimal CheckoutPercentage { get; set; }
        public int Position { get; set; }
    }
}
