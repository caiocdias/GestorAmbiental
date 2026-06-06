using GestorAmbiental.Application.Persistence;
using GestorAmbiental.Domain.Entities;

namespace GestorAmbiental.Infrastructure.Persistence;

public sealed class GestorAmbientalDataStore : IGestorAmbientalDataStore
{
    public GestorAmbientalDataStore(IDataFolderProvider dataFolderProvider)
    {
        Clientes = new JsonFileRepository<Cliente>(dataFolderProvider, "clientes");
        Projetos = new JsonFileRepository<Projeto>(dataFolderProvider, "projetos");
        Pagamentos = new JsonFileRepository<Pagamento>(dataFolderProvider, "pagamentos");
    }

    public IRepository<Cliente> Clientes { get; }

    public IRepository<Projeto> Projetos { get; }

    public IRepository<Pagamento> Pagamentos { get; }
}
