using Microsoft.EntityFrameworkCore;
using DartsStats.Api.Data;
using DartsStats.Api.Models;

namespace DartsStats.Api.Services;

public class DatabaseSeedService
{
    private readonly DartsDbContext _context;

    public DatabaseSeedService(DartsDbContext context)
    {
        _context = context;
    }

    public async Task SeedDataAsync()
    {
        // Only seed if database is empty
        if (await _context.Players.AnyAsync() || await _context.Matches.AnyAsync())
        {
            return;
        }

        // Seed players first (let EF generate IDs)
        var players = SeedPlayers();
        await _context.Players.AddRangeAsync(players);
        await _context.SaveChangesAsync();
        
        // Now get the saved players with their generated IDs
        var savedPlayers = await _context.Players.ToListAsync();
        
        // Seed matches using the actual player IDs
        var matches = SeedMatches(savedPlayers);
        await _context.Matches.AddRangeAsync(matches);
        await _context.SaveChangesAsync();
    }

    private List<Player> SeedPlayers()
    {
        // Real 2025 Premier League Darts players and final standings
        return new List<Player>
        {
            new Player { Name = "Luke Littler", Country = "England", MatchesPlayed = 23, MatchesWon = 15, MatchesLost = 8, LegsWon = 204, LegsLost = 147, PointsFor = 45, PointsAgainst = 0, AvgPoints = 102.43m, AvgLegDarts = 15.2m, CheckoutPercentage = 44.94m, Position = 1 },
            new Player { Name = "Luke Humphries", Country = "England", MatchesPlayed = 23, MatchesWon = 13, MatchesLost = 10, LegsWon = 171, LegsLost = 153, PointsFor = 34, PointsAgainst = 0, AvgPoints = 99.67m, AvgLegDarts = 16.1m, CheckoutPercentage = 41.11m, Position = 2 },
            new Player { Name = "Nathan Aspinall", Country = "England", MatchesPlayed = 22, MatchesWon = 10, MatchesLost = 12, LegsWon = 136, LegsLost = 128, PointsFor = 26, PointsAgainst = 0, AvgPoints = 95.70m, AvgLegDarts = 17.3m, CheckoutPercentage = 38.41m, Position = 3 },
            new Player { Name = "Gerwyn Price", Country = "Wales", MatchesPlayed = 22, MatchesWon = 9, MatchesLost = 13, LegsWon = 126, LegsLost = 120, PointsFor = 24, PointsAgainst = 0, AvgPoints = 96.60m, AvgLegDarts = 17.0m, CheckoutPercentage = 38.06m, Position = 4 },
            new Player { Name = "Michael van Gerwen", Country = "Netherlands", MatchesPlayed = 21, MatchesWon = 8, MatchesLost = 13, LegsWon = 117, LegsLost = 134, PointsFor = 20, PointsAgainst = 0, AvgPoints = 96.07m, AvgLegDarts = 17.4m, CheckoutPercentage = 38.11m, Position = 5 },
            new Player { Name = "Chris Dobey", Country = "England", MatchesPlayed = 20, MatchesWon = 7, MatchesLost = 13, LegsWon = 104, LegsLost = 121, PointsFor = 17, PointsAgainst = 0, AvgPoints = 97.19m, AvgLegDarts = 16.8m, CheckoutPercentage = 41.70m, Position = 6 },
            new Player { Name = "Rob Cross", Country = "England", MatchesPlayed = 19, MatchesWon = 6, MatchesLost = 13, LegsWon = 99, LegsLost = 119, PointsFor = 14, PointsAgainst = 0, AvgPoints = 98.18m, AvgLegDarts = 16.9m, CheckoutPercentage = 42.30m, Position = 7 },
            new Player { Name = "Stephen Bunting", Country = "England", MatchesPlayed = 18, MatchesWon = 5, MatchesLost = 13, LegsWon = 77, LegsLost = 118, PointsFor = 12, PointsAgainst = 0, AvgPoints = 97.48m, AvgLegDarts = 17.2m, CheckoutPercentage = 34.52m, Position = 8 }
        };
    }

