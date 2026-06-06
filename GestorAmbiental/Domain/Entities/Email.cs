using System.Net.Mail;
using GestorAmbiental.Domain.Common;

namespace GestorAmbiental.Domain.Entities;

public sealed class Email : Entity
{
    public string Endereco { get; set; } = string.Empty;
    public bool Principal { get; set; }
    public bool Verificado { get; set; }

    public bool Validar()
    {
        var endereco = Endereco.Trim();

        if (string.IsNullOrWhiteSpace(endereco))
        {
            return false;
        }

        try
        {
            var email = new MailAddress(endereco);
            return email.Address.Equals(endereco, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
