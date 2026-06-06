using GestorAmbiental.Domain.Common;

namespace GestorAmbiental.Domain.Entities;

public sealed class PagamentoProjeto : Entity
{
    public int PagamentoId { get; set; }
    public int ProjetoId { get; set; }
    public decimal ValorAssociado { get; set; }
    public string Observacao { get; set; } = string.Empty;
}
