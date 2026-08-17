using Microsoft.EntityFrameworkCore;
using FaturamentoService.Models;

namespace FaturamentoService.Data
{
    public class FaturamentoDbContext : DbContext
    {
        public FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> options)
            : base(options)
        {
        }

        public DbSet<NotaFiscal> NotasFiscais { get; set; }
        public DbSet<NotaFiscalItem> NotaFiscalItens { get; set; }
    }
}