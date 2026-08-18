using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EstoqueService.Data;
using EstoqueService.Models;
using EstoqueService.Services;

namespace EstoqueService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly EstoqueDbContext _context;
        private readonly GeminiService _geminiService;

        public ProdutosController(EstoqueDbContext context, GeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        // GET /api/produtos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Produto>>> GetProdutos()
        {
            return await _context.Produtos.ToListAsync();
        }

        // GET /api/produtos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Produto>> GetProduto(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);

            if (produto == null)
            {
                return NotFound(new { mensagem = $"Produto com Id {id} não encontrado." });
            }

            return produto;
        }

        // POST /api/produtos
        [HttpPost]
        public async Task<ActionResult<Produto>> PostProduto(Produto produto)
        {
            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduto), new { id = produto.Id }, produto);
        }

        // PUT /api/produtos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduto(int id, Produto produto)
        {
            if (id != produto.Id)
            {
                return BadRequest(new { mensagem = "O Id da URL não bate com o Id do produto enviado." });
            }

            _context.Entry(produto).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                var existe = await _context.Produtos.AnyAsync(p => p.Id == id);
                if (!existe)
                {
                    return NotFound(new { mensagem = $"Produto com Id {id} não encontrado." });
                }
                throw;
            }

            return NoContent();
        }

        // DELETE /api/produtos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduto(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null)
            {
                return NotFound(new { mensagem = $"Produto com Id {id} não encontrado." });
            }

            _context.Produtos.Remove(produto);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        public class BaixaEstoqueRequest
        {
            public int Quantidade { get; set; }
        }

        // PUT /api/produtos/5/baixa
        [HttpPut("{id}/baixa")]
        public async Task<IActionResult> BaixarEstoque(int id, BaixaEstoqueRequest request)
        {
            const int maxTentativas = 3;

            for (int tentativa = 1; tentativa <= maxTentativas; tentativa++)
            {
                var produto = await _context.Produtos.FindAsync(id);

                if (produto == null)
                {
                    return NotFound(new { mensagem = $"Produto com Id {id} não encontrado." });
                }

                if (produto.Saldo < request.Quantidade)
                {
                    return BadRequest(new
                    {
                        mensagem = $"Saldo insuficiente para o produto '{produto.Descricao}'. Saldo atual: {produto.Saldo}, quantidade solicitada: {request.Quantidade}."
                    });
                }

                produto.Saldo -= request.Quantidade;
                produto.Version++;

                try
                {
                    await _context.SaveChangesAsync();
                    return Ok(produto);
                }
                catch (DbUpdateConcurrencyException)
                {
                    _context.Entry(produto).State = EntityState.Detached;

                    if (tentativa == maxTentativas)
                    {
                        return Conflict(new
                        {
                            mensagem = "Não foi possível atualizar o saldo devido a alterações concorrentes. Tente novamente."
                        });
                    }
                }
            }

            return StatusCode(500, new { mensagem = "Erro inesperado ao processar a baixa de estoque." });
        }

        public class SugerirDescricaoRequest
        {
            public string Codigo { get; set; } = string.Empty;
        }

        // POST /api/produtos/sugerir-descricao
        [HttpPost("sugerir-descricao")]
        public async Task<IActionResult> SugerirDescricao(SugerirDescricaoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Codigo))
            {
                return BadRequest(new { mensagem = "Código é obrigatório para sugerir uma descrição." });
            }

            try
            {
                var descricaoSugerida = await _geminiService.SugerirDescricaoAsync(request.Codigo);
                return Ok(new { descricaoSugerida });
            }
            catch (Exception ex)
            {
                return StatusCode(502, new
                {
                    mensagem = "Não foi possível gerar a sugestão de descrição no momento.",
                    detalhe = ex.Message
                });
            }
        }
    }
}