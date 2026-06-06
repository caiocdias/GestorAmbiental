using GestorAmbiental.Domain.Common;
using GestorAmbiental.Domain.Display;
using GestorAmbiental.Domain.Enums;
using System.Text.Json.Serialization;

namespace GestorAmbiental.Domain.Entities;

public sealed class Cliente : Entity
{
    public string Nome { get; set; } = string.Empty;
    public DateTime DataCadastro { get; set; } = DateTime.Today;
    public SituacaoCliente Situacao { get; set; } = SituacaoCliente.ATIVO;
    public List<Documento> Documentos { get; set; } = [];
    public List<Email> Emails { get; set; } = [];
    public List<Telefone> Telefones { get; set; } = [];
    public List<ProjetoCliente> Projetos { get; set; } = [];
    public List<PagamentoCliente> Pagamentos { get; set; } = [];

    [JsonIgnore]
    public Documento? DocumentoPrincipal => Documentos.FirstOrDefault(documento => documento.Principal)
        ?? Documentos.FirstOrDefault();

    [JsonIgnore]
    public Email? EmailPrincipal => Emails.FirstOrDefault(email => email.Principal)
        ?? Emails.FirstOrDefault();

    [JsonIgnore]
    public Telefone? TelefonePrincipal => Telefones.FirstOrDefault(telefone => telefone.Principal)
        ?? Telefones.FirstOrDefault();

    [JsonIgnore]
    public string DocumentoPrincipalFormatado => DocumentoPrincipal?.Formatar() ?? string.Empty;

    [JsonIgnore]
    public string TipoDocumentoPrincipal => DocumentoPrincipal?.Tipo.ToString() ?? string.Empty;

    [JsonIgnore]
    public string EmailPrincipalEndereco => EmailPrincipal?.Endereco ?? string.Empty;

    [JsonIgnore]
    public string TelefonePrincipalFormatado => TelefonePrincipal?.Formatar() ?? string.Empty;

    [JsonIgnore]
    public string SituacaoDisplay => EnumDisplay.GetName(Situacao);

    [JsonIgnore]
    public string TipoDocumentoPrincipalDisplay => DocumentoPrincipal is null
        ? string.Empty
        : EnumDisplay.GetName(DocumentoPrincipal.Tipo);

    [JsonIgnore]
    public string ProjetosAssociadosDisplay { get; set; } = string.Empty;
}
