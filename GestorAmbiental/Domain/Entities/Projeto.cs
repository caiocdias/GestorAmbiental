using GestorAmbiental.Domain.Common;
using GestorAmbiental.Domain.Display;
using GestorAmbiental.Domain.Enums;
using System.Text.Json.Serialization;

namespace GestorAmbiental.Domain.Entities;

public sealed class Projeto : Entity
{
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; } = DateTime.Today;
    public DateTime? DataPrevistaFim { get; set; }
    public DateTime? DataFinal { get; set; }
    public decimal ValorContratado { get; set; }
    public decimal AreaAfetadaM2 { get; set; }
    public string DescricaoImpactoAmbiental { get; set; } = string.Empty;
    public TipoProjetoAmbiental TipoAmbiental { get; set; } = TipoProjetoAmbiental.OUTROS;
    public SituacaoProjeto Situacao { get; set; } = SituacaoProjeto.PLANEJADO;
    public Endereco Endereco { get; set; } = new();
    public List<ProjetoCliente> Clientes { get; set; } = [];
    public List<PagamentoProjeto> Pagamentos { get; set; } = [];

    [JsonIgnore]
    public decimal SaldoPendente => CalcularSaldoPendente();

    [JsonIgnore]
    public decimal ValorPago => CalcularTotalPago();

    [JsonIgnore]
    public string TipoAmbientalDisplay => EnumDisplay.GetName(TipoAmbiental);

    [JsonIgnore]
    public string SituacaoDisplay => EnumDisplay.GetName(Situacao);

    [JsonIgnore]
    public string SituacaoPrazoDisplay => PrazoDisplay.GetName(Situacao, DataPrevistaFim, DataFinal);

    [JsonIgnore]
    public string ClientesAssociadosDisplay { get; set; } = string.Empty;

    public decimal CalcularTotalPago()
    {
        return Pagamentos.Sum(pagamento => pagamento.ValorAssociado);
    }

    public decimal CalcularSaldoPendente()
    {
        return Math.Max(ValorContratado - CalcularTotalPago(), 0M);
    }
}
