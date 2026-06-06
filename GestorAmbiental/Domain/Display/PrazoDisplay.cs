using GestorAmbiental.Domain.Enums;

namespace GestorAmbiental.Domain.Display;

public static class PrazoDisplay
{
    public static string GetName(Enum situacao, DateTime? dataPrazo, DateTime? dataFinal)
    {
        if (situacao is SituacaoProjeto.CANCELADO or SituacaoTarefa.CANCELADO)
        {
            return "Cancelada";
        }

        if (dataFinal is not null || situacao is SituacaoProjeto.CONCLUIDO or SituacaoTarefa.CONCLUIDO)
        {
            return "Concluida";
        }

        if (dataPrazo is null)
        {
            return "No prazo";
        }

        var diasAtePrazo = (dataPrazo.Value.Date - DateTime.Today).Days;

        if (diasAtePrazo <= 0)
        {
            return "Vence hoje";
        }

        return diasAtePrazo <= 7
            ? "Vence em 7 dias"
            : "No prazo";
    }
}
