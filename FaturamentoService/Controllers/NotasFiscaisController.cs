using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FaturamentoService.Data;
using FaturamentoService.Models;
using FaturamentoService.Services;

namespace FaturamentoService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotasFiscaisController : ControllerBase
    {
        private readonly FaturamentoDbContext _context;
        private readonly EstoqueApiClient _estoqueApiClient;

        public NotasFiscaisController(FaturamentoDbContext context, EstoqueApiClient estoqueApiClient)
        {
            _context = context;
            _estoqueApiClient = estoqueApiClient;
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

        // POST /api/notasfiscais/5/imprimir
        [HttpPost("{id}/imprimir")]
        public async Task<IActionResult> ImprimirNotaFiscal(int id)
        {
            var nota = await _context.NotasFiscais
                .Include(n => n.Itens)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (nota == null)
            {
                return NotFound(new { mensagem = $"Nota Fiscal com Id {id} não encontrada." });
            }

            if (nota.Status != StatusNotaFiscal.Aberta)
            {
                return BadRequest(new { mensagem = "Só é possível imprimir notas fiscais com status Aberta." });
            }

            try
            {
                // Passo 1: validar cada item (produto existe? tem saldo suficiente?)
                foreach (var item in nota.Itens)
                {
                    var produto = await _estoqueApiClient.BuscarProdutoAsync(item.ProdutoId);

                    if (produto == null)
                    {
                        return BadRequest(new
                        {
                            mensagem = $"Produto de Id {item.ProdutoId} não encontrado no Estoque."
                        });
                    }

                    if (produto.Saldo < item.Quantidade)
                    {
                        return BadRequest(new
                        {
                            mensagem = $"Saldo insuficiente para o produto '{produto.Descricao}'. Saldo atual: {produto.Saldo}, solicitado: {item.Quantidade}."
                        });
                    }
                }

                // Passo 2: se passou em todas as validações, abater o saldo de fato
                foreach (var item in nota.Itens)
                {
                    var sucesso = await _estoqueApiClient.BaixarEstoqueAsync(item.ProdutoId, item.Quantidade);

                    if (!sucesso)
                    {
                        return StatusCode(500, new
                        {
                            mensagem = $"Falha ao abater saldo do produto {item.ProdutoId}. A nota não foi fechada."
                        });
                    }
                }
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, new
                {
                    mensagem = "Serviço de Estoque está indisponível no momento. Não foi possível imprimir a nota. Tente novamente em instantes."
                });
            }

            // Passo 3: fechar a nota
            nota.Status = StatusNotaFiscal.Fechada;
            await _context.SaveChangesAsync();

            return Ok(nota);
        }
    }
}