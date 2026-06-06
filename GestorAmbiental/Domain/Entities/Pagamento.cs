using GestorAmbiental.Domain.Common;
using GestorAmbiental.Domain.Display;
using GestorAmbiental.Domain.Enums;
using System.Text.Json.Serialization;

namespace GestorAmbiental.Domain.Entities;

public sealed class Pagamento : Entity
{
    public DateTime DataPagamento { get; set; } = DateTime.Today;
    public DateTime DataVencimento { get; set; } = DateTime.Today;
    public decimal ValorTotal { get; set; }
    public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.PIX;
    public SituacaoPagamento Situacao { get; set; } = SituacaoPagamento.PENDENTE;
    public string Observacao { get; set; } = string.Empty;
    public List<PagamentoProjeto> Projetos { get; set; } = [];
    public List<PagamentoCliente> Clientes { get; set; } = [];

    [JsonIgnore]
    public string FormaPagamentoDisplay => EnumDisplay.GetName(FormaPagamento);

    [JsonIgnore]
    public string SituacaoDisplay => EnumDisplay.GetName(Situacao);

    [JsonIgnore]
    public string ProjetosAssociadosDisplay { get; set; } = string.Empty;

    [JsonIgnore]
    public string ClientesAssociadosDisplay { get; set; } = string.Empty;

    public bool ValidarAssociacao()
    {
        return Projetos.Count > 0 || Clientes.Count > 0;
    }
}
