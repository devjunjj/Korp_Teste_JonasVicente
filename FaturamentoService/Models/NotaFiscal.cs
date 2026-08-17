namespace FaturamentoService.Models
{
    public enum StatusNotaFiscal
    {
        Aberta,
        Fechada
    }

    public class NotaFiscal
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public StatusNotaFiscal Status { get; set; } = StatusNotaFiscal.Aberta;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public List<NotaFiscalItem> Itens { get; set; } = new();
    }
}