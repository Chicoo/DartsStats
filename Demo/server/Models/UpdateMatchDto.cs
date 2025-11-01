namespace DartsStats.Api.Models
{
    public class UpdateMatchDto
    {
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public DateTime MatchDate { get; set; }
        public int Player1Score { get; set; }
        public int Player2Score { get; set; }
        public double Player1Average { get; set; }
        public double Player2Average { get; set; }
        public int Player1180s { get; set; }
        public int Player2180s { get; set; }
        public int Player1HighestCheckout { get; set; }
        public int Player2HighestCheckout { get; set; }
        public string Season { get; set; } = string.Empty;
        public string Round { get; set; } = string.Empty;
    }
}
