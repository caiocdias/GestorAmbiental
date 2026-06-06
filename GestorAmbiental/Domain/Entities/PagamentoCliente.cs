using GestorAmbiental.Domain.Common;

namespace GestorAmbiental.Domain.Entities;

public sealed class PagamentoCliente : Entity
{
    public int PagamentoId { get; set; }
    public int ClienteId { get; set; }
    public decimal ValorAssociado { get; set; }
    public string Observacao { get; set; } = string.Empty;
}