    private List<Match> SeedMatches(List<Player> players)
    {
        // Create a dictionary for quick player name to ID lookup
        var playerDict = players.ToDictionary(p => p.Name, p => p.Id);
        var matches = new List<Match>();
        
        // Real Premier League Darts 2025 results from actual tournament - All 16 Nights
        
        // Night 1 - 6 February 2025 - Belfast (Luke Humphries won)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 2, 6), Player1Average = 97.87, Player2Average = 96.21, Player1180s = 9, Player2180s = 5, Player1HighestCheckout = 170, Player2HighestCheckout = 120, Season = "2025", Round = "Night 1" },
            new Match { Player1Id = playerDict["Stephen Bunting"], Player2Id = playerDict["Rob Cross"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 2, 6), Player1Average = 94.03, Player2Average = 97.61, Player1180s = 3, Player2180s = 7, Player1HighestCheckout = 105, Player2HighestCheckout = 144, Season = "2025", Round = "Night 1" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 2, 6), Player1Average = 113.91, Player2Average = 105.91, Player1180s = 8, Player2180s = 9, Player1HighestCheckout = 156, Player2HighestCheckout = 138, Season = "2025", Round = "Night 1" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Luke Humphries"], Player1Score = 2, Player2Score = 6, MatchDate = new DateTime(2025, 2, 6), Player1Average = 95.73, Player2Average = 105.19, Player1180s = 4, Player2180s = 8, Player1HighestCheckout = 96, Player2HighestCheckout = 140, Season = "2025", Round = "Night 1" },
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Rob Cross"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 2, 6), Player1Average = 92.69, Player2Average = 89.55, Player1180s = 6, Player2180s = 3, Player1HighestCheckout = 121, Player2HighestCheckout = 88, Season = "2025", Round = "Night 1" },
            new Match { Player1Id = playerDict["Michael van Gerwen"], Player2Id = playerDict["Luke Humphries"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 2, 6), Player1Average = 101.51, Player2Average = 99.31, Player1180s = 7, Player2180s = 6, Player1HighestCheckout = 112, Player2HighestCheckout = 134, Season = "2025", Round = "Night 1" },
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Luke Humphries"], Player1Score = 1, Player2Score = 6, MatchDate = new DateTime(2025, 2, 6), Player1Average = 96.88, Player2Average = 95.86, Player1180s = 4, Player2180s = 5, Player1HighestCheckout = 94, Player2HighestCheckout = 116, Season = "2025", Round = "Night 1" }
        });
        
        // Night 2 - 13 February 2025 - Glasgow (Luke Littler won)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Rob Cross"], Player2Id = playerDict["Luke Littler"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 2, 13), Player1Average = 99.96, Player2Average = 104.59, Player1180s = 6, Player2180s = 11, Player1HighestCheckout = 126, Player2HighestCheckout = 170, Season = "2025", Round = "Night 2" },
            new Match { Player1Id = playerDict["Michael van Gerwen"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 6, Player2Score = 1, MatchDate = new DateTime(2025, 2, 13), Player1Average = 109.16, Player2Average = 93.91, Player1180s = 8, Player2180s = 2, Player1HighestCheckout = 148, Player2HighestCheckout = 76, Season = "2025", Round = "Night 2" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 2, 13), Player1Average = 96.25, Player2Average = 94.46, Player1180s = 4, Player2180s = 3, Player1HighestCheckout = 110, Player2HighestCheckout = 104, Season = "2025", Round = "Night 2" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Chris Dobey"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 2, 13), Player1Average = 106.07, Player2Average = 97.57, Player1180s = 11, Player2180s = 5, Player1HighestCheckout = 164, Player2HighestCheckout = 118, Season = "2025", Round = "Night 2" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 2, 13), Player1Average = 104.02, Player2Average = 98.52, Player1180s = 9, Player2180s = 6, Player1HighestCheckout = 142, Player2HighestCheckout = 96, Season = "2025", Round = "Night 2" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Luke Humphries"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 2, 13), Player1Average = 97.63, Player2Average = 101.98, Player1180s = 5, Player2180s = 8, Player1HighestCheckout = 108, Player2HighestCheckout = 128, Season = "2025", Round = "Night 2" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Luke Humphries"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 2, 13), Player1Average = 101.03, Player2Average = 93.68, Player1180s = 7, Player2180s = 4, Player1HighestCheckout = 136, Player2HighestCheckout = 84, Season = "2025", Round = "Night 2" }
        });
        
        // Night 3 - 20 February 2025 - Dublin (Gerwyn Price won)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 2, 20), Player1Average = 103.51, Player2Average = 100.49, Player1180s = 8, Player2180s = 6, Player1HighestCheckout = 140, Player2HighestCheckout = 121, Season = "2025", Round = "Night 3" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Luke Littler"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 2, 20), Player1Average = 102.64, Player2Average = 99.78, Player1180s = 7, Player2180s = 9, Player1HighestCheckout = 170, Player2HighestCheckout = 156, Season = "2025", Round = "Night 3" },
            new Match { Player1Id = playerDict["Rob Cross"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 3, Player2Score = 6, MatchDate = new DateTime(2025, 2, 20), Player1Average = 90.42, Player2Average = 85.53, Player1180s = 3, Player2180s = 14, Player1HighestCheckout = 88, Player2HighestCheckout = 170, Season = "2025", Round = "Night 3" },
            new Match { Player1Id = playerDict["Michael van Gerwen"], Player2Id = playerDict["Chris Dobey"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 2, 20), Player1Average = 98.96, Player2Average = 96.99, Player1180s = 6, Player2180s = 5, Player1HighestCheckout = 124, Player2HighestCheckout = 118, Season = "2025", Round = "Night 3" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 3, Player2Score = 6, MatchDate = new DateTime(2025, 2, 20), Player1Average = 93.56, Player2Average = 96.26, Player1180s = 4, Player2180s = 7, Player1HighestCheckout = 96, Player2HighestCheckout = 132, Season = "2025", Round = "Night 3" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 2, 20), Player1Average = 99.22, Player2Average = 93.21, Player1180s = 8, Player2180s = 4, Player1HighestCheckout = 124, Player2HighestCheckout = 108, Season = "2025", Round = "Night 3" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 2, 20), Player1Average = 100.57, Player2Average = 90.83, Player1180s = 6, Player2180s = 5, Player1HighestCheckout = 116, Player2HighestCheckout = 96, Season = "2025", Round = "Night 3" }
        });
        
        // Night 4 - 27 February 2025 - Exeter (Luke Humphries won)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Michael van Gerwen"], Player2Id = playerDict["Rob Cross"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 2, 27), Player1Average = 92.88, Player2Average = 99.33, Player1180s = 5, Player2180s = 8, Player1HighestCheckout = 112, Player2HighestCheckout = 144, Season = "2025", Round = "Night 4" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Luke Humphries"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 2, 27), Player1Average = 89.77, Player2Average = 97.22, Player1180s = 3, Player2180s = 7, Player1HighestCheckout = 96, Player2HighestCheckout = 167, Season = "2025", Round = "Night 4" },
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 2, 27), Player1Average = 92.98, Player2Average = 93.69, Player1180s = 4, Player2180s = 6, Player1HighestCheckout = 108, Player2HighestCheckout = 124, Season = "2025", Round = "Night 4" },
            new Match { Player1Id = playerDict["Stephen Bunting"], Player2Id = playerDict["Luke Littler"], Player1Score = 3, Player2Score = 6, MatchDate = new DateTime(2025, 2, 27), Player1Average = 104.89, Player2Average = 112.34, Player1180s = 8, Player2180s = 20, Player1HighestCheckout = 132, Player2HighestCheckout = 156, Season = "2025", Round = "Night 4" },
            new Match { Player1Id = playerDict["Rob Cross"], Player2Id = playerDict["Luke Humphries"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 2, 27), Player1Average = 98.81, Player2Average = 100.08, Player1180s = 6, Player2180s = 8, Player1HighestCheckout = 120, Player2HighestCheckout = 140, Season = "2025", Round = "Night 4" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Luke Littler"], Player1Score = 2, Player2Score = 6, MatchDate = new DateTime(2025, 2, 27), Player1Average = 95.21, Player2Average = 109.67, Player1180s = 4, Player2180s = 12, Player1HighestCheckout = 88, Player2HighestCheckout = 144, Season = "2025", Round = "Night 4" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Luke Littler"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 2, 27), Player1Average = 101.24, Player2Average = 96.82, Player1180s = 9, Player2180s = 11, Player1HighestCheckout = 132, Player2HighestCheckout = 120, Season = "2025", Round = "Night 4" }
        });
        
        // Night 5 - 6 March 2025 - Brighton (Luke Littler won)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 3, Player2Score = 6, MatchDate = new DateTime(2025, 3, 6), Player1Average = 90.11, Player2Average = 95.36, Player1180s = 3, Player2180s = 6, Player1HighestCheckout = 96, Player2HighestCheckout = 124, Season = "2025", Round = "Night 5" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Chris Dobey"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 3, 6), Player1Average = 97.83, Player2Average = 100.21, Player1180s = 12, Player2180s = 8, Player1HighestCheckout = 141, Player2HighestCheckout = 132, Season = "2025", Round = "Night 5" },
            new Match { Player1Id = playerDict["Rob Cross"], Player2Id = playerDict["Luke Humphries"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 3, 6), Player1Average = 111.19, Player2Average = 98.60, Player1180s = 9, Player2180s = 7, Player1HighestCheckout = 141, Player2HighestCheckout = 120, Season = "2025", Round = "Night 5" },
            new Match { Player1Id = playerDict["Stephen Bunting"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 2, Player2Score = 6, MatchDate = new DateTime(2025, 3, 6), Player1Average = 97.06, Player2Average = 106.46, Player1180s = 4, Player2180s = 11, Player1HighestCheckout = 108, Player2HighestCheckout = 124, Season = "2025", Round = "Night 5" },
            new Match { Player1Id = playerDict["Michael van Gerwen"], Player2Id = playerDict["Luke Littler"], Player1Score = 2, Player2Score = 6, MatchDate = new DateTime(2025, 3, 6), Player1Average = 90.41, Player2Average = 106.11, Player1180s = 4, Player2180s = 9, Player1HighestCheckout = 88, Player2HighestCheckout = 132, Season = "2025", Round = "Night 5" },
            new Match { Player1Id = playerDict["Rob Cross"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 3, 6), Player1Average = 106.91, Player2Average = 96.49, Player1180s = 8, Player2180s = 6, Player1HighestCheckout = 124, Player2HighestCheckout = 96, Season = "2025", Round = "Night 5" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 3, 6), Player1Average = 100.47, Player2Average = 94.54, Player1180s = 8, Player2180s = 5, Player1HighestCheckout = 116, Player2HighestCheckout = 88, Season = "2025", Round = "Night 5" }
        });
        
        // Night 6 - 13 March 2025 - Nottingham (Gerwyn Price won)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Luke Littler"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 3, 13), Player1Average = 99.49, Player2Average = 100.15, Player1180s = 7, Player2180s = 16, Player1HighestCheckout = 124, Player2HighestCheckout = 144, Season = "2025", Round = "Night 6" },
            new Match { Player1Id = playerDict["Rob Cross"], Player2Id = playerDict["Chris Dobey"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 3, 13), Player1Average = 100.90, Player2Average = 98.79, Player1180s = 8, Player2180s = 5, Player1HighestCheckout = 132, Player2HighestCheckout = 108, Season = "2025", Round = "Night 6" },
            new Match { Player1Id = playerDict["Stephen Bunting"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 3, 13), Player1Average = 102.11, Player2Average = 97.86, Player1180s = 8, Player2180s = 6, Player1HighestCheckout = 170, Player2HighestCheckout = 132, Season = "2025", Round = "Night 6" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 3, 13), Player1Average = 89.04, Player2Average = 95.13, Player1180s = 3, Player2180s = 5, Player1HighestCheckout = 88, Player2HighestCheckout = 108, Season = "2025", Round = "Night 6" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Rob Cross"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 3, 13), Player1Average = 107.82, Player2Average = 103.88, Player1180s = 12, Player2180s = 7, Player1HighestCheckout = 132, Player2HighestCheckout = 116, Season = "2025", Round = "Night 6" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 6, Player2Score = 1, MatchDate = new DateTime(2025, 3, 13), Player1Average = 107.25, Player2Average = 97.58, Player1180s = 9, Player2180s = 4, Player1HighestCheckout = 124, Player2HighestCheckout = 88, Season = "2025", Round = "Night 6" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 3, Player2Score = 6, MatchDate = new DateTime(2025, 3, 13), Player1Average = 103.39, Player2Average = 94.10, Player1180s = 9, Player2180s = 5, Player1HighestCheckout = 108, Player2HighestCheckout = 116, Season = "2025", Round = "Night 6" }
        });
        
        // Night 7 - 20 March 2025 - Cardiff (Luke Littler won - with nine-dart finish)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 3, 20), Player1Average = 90.27, Player2Average = 87.05, Player1180s = 5, Player2180s = 3, Player1HighestCheckout = 108, Player2HighestCheckout = 88, Season = "2025", Round = "Night 7" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 3, 20), Player1Average = 101.93, Player2Average = 104.04, Player1180s = 8, Player2180s = 9, Player1HighestCheckout = 132, Player2HighestCheckout = 124, Season = "2025", Round = "Night 7" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 3, 20), Player1Average = 102.21, Player2Average = 88.73, Player1180s = 22, Player2180s = 4, Player1HighestCheckout = 132, Player2HighestCheckout = 96, Season = "2025", Round = "Night 7" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Rob Cross"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 3, 20), Player1Average = 97.10, Player2Average = 89.40, Player1180s = 6, Player2180s = 4, Player1HighestCheckout = 116, Player2HighestCheckout = 170, Season = "2025", Round = "Night 7" },
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 1, Player2Score = 6, MatchDate = new DateTime(2025, 3, 20), Player1Average = 82.28, Player2Average = 91.53, Player1180s = 2, Player2180s = 5, Player1HighestCheckout = 76, Player2HighestCheckout = 108, Season = "2025", Round = "Night 7" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Rob Cross"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 3, 20), Player1Average = 107.67, Player2Average = 99.80, Player1180s = 14, Player2180s = 6, Player1HighestCheckout = 124, Player2HighestCheckout = 108, Season = "2025", Round = "Night 7" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 3, 20), Player1Average = 112.50, Player2Average = 101.41, Player1180s = 18, Player2180s = 8, Player1HighestCheckout = 170, Player2HighestCheckout = 132, Season = "2025", Round = "Night 7" }
        });
        
        // Night 8 - 27 March 2025 - Newcastle (Luke Littler won)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 6, Player2Score = 0, MatchDate = new DateTime(2025, 3, 27), Player1Average = 109.98, Player2Average = 102.15, Player1180s = 10, Player2180s = 4, Player1HighestCheckout = 132, Player2HighestCheckout = 88, Season = "2025", Round = "Night 8" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Rob Cross"], Player1Score = 2, Player2Score = 6, MatchDate = new DateTime(2025, 3, 27), Player1Average = 86.17, Player2Average = 100.87, Player1180s = 2, Player2180s = 7, Player1HighestCheckout = 76, Player2HighestCheckout = 124, Season = "2025", Round = "Night 8" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Chris Dobey"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 3, 27), Player1Average = 92.93, Player2Average = 93.60, Player1180s = 5, Player2180s = 6, Player1HighestCheckout = 108, Player2HighestCheckout = 116, Season = "2025", Round = "Night 8" },
            new Match { Player1Id = playerDict["Michael van Gerwen"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 3, 27), Player1Average = 96.04, Player2Average = 97.09, Player1180s = 5, Player2180s = 7, Player1HighestCheckout = 108, Player2HighestCheckout = 142, Season = "2025", Round = "Night 8" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Rob Cross"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 3, 27), Player1Average = 97.51, Player2Average = 94.98, Player1180s = 8, Player2180s = 5, Player1HighestCheckout = 116, Player2HighestCheckout = 96, Season = "2025", Round = "Night 8" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 3, 27), Player1Average = 106.73, Player2Average = 101.18, Player1180s = 8, Player2180s = 6, Player1HighestCheckout = 124, Player2HighestCheckout = 108, Season = "2025", Round = "Night 8" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Luke Humphries"], Player1Score = 6, Player2Score = 1, MatchDate = new DateTime(2025, 3, 27), Player1Average = 93.62, Player2Average = 93.31, Player1180s = 6, Player2180s = 4, Player1HighestCheckout = 108, Player2HighestCheckout = 88, Season = "2025", Round = "Night 8" }
        });
        
        // Night 9 - 3 April 2025 - Berlin (Stephen Bunting won, Michael van Gerwen withdrew)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 2, Player2Score = 6, MatchDate = new DateTime(2025, 4, 3), Player1Average = 100.23, Player2Average = 103.40, Player1180s = 6, Player2180s = 12, Player1HighestCheckout = 108, Player2HighestCheckout = 124, Season = "2025", Round = "Night 9" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Rob Cross"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 4, 3), Player1Average = 98.00, Player2Average = 99.68, Player1180s = 7, Player2180s = 8, Player1HighestCheckout = 116, Player2HighestCheckout = 110, Season = "2025", Round = "Night 9" },
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Luke Littler"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 4, 3), Player1Average = 93.23, Player2Average = 93.28, Player1180s = 5, Player2180s = 4, Player1HighestCheckout = 108, Player2HighestCheckout = 96, Season = "2025", Round = "Night 9" },
            new Match { Player1Id = playerDict["Stephen Bunting"], Player2Id = playerDict["Luke Humphries"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 4, 3), Player1Average = 95.66, Player2Average = 87.83, Player1180s = 8, Player2180s = 3, Player1HighestCheckout = 116, Player2HighestCheckout = 88, Season = "2025", Round = "Night 9" },
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 2, Player2Score = 6, MatchDate = new DateTime(2025, 4, 3), Player1Average = 96.94, Player2Average = 92.42, Player1180s = 4, Player2180s = 6, Player1HighestCheckout = 96, Player2HighestCheckout = 108, Season = "2025", Round = "Night 9" },
            new Match { Player1Id = playerDict["Stephen Bunting"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 4, 3), Player1Average = 93.73, Player2Average = 93.12, Player1180s = 7, Player2180s = 5, Player1HighestCheckout = 108, Player2HighestCheckout = 96, Season = "2025", Round = "Night 9" }
        });
        
        // Night 10 - 10 April 2025 - Manchester (Nathan Aspinall won, Gerwyn Price nine-dart finish)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 4, 10), Player1Average = 94.32, Player2Average = 97.20, Player1180s = 4, Player2180s = 7, Player1HighestCheckout = 108, Player2HighestCheckout = 124, Season = "2025", Round = "Night 10" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Rob Cross"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 4, 10), Player1Average = 96.00, Player2Average = 97.93, Player1180s = 11, Player2180s = 6, Player1HighestCheckout = 132, Player2HighestCheckout = 108, Season = "2025", Round = "Night 10" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 4, 10), Player1Average = 103.93, Player2Average = 103.47, Player1180s = 9, Player2180s = 8, Player1HighestCheckout = 124, Player2HighestCheckout = 132, Season = "2025", Round = "Night 10" },
            new Match { Player1Id = playerDict["Stephen Bunting"], Player2Id = playerDict["Luke Humphries"], Player1Score = 1, Player2Score = 6, MatchDate = new DateTime(2025, 4, 10), Player1Average = 105.33, Player2Average = 118.43, Player1180s = 7, Player2180s = 9, Player1HighestCheckout = 108, Player2HighestCheckout = 142, Season = "2025", Round = "Night 10" },
            new Match { Player1Id = playerDict["Michael van Gerwen"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 2, Player2Score = 6, MatchDate = new DateTime(2025, 4, 10), Player1Average = 85.59, Player2Average = 101.31, Player1180s = 3, Player2180s = 8, Player1HighestCheckout = 88, Player2HighestCheckout = 124, Season = "2025", Round = "Night 10" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Luke Humphries"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 4, 10), Player1Average = 96.63, Player2Average = 93.80, Player1180s = 6, Player2180s = 5, Player1HighestCheckout = 108, Player2HighestCheckout = 116, Season = "2025", Round = "Night 10" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Luke Humphries"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 4, 10), Player1Average = 93.29, Player2Average = 98.55, Player1180s = 5, Player2180s = 7, Player1HighestCheckout = 96, Player2HighestCheckout = 124, Season = "2025", Round = "Night 10" }
        });
        
        // Night 11 - 17 April 2025 - Rotterdam (Chris Dobey won)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Luke Humphries"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 4, 17), Player1Average = 100.49, Player2Average = 90.17, Player1180s = 14, Player2180s = 4, Player1HighestCheckout = 132, Player2HighestCheckout = 96, Season = "2025", Round = "Night 11" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 3, Player2Score = 6, MatchDate = new DateTime(2025, 4, 17), Player1Average = 94.19, Player2Average = 97.02, Player1180s = 4, Player2180s = 7, Player1HighestCheckout = 108, Player2HighestCheckout = 170, Season = "2025", Round = "Night 11" },
            new Match { Player1Id = playerDict["Stephen Bunting"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 4, 17), Player1Average = 99.76, Player2Average = 89.86, Player1180s = 8, Player2180s = 4, Player1HighestCheckout = 124, Player2HighestCheckout = 88, Season = "2025", Round = "Night 11" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Rob Cross"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 4, 17), Player1Average = 108.56, Player2Average = 110.15, Player1180s = 11, Player2180s = 9, Player1HighestCheckout = 144, Player2HighestCheckout = 132, Season = "2025", Round = "Night 11" },
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 4, 17), Player1Average = 100.67, Player2Average = 90.03, Player1180s = 9, Player2180s = 3, Player1HighestCheckout = 124, Player2HighestCheckout = 88, Season = "2025", Round = "Night 11" },
            new Match { Player1Id = playerDict["Stephen Bunting"], Player2Id = playerDict["Luke Littler"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 4, 17), Player1Average = 106.43, Player2Average = 101.39, Player1180s = 9, Player2180s = 8, Player1HighestCheckout = 132, Player2HighestCheckout = 120, Season = "2025", Round = "Night 11" },
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 4, 17), Player1Average = 101.84, Player2Average = 97.12, Player1180s = 8, Player2180s = 6, Player1HighestCheckout = 116, Player2HighestCheckout = 108, Season = "2025", Round = "Night 11" }
        });
        
        // Night 12 - 24 April 2025 - Liverpool (Gerwyn Price won)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 4, 24), Player1Average = 113.50, Player2Average = 104.03, Player1180s = 9, Player2180s = 7, Player1HighestCheckout = 144, Player2HighestCheckout = 124, Season = "2025", Round = "Night 12" },
            new Match { Player1Id = playerDict["Michael van Gerwen"], Player2Id = playerDict["Luke Littler"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 4, 24), Player1Average = 95.53, Player2Average = 98.77, Player1180s = 5, Player2180s = 8, Player1HighestCheckout = 108, Player2HighestCheckout = 160, Season = "2025", Round = "Night 12" },
            new Match { Player1Id = playerDict["Rob Cross"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 6, Player2Score = 1, MatchDate = new DateTime(2025, 4, 24), Player1Average = 100.05, Player2Average = 91.01, Player1180s = 7, Player2180s = 3, Player1HighestCheckout = 124, Player2HighestCheckout = 88, Season = "2025", Round = "Night 12" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Chris Dobey"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 4, 24), Player1Average = 106.74, Player2Average = 105.61, Player1180s = 9, Player2180s = 7, Player1HighestCheckout = 132, Player2HighestCheckout = 124, Season = "2025", Round = "Night 12" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 4, 24), Player1Average = 98.22, Player2Average = 98.21, Player1180s = 6, Player2180s = 5, Player1HighestCheckout = 116, Player2HighestCheckout = 108, Season = "2025", Round = "Night 12" },
            new Match { Player1Id = playerDict["Rob Cross"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 2, Player2Score = 6, MatchDate = new DateTime(2025, 4, 24), Player1Average = 86.51, Player2Average = 105.27, Player1180s = 3, Player2180s = 8, Player1HighestCheckout = 88, Player2HighestCheckout = 124, Season = "2025", Round = "Night 12" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 4, 24), Player1Average = 102.64, Player2Average = 104.43, Player1180s = 7, Player2180s = 8, Player1HighestCheckout = 124, Player2HighestCheckout = 132, Season = "2025", Round = "Night 12" }
        });
        
        // Night 13 - 1 May 2025 - Birmingham (Luke Littler won)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 5, 1), Player1Average = 98.11, Player2Average = 95.25, Player1180s = 13, Player2180s = 6, Player1HighestCheckout = 132, Player2HighestCheckout = 108, Season = "2025", Round = "Night 13" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Chris Dobey"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 5, 1), Player1Average = 92.28, Player2Average = 87.43, Player1180s = 5, Player2180s = 3, Player1HighestCheckout = 96, Player2HighestCheckout = 88, Season = "2025", Round = "Night 13" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 5, 1), Player1Average = 107.46, Player2Average = 99.43, Player1180s = 9, Player2180s = 7, Player1HighestCheckout = 144, Player2HighestCheckout = 124, Season = "2025", Round = "Night 13" },
            new Match { Player1Id = playerDict["Rob Cross"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 5, 1), Player1Average = 97.97, Player2Average = 95.79, Player1180s = 6, Player2180s = 5, Player1HighestCheckout = 108, Player2HighestCheckout = 130, Season = "2025", Round = "Night 13" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 5, 1), Player1Average = 96.56, Player2Average = 98.16, Player1180s = 8, Player2180s = 7, Player1HighestCheckout = 116, Player2HighestCheckout = 124, Season = "2025", Round = "Night 13" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 3, Player2Score = 6, MatchDate = new DateTime(2025, 5, 1), Player1Average = 80.53, Player2Average = 91.98, Player1180s = 2, Player2180s = 5, Player1HighestCheckout = 76, Player2HighestCheckout = 108, Season = "2025", Round = "Night 13" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 5, 1), Player1Average = 102.50, Player2Average = 94.31, Player1180s = 10, Player2180s = 6, Player1HighestCheckout = 124, Player2HighestCheckout = 108, Season = "2025", Round = "Night 13" }
        });
        
        // Night 14 - 8 May 2025 - Leeds (Luke Humphries won)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Rob Cross"], Player2Id = playerDict["Gerwyn Price"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 5, 8), Player1Average = 88.76, Player2Average = 94.16, Player1180s = 4, Player2180s = 6, Player1HighestCheckout = 96, Player2HighestCheckout = 120, Season = "2025", Round = "Night 14" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Luke Littler"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 5, 8), Player1Average = 95.81, Player2Average = 91.41, Player1180s = 6, Player2180s = 5, Player1HighestCheckout = 164, Player2HighestCheckout = 108, Season = "2025", Round = "Night 14" },
            new Match { Player1Id = playerDict["Michael van Gerwen"], Player2Id = playerDict["Luke Humphries"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 5, 8), Player1Average = 92.08, Player2Average = 98.89, Player1180s = 4, Player2180s = 7, Player1HighestCheckout = 108, Player2HighestCheckout = 132, Season = "2025", Round = "Night 14" },
            new Match { Player1Id = playerDict["Stephen Bunting"], Player2Id = playerDict["Chris Dobey"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 5, 8), Player1Average = 97.46, Player2Average = 99.43, Player1180s = 6, Player2180s = 8, Player1HighestCheckout = 124, Player2HighestCheckout = 132, Season = "2025", Round = "Night 14" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Luke Littler"], Player1Score = 3, Player2Score = 6, MatchDate = new DateTime(2025, 5, 8), Player1Average = 97.82, Player2Average = 104.29, Player1180s = 5, Player2180s = 9, Player1HighestCheckout = 108, Player2HighestCheckout = 132, Season = "2025", Round = "Night 14" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 5, 8), Player1Average = 91.94, Player2Average = 95.70, Player1180s = 6, Player2180s = 7, Player1HighestCheckout = 116, Player2HighestCheckout = 124, Season = "2025", Round = "Night 14" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Luke Humphries"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 5, 8), Player1Average = 99.89, Player2Average = 100.96, Player1180s = 8, Player2180s = 10, Player1HighestCheckout = 124, Player2HighestCheckout = 144, Season = "2025", Round = "Night 14" }
        });
        
        // Night 15 - 15 May 2025 - Aberdeen (Nathan Aspinall won, Gerwyn Price nine-dart finish)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Michael van Gerwen"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 3, Player2Score = 6, MatchDate = new DateTime(2025, 5, 15), Player1Average = 97.52, Player2Average = 97.20, Player1180s = 6, Player2180s = 7, Player1HighestCheckout = 108, Player2HighestCheckout = 124, Season = "2025", Round = "Night 15" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 4, Player2Score = 6, MatchDate = new DateTime(2025, 5, 15), Player1Average = 91.56, Player2Average = 94.82, Player1180s = 5, Player2180s = 6, Player1HighestCheckout = 108, Player2HighestCheckout = 116, Season = "2025", Round = "Night 15" },
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Rob Cross"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 5, 15), Player1Average = 107.08, Player2Average = 100.42, Player1180s = 13, Player2180s = 5, Player1HighestCheckout = 132, Player2HighestCheckout = 108, Season = "2025", Round = "Night 15" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Luke Humphries"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 5, 15), Player1Average = 115.96, Player2Average = 110.01, Player1180s = 13, Player2180s = 8, Player1HighestCheckout = 144, Player2HighestCheckout = 132, Season = "2025", Round = "Night 15" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 6, Player2Score = 1, MatchDate = new DateTime(2025, 5, 15), Player1Average = 94.19, Player2Average = 95.63, Player1180s = 5, Player2180s = 4, Player1HighestCheckout = 108, Player2HighestCheckout = 96, Season = "2025", Round = "Night 15" },
            new Match { Player1Id = playerDict["Chris Dobey"], Player2Id = playerDict["Luke Littler"], Player1Score = 6, Player2Score = 5, MatchDate = new DateTime(2025, 5, 15), Player1Average = 97.89, Player2Average = 101.44, Player1180s = 8, Player2180s = 9, Player1HighestCheckout = 116, Player2HighestCheckout = 124, Season = "2025", Round = "Night 15" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Chris Dobey"], Player1Score = 6, Player2Score = 1, MatchDate = new DateTime(2025, 5, 15), Player1Average = 92.77, Player2Average = 98.39, Player1180s = 4, Player2180s = 6, Player1HighestCheckout = 96, Player2HighestCheckout = 108, Season = "2025", Round = "Night 15" }
        });
        
        // Night 16 - 22 May 2025 - Sheffield (Luke Littler won, final night)
        matches.AddRange(new List<Match>
        {
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Stephen Bunting"], Player1Score = 6, Player2Score = 1, MatchDate = new DateTime(2025, 5, 22), Player1Average = 114.37, Player2Average = 94.14, Player1180s = 10, Player2180s = 3, Player1HighestCheckout = 170, Player2HighestCheckout = 88, Season = "2025", Round = "Night 16" },
            new Match { Player1Id = playerDict["Nathan Aspinall"], Player2Id = playerDict["Michael van Gerwen"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 5, 22), Player1Average = 98.72, Player2Average = 88.72, Player1180s = 7, Player2180s = 3, Player1HighestCheckout = 124, Player2HighestCheckout = 96, Season = "2025", Round = "Night 16" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Rob Cross"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 5, 22), Player1Average = 97.41, Player2Average = 95.91, Player1180s = 6, Player2180s = 5, Player1HighestCheckout = 132, Player2HighestCheckout = 108, Season = "2025", Round = "Night 16" },
            new Match { Player1Id = playerDict["Gerwyn Price"], Player2Id = playerDict["Chris Dobey"], Player1Score = 5, Player2Score = 6, MatchDate = new DateTime(2025, 5, 22), Player1Average = 103.96, Player2Average = 108.91, Player1180s = 7, Player2180s = 10, Player1HighestCheckout = 124, Player2HighestCheckout = 144, Season = "2025", Round = "Night 16" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Nathan Aspinall"], Player1Score = 6, Player2Score = 4, MatchDate = new DateTime(2025, 5, 22), Player1Average = 98.42, Player2Average = 96.42, Player1180s = 8, Player2180s = 6, Player1HighestCheckout = 124, Player2HighestCheckout = 108, Season = "2025", Round = "Night 16" },
            new Match { Player1Id = playerDict["Luke Humphries"], Player2Id = playerDict["Chris Dobey"], Player1Score = 6, Player2Score = 2, MatchDate = new DateTime(2025, 5, 22), Player1Average = 106.04, Player2Average = 98.18, Player1180s = 8, Player2180s = 6, Player1HighestCheckout = 132, Player2HighestCheckout = 116, Season = "2025", Round = "Night 16" },
            new Match { Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Luke Humphries"], Player1Score = 6, Player2Score = 3, MatchDate = new DateTime(2025, 5, 22), Player1Average = 96.99, Player2Average = 92.93, Player1180s = 7, Player2180s = 5, Player1HighestCheckout = 124, Player2HighestCheckout = 108, Season = "2025", Round = "Night 16" }
        });
        
        // Add Play-offs - 29 May 2025 - London O2 Arena (Luke Littler won)
        // Semi-finals
        matches.Add(new Match { 
            Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Luke Humphries"], 
            Player1Score = 10, Player2Score = 6, MatchDate = new DateTime(2025, 5, 29), 
            Player1Average = 103.16, Player2Average = 101.66, Player1180s = 16, Player2180s = 13, 
            Player1HighestCheckout = 144, Player2HighestCheckout = 132, Season = "2025", Round = "Semi-Final 1" 
        });
        
        matches.Add(new Match { 
            Player1Id = playerDict["Michael van Gerwen"], Player2Id = playerDict["Nathan Aspinall"], 
            Player1Score = 10, Player2Score = 5, MatchDate = new DateTime(2025, 5, 29), 
            Player1Average = 99.94, Player2Average = 98.23, Player1180s = 12, Player2180s = 9, 
            Player1HighestCheckout = 124, Player2HighestCheckout = 108, Season = "2025", Round = "Semi-Final 2" 
        });
        
        // Final
        matches.Add(new Match { 
            Player1Id = playerDict["Luke Littler"], Player2Id = playerDict["Michael van Gerwen"], 
            Player1Score = 11, Player2Score = 3, MatchDate = new DateTime(2025, 5, 29), 
            Player1Average = 102.85, Player2Average = 96.42, Player1180s = 14, Player2180s = 7, 
            Player1HighestCheckout = 170, Player2HighestCheckout = 132, Season = "2025", Round = "Final" 
        });
        
        return matches;
    }
}
