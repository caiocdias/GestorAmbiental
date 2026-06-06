using GestorAmbiental.Domain.Enums;
using System.Reflection;

namespace GestorAmbiental.Domain.Display;

public sealed record EnumOption<TEnum>(TEnum Value, string Display) where TEnum : struct, Enum;

public sealed record EnumFilterOption<TEnum>(TEnum? Value, string Display) where TEnum : struct, Enum;

public static class EnumDisplay
{
    public static string GetName(Enum value)
    {
        return value switch
        {
            SituacaoCliente.ATIVO => "Ativo",
            SituacaoCliente.INATIVO => "Inativo",
            SituacaoCliente.BLOQUEADO => "Bloqueado",

            TipoDocumento.CPF => "CPF",
            TipoDocumento.CNPJ => "CNPJ",
            TipoDocumento.RG => "RG",
            TipoDocumento.PASSAPORTE => "Passaporte",
            TipoDocumento.SEM_DOCUMENTO => "Sem documento",
            TipoDocumento.OUTRO => "Outro",

            TipoTelefone.CELULAR => "Celular",
            TipoTelefone.RESIDENCIAL => "Residencial",
            TipoTelefone.COMERCIAL => "Comercial",
            TipoTelefone.OUTRO => "Outro",

            SituacaoProjeto.PLANEJADO => "Planejado",
            SituacaoProjeto.EM_ANDAMENTO => "Em andamento",
            SituacaoProjeto.PAUSADO => "Pausado",
            SituacaoProjeto.CONCLUIDO => "Concluido",
            SituacaoProjeto.CANCELADO => "Cancelado",

            SituacaoTarefa.PLANEJADO => "Planejado",
            SituacaoTarefa.EM_ANDAMENTO => "Em andamento",
            SituacaoTarefa.CONCLUIDO => "Concluido",
            SituacaoTarefa.CANCELADO => "Cancelado",

            TipoProjetoAmbiental.LICENCIAMENTO_AMBIENTAL => "Licenciamento Ambiental",
            TipoProjetoAmbiental.CONSULTORIA_AMBIENTAL_MENSAL => "Consultoria Ambiental Mensal",
            TipoProjetoAmbiental.PROCESSO_DAIA_IEF => "Processo de DAIA IEF",
            TipoProjetoAmbiental.CONSULTORIA_PSS_CAS_IEF => "Consultoria PSS/CAS IEF",
            TipoProjetoAmbiental.OUTORGA_USO_AGUA => "Outorga Uso de Agua",
            TipoProjetoAmbiental.CADASTRO_REGISTRO_IEF_IBAMA => "Cadastro e Registro IEF/IBAMA",
            TipoProjetoAmbiental.OUTROS => "Outros",

            SituacaoPagamento.PENDENTE => "Pendente",
            SituacaoPagamento.PAGO => "Pago",
            SituacaoPagamento.PARCIAL => "Parcial",
            SituacaoPagamento.CANCELADO => "Cancelado",

            FormaPagamento.PIX => "PIX",
            FormaPagamento.BOLETO => "Boleto",
            FormaPagamento.CARTAO => "Cartao",
            FormaPagamento.TRANSFERENCIA => "Transferencia",
            FormaPagamento.DINHEIRO => "Dinheiro",

            _ => value.ToString()
        };
    }

    public static IReadOnlyList<EnumOption<TEnum>> GetOptions<TEnum>() where TEnum : struct, Enum
    {
        return GetVisibleValues<TEnum>()
            .Select(value => new EnumOption<TEnum>(value, GetName(value)))
            .ToArray();
    }

    public static IReadOnlyList<EnumFilterOption<TEnum>> GetFilterOptions<TEnum>(string allLabel) where TEnum : struct, Enum
    {
        return new[] { new EnumFilterOption<TEnum>(null, allLabel) }
            .Concat(GetVisibleValues<TEnum>().Select(value => new EnumFilterOption<TEnum>(value, GetName(value))))
            .ToArray();
    }

    private static IEnumerable<TEnum> GetVisibleValues<TEnum>() where TEnum : struct, Enum
    {
        return typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.GetCustomAttribute<ObsoleteAttribute>() is null)
            .Select(field => (TEnum)field.GetValue(null)!)
            .Distinct();
    }
}
