using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsStats.Api.Models;
using DartsStats.Api.Data;

namespace DartsStats.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class ManagementController : ControllerBase
    {
        private readonly DartsDbContext _context;
        private readonly ILogger<ManagementController> _logger;

        public ManagementController(DartsDbContext context, ILogger<ManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Update match data including statistics, result and players
        /// </summary>
        [HttpPut("matches/{id}")]
        public async Task<ActionResult<Match>> UpdateMatch(int id, [FromBody] UpdateMatchDto updateDto)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null)
            {
                return NotFound(new { message = $"Match with ID {id} not found" });
            }

            // Validate that players exist
            var player1Exists = await _context.Players.AnyAsync(p => p.Id == updateDto.Player1Id);
            var player2Exists = await _context.Players.AnyAsync(p => p.Id == updateDto.Player2Id);

            if (!player1Exists)
            {
                return BadRequest(new { message = $"Player with ID {updateDto.Player1Id} not found" });
            }

            if (!player2Exists)
            {
                return BadRequest(new { message = $"Player with ID {updateDto.Player2Id} not found" });
            }

            // Update match properties
            match.Player1Id = updateDto.Player1Id;
            match.Player2Id = updateDto.Player2Id;
            match.MatchDate = updateDto.MatchDate;
            match.Player1Score = updateDto.Player1Score;
            match.Player2Score = updateDto.Player2Score;
            match.Player1Average = updateDto.Player1Average;
            match.Player2Average = updateDto.Player2Average;
            match.Player1180s = updateDto.Player1180s;
            match.Player2180s = updateDto.Player2180s;
            match.Player1HighestCheckout = updateDto.Player1HighestCheckout;
            match.Player2HighestCheckout = updateDto.Player2HighestCheckout;
            match.Season = updateDto.Season;
            match.Round = updateDto.Round;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Match {MatchId} updated by {User}", id, User.Identity?.Name);

                // Reload with navigation properties
                await _context.Entry(match).Reference(m => m.Player1).LoadAsync();
                await _context.Entry(match).Reference(m => m.Player2).LoadAsync();

                return Ok(match);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating match {MatchId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the match" });
            }
        }

        /// <summary>
        /// Get match for editing
        /// </summary>
        [HttpGet("matches/{id}")]
        public async Task<ActionResult<Match>> GetMatchForEdit(int id)
        {
            var match = await _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
            {
                return NotFound(new { message = $"Match with ID {id} not found" });
            }

            return Ok(match);
        }

        /// <summary>
        /// Delete a match
        /// </summary>
        [HttpDelete("matches/{id}")]
        public async Task<ActionResult> DeleteMatch(int id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null)
            {
                return NotFound(new { message = $"Match with ID {id} not found" });
            }

            _context.Matches.Remove(match);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Match {MatchId} deleted by {User}", id, User.Identity?.Name);

            return NoContent();
        }
    }
}
