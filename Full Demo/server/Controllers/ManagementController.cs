using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsStats.Api.DTOs;
using DartsStats.Api.Data;
using DartsStats.Api.Mappings;

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
        public async Task<ActionResult<MatchDto>> UpdateMatch(int id, [FromBody] UpdateMatchDto updateDto)
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

            // Update match properties using the mapping extension
            updateDto.UpdateEntity(match);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Match {MatchId} updated by {User}", id, User.Identity?.Name);

                // Reload with navigation properties
                await _context.Entry(match).Reference(m => m.Player1).LoadAsync();
                await _context.Entry(match).Reference(m => m.Player2).LoadAsync();

                return Ok(match.ToDto());
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
        public async Task<ActionResult<MatchDto>> GetMatchForEdit(int id)
        {
            var match = await _context.Matches
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
            {
                return NotFound(new { message = $"Match with ID {id} not found" });
            }

            return Ok(match.ToDto());
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
