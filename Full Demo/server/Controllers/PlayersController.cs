using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DartsStats.Api.DTOs;
using DartsStats.Api.Data;
using DartsStats.Api.Mappings;

namespace DartsStats.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly DartsDbContext _context;

        public PlayersController(DartsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlayerDto>>> GetPlayers()
        {
            var players = await _context.Players
                .OrderBy(p => p.Position)
                .ToListAsync();
            
            return Ok(players.Select(p => p.ToDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PlayerDto>> GetPlayer(int id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }
            return Ok(player.ToDto());
        }
    }
}
