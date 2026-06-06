using GestorAmbiental.Application.Persistence;
using GestorAmbiental.Domain.Entities;

namespace GestorAmbiental.Infrastructure.Persistence;

public sealed class GestorAmbientalDataStore : IGestorAmbientalDataStore
{
    public GestorAmbientalDataStore(IDataFolderProvider dataFolderProvider)
    {
        Clientes = new JsonFileRepository<Cliente>(dataFolderProvider, "clientes");
        Projetos = new JsonFileRepository<Projeto>(dataFolderProvider, "projetos");
        Tarefas = new JsonFileRepository<Tarefa>(dataFolderProvider, "tarefas");
        Pagamentos = new JsonFileRepository<Pagamento>(dataFolderProvider, "pagamentos");
    }

    public IRepository<Cliente> Clientes { get; }

    public IRepository<Projeto> Projetos { get; }

    public IRepository<Tarefa> Tarefas { get; }

    public IRepository<Pagamento> Pagamentos { get; }
}
