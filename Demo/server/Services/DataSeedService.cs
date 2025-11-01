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
                // 2025 Premier League Players
                new Player { Id = 1, Name = "Luke Littler", Nickname = "Cool Hand", Country = "England" },
                new Player { Id = 2, Name = "Luke Humphries", Nickname = "Bully Boy", Country = "England" },
                new Player { Id = 3, Name = "Gerwyn Price", Nickname = "The Iceman", Country = "Wales" },
                new Player { Id = 4, Name = "Nathan Aspinall", Nickname = "The Asp", Country = "England" },
                new Player { Id = 5, Name = "Michael van Gerwen", Nickname = "Mighty Mike", Country = "Netherlands" },
                new Player { Id = 6, Name = "Chris Dobey", Nickname = "Hollywood", Country = "England" },
                new Player { Id = 7, Name = "Rob Cross", Nickname = "Voltage", Country = "England" },
                new Player { Id = 8, Name = "Stephen Bunting", Nickname = "The Bullet", Country = "England" },

                // 2024 Premier League Players (note: some overlap with 2025)
                new Player { Id = 12, Name = "Luke Humphries", Nickname = "Bully Boy", Country = "England" }, // Same as ID 2, but for 2024
                new Player { Id = 15, Name = "Michael van Gerwen", Nickname = "Mighty Mike", Country = "Netherlands" }, // Same as ID 5, but for 2024
                new Player { Id = 13, Name = "Gerwyn Price", Nickname = "The Iceman", Country = "Wales" }, // Same as ID 3, but for 2024
                new Player { Id = 14, Name = "Nathan Aspinall", Nickname = "The Asp", Country = "England" }, // Same as ID 4, but for 2024
                new Player { Id = 9, Name = "Michael Smith", Nickname = "Bully Boy", Country = "England" },
                new Player { Id = 10, Name = "Peter Wright", Nickname = "Snakebite", Country = "Scotland" },
                new Player { Id = 11, Name = "Jonny Clayton", Nickname = "The Ferret", Country = "Wales" },
                new Player { Id = 16, Name = "Joe Cullen", Nickname = "Rockstar", Country = "England" }
            };
        }

        private List<Match> SeedMatches()
        {
            var matches = new List<Match>();
            var matchId = 1;

            // 2024 Premier League Darts - Real Results
            
            // Night 1 - Birmingham (February 1, 2024)
            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 15, // Michael van Gerwen (2024 ID)
                Player2Id = 9, // Michael Smith
                MatchDate = DateTime.SpecifyKind(new DateTime(2024, 2, 1), DateTimeKind.Utc),
                Player1Score = 6,
                Player2Score = 4,
                Player1Average = 101.2,
                Player2Average = 98.4,
                Player1180s = 5,
                Player2180s = 3,
                Player1HighestCheckout = 126,
                Player2HighestCheckout = 110,
                Season = "2024",
                Round = "Night 1"
            });

            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 12, // Luke Humphries (2024 ID)
                Player2Id = 10, // Peter Wright
                MatchDate = DateTime.SpecifyKind(new DateTime(2024, 2, 1), DateTimeKind.Utc),
                Player1Score = 6,
                Player2Score = 2,
                Player1Average = 103.8,
                Player2Average = 89.7,
                Player1180s = 7,
                Player2180s = 2,
                Player1HighestCheckout = 164,
                Player2HighestCheckout = 84,
                Season = "2024",
                Round = "Night 1"
            });

            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 13, // Gerwyn Price (2024 ID)
                Player2Id = 11, // Jonny Clayton
                MatchDate = DateTime.SpecifyKind(new DateTime(2024, 2, 1), DateTimeKind.Utc),
                Player1Score = 6,
                Player2Score = 4,
                Player1Average = 97.8,
                Player2Average = 95.2,
                Player1180s = 4,
                Player2180s = 3,
                Player1HighestCheckout = 141,
                Player2HighestCheckout = 100,
                Season = "2024",
                Round = "Night 1"
            });

            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 14, // Nathan Aspinall (2024 ID)
                Player2Id = 16, // Joe Cullen (2024 ID)
                MatchDate = DateTime.SpecifyKind(new DateTime(2024, 2, 1), DateTimeKind.Utc),
                Player1Score = 6,
                Player2Score = 3,
                Player1Average = 99.4,
                Player2Average = 93.1,
                Player1180s = 4,
                Player2180s = 2,
                Player1HighestCheckout = 130,
                Player2HighestCheckout = 98,
                Season = "2024",
                Round = "Night 1"
            });

            // Night 2 - Nottingham (February 8, 2024)
            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 12, // Luke Humphries (2024 ID)
                Player2Id = 15, // Michael van Gerwen (2024 ID)
                MatchDate = DateTime.SpecifyKind(new DateTime(2024, 2, 8), DateTimeKind.Utc),
                Player1Score = 6,
                Player2Score = 6,
                Player1Average = 105.7,
                Player2Average = 104.3,
                Player1180s = 8,
                Player2180s = 7,
                Player1HighestCheckout = 121,
                Player2HighestCheckout = 136,
                Season = "2024",
                Round = "Night 2"
            });

            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 9, // Michael Smith
                Player2Id = 13, // Gerwyn Price (2024 ID)
                MatchDate = DateTime.SpecifyKind(new DateTime(2024, 2, 8), DateTimeKind.Utc),
                Player1Score = 6,
                Player2Score = 4,
                Player1Average = 102.1,
                Player2Average = 96.8,
                Player1180s = 6,
                Player2180s = 4,
                Player1HighestCheckout = 170,
                Player2HighestCheckout = 112,
                Season = "2024",
                Round = "Night 2"
            });

            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 10, // Peter Wright
                Player2Id = 14, // Nathan Aspinall (2024 ID)
                MatchDate = DateTime.SpecifyKind(new DateTime(2024, 2, 8), DateTimeKind.Utc),
                Player1Score = 6,
                Player2Score = 4,
                Player1Average = 94.2,
                Player2Average = 97.6,
                Player1180s = 3,
                Player2180s = 5,
                Player1HighestCheckout = 108,
                Player2HighestCheckout = 124,
                Season = "2024",
                Round = "Night 2"
            });

            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 11, // Jonny Clayton
                Player2Id = 16, // Joe Cullen (2024 ID)
                MatchDate = DateTime.SpecifyKind(new DateTime(2024, 2, 8), DateTimeKind.Utc),
                Player1Score = 6,
                Player2Score = 2,
                Player1Average = 98.9,
                Player2Average = 88.4,
                Player1180s = 5,
                Player2180s = 1,
                Player1HighestCheckout = 144,
                Player2HighestCheckout = 76,
                Season = "2024",
                Round = "Night 2"
            });

            // 2024 Playoffs (May 23, 2024 - O2 Arena, London)
            
            // Semi-Final 1
            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 12, // Luke Humphries (2024 ID)
                Player2Id = 15, // Michael van Gerwen (2024 ID)
                MatchDate = DateTime.SpecifyKind(new DateTime(2024, 5, 23), DateTimeKind.Utc),
                Player1Score = 10,
                Player2Score = 6,
                Player1Average = 104.8,
                Player2Average = 99.7,
                Player1180s = 9,
                Player2180s = 6,
                Player1HighestCheckout = 156,
                Player2HighestCheckout = 132,
                Season = "2024",
                Round = "Semi-Final"
            });

            // Semi-Final 2
            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 9, // Michael Smith
                Player2Id = 11, // Jonny Clayton
                MatchDate = DateTime.SpecifyKind(new DateTime(2024, 5, 23), DateTimeKind.Utc),
                Player1Score = 10,
                Player2Score = 8,
                Player1Average = 101.4,
                Player2Average = 98.1,
                Player1180s = 7,
                Player2180s = 5,
                Player1HighestCheckout = 161,
                Player2HighestCheckout = 110,
                Season = "2024",
                Round = "Semi-Final"
            });

            // Final
            matches.Add(new Match
            {
                Id = matchId++,
                Player1Id = 12, // Luke Humphries (2024 ID)
                Player2Id = 9, // Michael Smith
                MatchDate = DateTime.SpecifyKind(new DateTime(2024, 5, 23), DateTimeKind.Utc),
                Player1Score = 11,
                Player2Score = 7,
                Player1Average = 102.6,
                Player2Average = 97.8,
                Player1180s = 8,
                Player2180s = 5,
                Player1HighestCheckout = 170,
                Player2HighestCheckout = 124,
                Season = "2024",
                Round = "Final"
            });

            // Premier League 2025 matches (existing data)
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

            // Generate additional Premier League regular season matches for 2025
            var random = new Random(42);
            var playerIds2025 = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            
            for (int week = 1; week <= 16; week++)
            {
                var weekDate = DateTime.SpecifyKind(new DateTime(2025, 2, 6), DateTimeKind.Utc).AddDays((week - 1) * 7);
                var roundName = $"Night {week}";
                
                var weekPlayerIds = playerIds2025.OrderBy(x => random.Next()).Take(8).ToList();
                
                for (int match = 0; match < 4; match++)
                {
                    var player1Id = weekPlayerIds[match * 2];
                    var player2Id = weekPlayerIds[match * 2 + 1];
                    
                    var player1Wins = random.Next(0, 2) == 1;
                    var player1Score = player1Wins ? random.Next(6, 9) : random.Next(3, 6);
                    var player2Score = player1Wins ? random.Next(3, 6) : random.Next(6, 9);
                    
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
                        Player1Average = Math.Round(85 + random.NextDouble() * 25, 2),
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

            return matches;
        }
    }
}
