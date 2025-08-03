using DartsStats.Api.Models;

namespace DartsStats.Api.Services
{
    public class DataSeedService
    {
        private readonly List<Player> _players;
        private readonly List<Match> _matches;

        public DataSeedService()
        {
            _players = SeedPlayers();
            _matches = SeedMatches();
        }

        public List<Player> GetPlayers() => _players;
        public List<Match> GetMatches() 
        {
            // Populate player objects in matches
            foreach (var match in _matches)
            {
                match.Player1 = _players.FirstOrDefault(p => p.Id == match.Player1Id);
                match.Player2 = _players.FirstOrDefault(p => p.Id == match.Player2Id);
            }
            return _matches;
        }

        private List<Player> SeedPlayers()
        {
            return new List<Player>
            {
                new Player { Id = 1, Name = "Luke Littler", Nickname = "Cool Hand", Country = "England" },
                new Player { Id = 2, Name = "Luke Humphries", Nickname = "Bully Boy", Country = "England" },
                new Player { Id = 3, Name = "Gerwyn Price", Nickname = "The Iceman", Country = "Wales" },
                new Player { Id = 4, Name = "Nathan Aspinall", Nickname = "The Asp", Country = "England" },
                new Player { Id = 5, Name = "Michael van Gerwen", Nickname = "Mighty Mike", Country = "Netherlands" },
                new Player { Id = 6, Name = "Chris Dobey", Nickname = "Hollywood", Country = "England" },
                new Player { Id = 7, Name = "Rob Cross", Nickname = "Voltage", Country = "England" },
                new Player { Id = 8, Name = "Stephen Bunting", Nickname = "The Bullet", Country = "England" }
            };
        }

        private List<Match> SeedMatches()
        {
            var random = new Random(42); // Fixed seed for consistent data
            var matches = new List<Match>();
            var matchId = 1;

            // Premier League 2025 Final matches (based on real data)
            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 1, // Luke Littler
                Player2Id = 3, // Gerwyn Price
                MatchDate = DateTime.SpecifyKind(new DateTime(2025, 5, 29), DateTimeKind.Utc),
                Player1Score = 10,
                Player2Score = 7,
                Player1Average = 104.64,
                Player2Average = 95.37,
                Player1180s = 8,
                Player2180s = 4,
                Player1HighestCheckout = 170,
                Player2HighestCheckout = 121,
                Season = "2025",
                Round = "Semi-Final"
            });

            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 2, // Luke Humphries
                Player2Id = 4, // Nathan Aspinall
                MatchDate = DateTime.SpecifyKind(new DateTime(2025, 5, 29), DateTimeKind.Utc),
                Player1Score = 10,
                Player2Score = 7,
                Player1Average = 105.81,
                Player2Average = 101.76,
                Player1180s = 9,
                Player2180s = 7,
                Player1HighestCheckout = 164,
                Player2HighestCheckout = 141,
                Season = "2025",
                Round = "Semi-Final"
            });

            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 2, // Luke Humphries
                Player2Id = 1, // Luke Littler
                MatchDate = DateTime.SpecifyKind(new DateTime(2025, 5, 29), DateTimeKind.Utc),
                Player1Score = 11,
                Player2Score = 8,
                Player1Average = 97.86,
                Player2Average = 100.29,
                Player1180s = 6,
                Player2180s = 8,
                Player1HighestCheckout = 156,
                Player2HighestCheckout = 180,
                Season = "2025",
                Round = "Final"
            });

            // Generate additional Premier League regular season matches
            var playerIds = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            
            for (int week = 1; week <= 16; week++)
            {
                var weekDate = DateTime.SpecifyKind(new DateTime(2025, 2, 6), DateTimeKind.Utc).AddDays((week - 1) * 7); // Starting Feb 6, 2025
                var roundName = $"Night {week}";
                
                // Each week has 4 matches (8 players, round-robin style)
                var weekPlayerIds = playerIds.OrderBy(x => random.Next()).Take(8).ToList();
                
                for (int match = 0; match < 4; match++)
                {
                    var player1Id = weekPlayerIds[match * 2];
                    var player2Id = weekPlayerIds[match * 2 + 1];
                    
                    var player1Wins = random.Next(0, 2) == 1;
                    var player1Score = player1Wins ? random.Next(6, 9) : random.Next(3, 6);
                    var player2Score = player1Wins ? random.Next(3, 6) : random.Next(6, 9);
                    
                    // Ensure different scores
                    if (player1Score == player2Score)
                    {
                        if (player1Wins) player1Score++;
                        else player2Score++;
                    }

                    matches.Add(new Match
                    {
                        Id = matchId++,
                        Player1Id = player1Id,
                        Player2Id = player2Id,
                        MatchDate = weekDate,
                        Player1Score = player1Score,
                        Player2Score = player2Score,
                        Player1Average = Math.Round(85 + random.NextDouble() * 25, 2), // 85-110 average
                        Player2Average = Math.Round(85 + random.NextDouble() * 25, 2),
                        Player1180s = random.Next(0, 12),
                        Player2180s = random.Next(0, 12),
                        Player1HighestCheckout = random.Next(80, 181),
                        Player2HighestCheckout = random.Next(80, 181),
                        Season = "2025",
                        Round = roundName
                    });
                }
            }

            // Add some 2024 matches for comparison
            for (int i = 0; i < 20; i++)
            {
                var player1Id = playerIds[random.Next(playerIds.Count)];
                var player2Id = playerIds[random.Next(playerIds.Count)];
                while (player2Id == player1Id)
                {
                    player2Id = playerIds[random.Next(playerIds.Count)];
                }

                var player1Wins = random.Next(0, 2) == 1;
                var player1Score = player1Wins ? random.Next(6, 9) : random.Next(3, 6);
                var player2Score = player1Wins ? random.Next(3, 6) : random.Next(6, 9);

                matches.Add(new Match
                {
                    Id = matchId++,
                    Player1Id = player1Id,
                    Player2Id = player2Id,
                    MatchDate = DateTime.SpecifyKind(new DateTime(2024, random.Next(3, 12), random.Next(1, 29)), DateTimeKind.Utc),
                    Player1Score = player1Score,
                    Player2Score = player2Score,
                    Player1Average = Math.Round(80 + random.NextDouble() * 30, 2),
                    Player2Average = Math.Round(80 + random.NextDouble() * 30, 2),
                    Player1180s = random.Next(0, 10),
                    Player2180s = random.Next(0, 10),
                    Player1HighestCheckout = random.Next(60, 181),
                    Player2HighestCheckout = random.Next(60, 181),
                    Season = "2024",
                    Round = $"Night {random.Next(1, 17)}"
                });
            }

            return matches;
        }
    }
}
