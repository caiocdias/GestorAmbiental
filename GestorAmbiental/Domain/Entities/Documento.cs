using System.Text.RegularExpressions;
using GestorAmbiental.Domain.Common;
using GestorAmbiental.Domain.Enums;

namespace GestorAmbiental.Domain.Entities;

public sealed class Documento : Entity
{
    private static readonly Regex NonDigitRegex = new("[^0-9]", RegexOptions.Compiled);

    public string Numero { get; set; } = string.Empty;
    public TipoDocumento Tipo { get; set; } = TipoDocumento.OUTRO;
    public bool Principal { get; set; }

    public bool Validar()
    {
        var numero = Numero.Trim();
        var digitos = ApenasDigitos(numero);

        return Tipo switch
        {
            TipoDocumento.CPF => ValidarCpf(digitos),
            TipoDocumento.CNPJ => ValidarCnpj(digitos),
            TipoDocumento.RG => numero.Length >= 5,
            TipoDocumento.PASSAPORTE => Regex.IsMatch(numero, "^[A-Za-z0-9]{5,20}$"),
            TipoDocumento.SEM_DOCUMENTO => true,
            TipoDocumento.OUTRO => !string.IsNullOrWhiteSpace(numero),
            _ => false
        };
    }

    public string Formatar()
    {
        var numero = Numero.Trim();
        var digitos = ApenasDigitos(numero);

        return Tipo switch
        {
            TipoDocumento.CPF when digitos.Length == 11 =>
                $"{digitos[..3]}.{digitos.Substring(3, 3)}.{digitos.Substring(6, 3)}-{digitos[9..]}",
            TipoDocumento.CNPJ when digitos.Length == 14 =>
                $"{digitos[..2]}.{digitos.Substring(2, 3)}.{digitos.Substring(5, 3)}/{digitos.Substring(8, 4)}-{digitos[12..]}",
            TipoDocumento.RG when digitos.Length == 9 =>
                $"{digitos[..2]}.{digitos.Substring(2, 3)}.{digitos.Substring(5, 3)}-{digitos[8]}",
            TipoDocumento.PASSAPORTE => numero.ToUpperInvariant(),
            TipoDocumento.SEM_DOCUMENTO => string.Empty,
            _ => numero
        };
    }

    private static string ApenasDigitos(string valor)
    {
        return NonDigitRegex.Replace(valor, string.Empty);
    }

    private static bool ValidarCpf(string digitos)
    {
        if (digitos.Length != 11 || digitos.Distinct().Count() == 1)
        {
            return false;
        }

        return digitos[9] == CalcularDigitoCpf(digitos[..9], 10)
            && digitos[10] == CalcularDigitoCpf(digitos[..10], 11);
    }

    private static char CalcularDigitoCpf(string digitos, int pesoInicial)
    {
        var soma = 0;

        for (var i = 0; i < digitos.Length; i++)
        {
            soma += (digitos[i] - '0') * (pesoInicial - i);
        }

        var resto = soma % 11;
        var digito = resto < 2 ? 0 : 11 - resto;
        return (char)('0' + digito);
    }

    private static bool ValidarCnpj(string digitos)
    {
        if (digitos.Length != 14 || digitos.Distinct().Count() == 1)
        {
            return false;
        }

        int[] primeiroPeso = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] segundoPeso = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        return digitos[12] == CalcularDigitoCnpj(digitos[..12], primeiroPeso)
            && digitos[13] == CalcularDigitoCnpj(digitos[..13], segundoPeso);
    }

    private static char CalcularDigitoCnpj(string digitos, IReadOnlyList<int> pesos)
    {
        var soma = 0;

        for (var i = 0; i < digitos.Length; i++)
        {
            soma += (digitos[i] - '0') * pesos[i];
        }

        var resto = soma % 11;
        var digito = resto < 2 ? 0 : 11 - resto;
        return (char)('0' + digito);
    }
}
