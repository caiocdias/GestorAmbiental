using System.Text.RegularExpressions;
using GestorAmbiental.Domain.Common;
using GestorAmbiental.Domain.Enums;

namespace GestorAmbiental.Domain.Entities;

public sealed class Telefone : Entity
{
    private static readonly Regex NonDigitRegex = new("[^0-9]", RegexOptions.Compiled);

    public string Ddi { get; set; } = "55";
    public string Ddd { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public TipoTelefone Tipo { get; set; } = TipoTelefone.CELULAR;
    public bool Principal { get; set; }

    public bool Validar()
    {
        var ddi = ApenasDigitos(Ddi);
        var ddd = ApenasDigitos(Ddd);
        var numero = ApenasDigitos(Numero);

        return ddi.Length is >= 1 and <= 3
            && ddd.Length == 2
            && numero.Length is 8 or 9;
    }

    public string Formatar()
    {
        var ddi = ApenasDigitos(Ddi);
        var ddd = ApenasDigitos(Ddd);
        var numero = ApenasDigitos(Numero);
        var numeroFormatado = numero.Length == 9
            ? $"{numero[..5]}-{numero[5..]}"
            : numero.Length == 8
                ? $"{numero[..4]}-{numero[4..]}"
                : numero;

        return $"+{ddi} ({ddd}) {numeroFormatado}".Trim();
    }

    private static string ApenasDigitos(string valor)
    {
        return NonDigitRegex.Replace(valor, string.Empty);
    }
}
