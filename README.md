# Gestor Ambiental

Aplicativo desktop em WPF para gerenciar clientes, projetos ambientais, tarefas e pagamentos. Os dados sao salvos em arquivos JSON dentro de uma pasta escolhida pelo usuario, sem banco de dados local ou servidor externo.

## Funcionalidades

- Cadastro, edicao, exclusao e filtros de clientes.
- Cadastro, edicao, exclusao e filtros de projetos ambientais.
- Associacao de varios clientes a um projeto e de varios projetos a um cliente.
- Cadastro, edicao, exclusao e filtros de tarefas vinculadas a um projeto e, opcionalmente, a um cliente.
- Acompanhamento de prazo em projetos e tarefas, com data prevista, data final e situacao calculada.
- Lancamento, edicao, exclusao e filtros de pagamentos.
- Dashboard analitico com filtro por intervalo de datas, indicadores financeiros e operacionais.
- Grafico de recebido acumulado por data de pagamento.
- Graficos de prazo para projetos e tarefas ativos, separados por situacao calculada.
- Restricao para impedir remover um cliente de um projeto quando ja existe pagamento vinculado ao par cliente/projeto.
- Calculo de valores contratados, pagamentos recebidos e falta a receber.
- Mascara para CPF, CNPJ, RG, CEP e valores monetarios em reais.
- Busca gratuita de endereco por CEP usando ViaCEP, sem login ou chave de API.
- Persistencia da ultima pasta de dados escolhida em `%APPDATA%\GestorAmbiental\data-folder.txt`.

## Dashboard

O dashboard concentra a leitura geral da operacao. Ele possui filtro opcional por data inicial e data final:

- Sem datas preenchidas, usa todo o periodo disponivel.
- Apenas data inicial preenchida, considera tudo a partir dela.
- Apenas data final preenchida, considera tudo ate ela.
- Com as duas datas preenchidas, considera somente o intervalo informado.

O filtro impacta valor contratado, valor recebido, clientes cadastrados, projetos concluidos, tarefas concluidas e o grafico de recebido acumulado. O valor pendente a receber e os graficos de prazo de projetos/tarefas ficam sem filtro de data para mostrar a situacao atual.

## Tecnologias

- .NET 10
- WPF
- C#
- Persistencia em JSON
- ViaCEP para consulta de endereco

## Requisitos

- Windows
- .NET 10 SDK
- Visual Studio com suporte a WPF, ou SDK .NET via terminal

## Como executar

No terminal, a partir da raiz do repositorio:

```powershell
dotnet build GestorAmbiental.slnx
dotnet run --project GestorAmbiental/GestorAmbiental.csproj
```

Ao abrir o aplicativo pela primeira vez, escolha uma pasta para armazenar os dados. O caminho escolhido sera lembrado nas proximas execucoes.

## Como os dados sao salvos

A aplicacao cria arquivos JSON na pasta selecionada pelo usuario:

- `clientes.json`
- `projetos.json`
- `tarefas.json`
- `pagamentos.json`

Esses arquivos sao dados reais do usuario e nao devem ser versionados no Git. A pasta de preferencia do aplicativo guarda apenas o caminho da ultima pasta usada.

## Estrutura do projeto

```text
GestorAmbiental/
  Application/
    Persistence/          Contratos de persistencia
  Assets/                 Icone e recursos visuais do aplicativo
  Domain/
    Common/               Base de entidades
    Display/              Nomes amigaveis para enums
    Entities/             Entidades de negocio
    Enums/                Enumeracoes do dominio
  Infrastructure/
    ExternalServices/     Integracao com ViaCEP
    Persistence/          Persistencia em arquivos JSON
  App.xaml
  MainWindow.xaml
```

## Autor

Desenvolvido por Caio Cezar Dias.

Contato: caiocd007@gmail.com
