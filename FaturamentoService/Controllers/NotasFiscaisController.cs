using System.Collections.Concurrent;
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

        // Cache em memória usado para garantir idempotência do endpoint de impressão.
        // Guarda o resultado da primeira execução para cada Idempotency-Key recebida.
        private static readonly ConcurrentDictionary<string, (int StatusCode, object? Body)> _idempotencyCache = new();

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
        // Aceita opcionalmente um cabeçalho "Idempotency-Key". Se a mesma chave for enviada
        // novamente, a resposta da primeira execução é devolvida sem reprocessar a operação.
        [HttpPost("{id}/imprimir")]
        public async Task<IActionResult> ImprimirNotaFiscal(int id, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (!string.IsNullOrEmpty(idempotencyKey) && _idempotencyCache.TryGetValue(idempotencyKey, out var resultadoCacheado))
            {
                return StatusCode(resultadoCacheado.StatusCode, resultadoCacheado.Body);
            }

            var resultado = await ProcessarImpressaoAsync(id);

            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                _idempotencyCache[idempotencyKey] = resultado;
            }

            return StatusCode(resultado.StatusCode, resultado.Body);
        }

        private async Task<(int StatusCode, object? Body)> ProcessarImpressaoAsync(int id)
        {
            var nota = await _context.NotasFiscais
                .Include(n => n.Itens)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (nota == null)
            {
                return (404, new { mensagem = $"Nota Fiscal com Id {id} não encontrada." });
            }

            if (nota.Status != StatusNotaFiscal.Aberta)
            {
                return (400, new { mensagem = "Só é possível imprimir notas fiscais com status Aberta." });
            }

            try
            {
                foreach (var item in nota.Itens)
                {
                    var produto = await _estoqueApiClient.BuscarProdutoAsync(item.ProdutoId);

                    if (produto == null)
                    {
                        return (400, new { mensagem = $"Produto de Id {item.ProdutoId} não encontrado no Estoque." });
                    }

                    if (produto.Saldo < item.Quantidade)
                    {
                        return (400, new
                        {
                            mensagem = $"Saldo insuficiente para o produto '{produto.Descricao}'. Saldo atual: {produto.Saldo}, solicitado: {item.Quantidade}."
                        });
                    }
                }

                foreach (var item in nota.Itens)
                {
                    var sucesso = await _estoqueApiClient.BaixarEstoqueAsync(item.ProdutoId, item.Quantidade);

                    if (!sucesso)
                    {
                        return (500, new { mensagem = $"Falha ao abater saldo do produto {item.ProdutoId}. A nota não foi fechada." });
                    }
                }
            }
            catch (HttpRequestException)
            {
                return (503, new { mensagem = "Serviço de Estoque está indisponível no momento. Não foi possível imprimir a nota. Tente novamente em instantes." });
            }

            nota.Status = StatusNotaFiscal.Fechada;
            await _context.SaveChangesAsync();

            return (200, nota);
        }
    }
}