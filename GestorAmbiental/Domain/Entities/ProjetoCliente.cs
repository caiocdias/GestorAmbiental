using GestorAmbiental.Domain.Common;

namespace GestorAmbiental.Domain.Entities;

public sealed class ProjetoCliente : Entity
{
    public int ProjetoId { get; set; }
    public int ClienteId { get; set; }
    public string Papel { get; set; } = string.Empty;
    public decimal PercentualResponsabilidade { get; set; }
    public DateTime DataVinculo { get; set; } = DateTime.Today;
    public bool Principal { get; set; }
}
