using GestorAmbiental.Domain.Common;
using GestorAmbiental.Domain.Display;
using GestorAmbiental.Domain.Enums;
using System.Text.Json.Serialization;

namespace GestorAmbiental.Domain.Entities;

public sealed class Tarefa : Entity
{
    public int ProjetoId { get; set; }
    public int? ClienteId { get; set; }
    public DateTime DataInicio { get; set; } = DateTime.Today;
    public DateTime DataPrevisao { get; set; } = DateTime.Today;
    public DateTime? DataFinal { get; set; }
    public SituacaoTarefa Situacao { get; set; } = SituacaoTarefa.PLANEJADO;

    [JsonIgnore]
    public string ProjetoDisplay { get; set; } = string.Empty;

    [JsonIgnore]
    public string ClienteDisplay { get; set; } = string.Empty;

    [JsonIgnore]
    public string SituacaoDisplay => EnumDisplay.GetName(Situacao);
}
