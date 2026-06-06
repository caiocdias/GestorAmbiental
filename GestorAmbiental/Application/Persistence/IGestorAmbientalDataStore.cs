using GestorAmbiental.Domain.Entities;

namespace GestorAmbiental.Application.Persistence;

public interface IGestorAmbientalDataStore
{
    IRepository<Cliente> Clientes { get; }

    IRepository<Projeto> Projetos { get; }

    IRepository<Tarefa> Tarefas { get; }

    IRepository<Pagamento> Pagamentos { get; }
}
