# Gestor Ambiental

Aplicativo desktop em WPF para gerenciar clientes, projetos ambientais, tarefas e pagamentos. Os dados sao salvos em arquivos JSON dentro de uma pasta escolhida pelo usuario, sem banco de dados local ou servidor externo.

## Funcionalidades

- Cadastro, edicao, exclusao e filtros de clientes.
- Cadastro, edicao, exclusao e filtros de projetos ambientais.
- Associacao de varios clientes a um projeto e de varios projetos a um cliente.
- Cadastro, edicao, exclusao e filtros de tarefas vinculadas a um projeto e, opcionalmente, a um cliente.
- Lancamento, edicao, exclusao e filtros de pagamentos.
- Restricao para impedir remover um cliente de um projeto quando ja existe pagamento vinculado ao par cliente/projeto.
- Calculo de valores contratados, pagamentos recebidos e falta a receber.
- Mascara para CPF, CNPJ, RG, CEP e valores monetarios em reais.
- Busca gratuita de endereco por CEP usando ViaCEP, sem login ou chave de API.
- Persistencia da ultima pasta de dados escolhida em `%APPDATA%\GestorAmbiental\data-folder.txt`.

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
