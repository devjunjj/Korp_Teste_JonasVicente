using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FaturamentoService.Data;
using FaturamentoService.Models;

namespace FaturamentoService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotasFiscaisController : ControllerBase
    {
        private readonly FaturamentoDbContext _context;

        public NotasFiscaisController(FaturamentoDbContext context)
        {
            _context = context;
        }

        // GET /api/notasfiscais
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotaFiscal>>> GetNotasFiscais()
        {
            return await _context.NotasFiscais
                .Include(n => n.Itens)
                .ToListAsync();
        }

        // GET /api/notasfiscais/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NotaFiscal>> GetNotaFiscal(int id)
        {
            var nota = await _context.NotasFiscais
                .Include(n => n.Itens)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (nota == null)
            {
                return NotFound(new { mensagem = $"Nota Fiscal com Id {id} não encontrada." });
            }

            return nota;
        }

        // POST /api/notasfiscais
        [HttpPost]
        public async Task<ActionResult<NotaFiscal>> PostNotaFiscal(NotaFiscal nota)
        {
            var ultimoNumero = await _context.NotasFiscais
                .OrderByDescending(n => n.Numero)
                .Select(n => n.Numero)
                .FirstOrDefaultAsync();

            nota.Numero = ultimoNumero + 1;
            nota.Status = StatusNotaFiscal.Aberta;

            _context.NotasFiscais.Add(nota);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNotaFiscal), new { id = nota.Id }, nota);
        }
    }
}