using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsStats.Api.DTOs;
using DartsStats.Api.Data;
using DartsStats.Api.Mappings;

namespace DartsStats.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchesController : ControllerBase
    {
        private readonly DartsDbContext _context;

        public MatchesController(DartsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MatchDto>>> GetMatches([FromQuery] string? season = null, [FromQuery] string? round = null)
        {
            var matches = _context.Matches.Include(m => m.Player1).Include(m => m.Player2).AsQueryable();
            
            if (!string.IsNullOrEmpty(season))
            {
                matches = matches.Where(m => m.Season == season);
            }
            
            if (!string.IsNullOrEmpty(round))
            {
                matches = matches.Where(m => m.Round == round);
            }
            
            var matchList = await matches.OrderBy(m => m.MatchDate).ToListAsync();
            return Ok(matchList.Select(m => m.ToDto()));
        }

        [HttpGet("rounds")]
        public async Task<ActionResult<IEnumerable<string>>> GetRounds([FromQuery] string? season = null)
        {
            var rounds = _context.Matches.AsQueryable();
            
            if (!string.IsNullOrEmpty(season))
            {
                rounds = rounds.Where(m => m.Season == season);
            }
            
            var distinctRounds = await rounds
                .Select(m => m.Round)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
            
            return Ok(distinctRounds);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MatchDto>> GetMatch(int id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null)
            {
                return NotFound();
            }
            return Ok(match.ToDto());
        }
    }
}
