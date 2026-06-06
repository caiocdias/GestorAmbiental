using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using GestorAmbiental.Domain.Display;
using GestorAmbiental.Domain.Entities;
using GestorAmbiental.Domain.Enums;
using GestorAmbiental.Infrastructure.Export;
using GestorAmbiental.Infrastructure.ExternalServices;
using GestorAmbiental.Infrastructure.Persistence;
using Microsoft.Win32;

namespace GestorAmbiental;

public partial class MainWindow : Window
{
    private readonly UserSelectedDataFolderProvider _dataFolderProvider = new();
    private readonly GestorAmbientalDataStore _dataStore;
    private readonly ViaCepAddressLookup _viaCepAddressLookup = new();
    private readonly ObservableCollection<Cliente> _clientes = [];
    private readonly ObservableCollection<Projeto> _projetos = [];
    private readonly ObservableCollection<Tarefa> _tarefas = [];
    private readonly ObservableCollection<Pagamento> _pagamentos = [];
    private readonly ObservableCollection<Cliente> _tarefaClientesDisponiveis = [];
    private readonly ObservableCollection<Projeto> _pagamentoProjetosDisponiveis = [];
    private readonly ObservableCollection<Cliente> _pagamentoClientesDisponiveis = [];
    private static readonly Cliente SemClienteTarefa = new() { Id = 0, Nome = "Sem cliente" };
    private static readonly CultureInfo RealCulture = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly Brush DashboardLineBrush = new SolidColorBrush(Color.FromRgb(31, 111, 100));
    private static readonly Brush DashboardGridBrush = new SolidColorBrush(Color.FromRgb(215, 226, 221));
    private static readonly Brush DashboardTextBrush = new SolidColorBrush(Color.FromRgb(56, 81, 77));
    private static readonly Brush DashboardNoPrazoBrush = new SolidColorBrush(Color.FromRgb(31, 111, 100));
    private static readonly Brush DashboardVenceSeteBrush = new SolidColorBrush(Color.FromRgb(214, 166, 41));
    private static readonly Brush DashboardVenceHojeBrush = new SolidColorBrush(Color.FromRgb(160, 68, 62));
    private static readonly Brush DashboardFallbackBrush = new SolidColorBrush(Color.FromRgb(94, 112, 107));
    private ICollectionView? _clientesView;
    private ICollectionView? _projetosView;
    private ICollectionView? _tarefasView;
    private ICollectionView? _pagamentosView;

    private Cliente? _clienteSelecionado;
    private Projeto? _projetoSelecionado;
    private Tarefa? _tarefaSelecionada;
    private Pagamento? _pagamentoSelecionado;
    private bool _aplicandoMascaraDocumento;
    private bool _aplicandoMascaraValorProjeto;
    private bool _aplicandoMascaraValorPagamento;
    private bool _aplicandoMascaraCepProjeto;
    private bool _atualizandoOpcoesTarefa;
    private bool _atualizandoOpcoesPagamento;

    public MainWindow()
    {
        _dataStore = new GestorAmbientalDataStore(_dataFolderProvider);

        InitializeComponent();
        ConfigurarListas();
        ConfigurarCombos();
        LimparFormularioCliente();
        LimparFormularioProjeto();
        LimparFormularioTarefa();
        LimparFormularioPagamento();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        MostrarTela(DashboardView, "Dashboard");
        AtualizarPastaSelecionada();

        if (_dataFolderProvider.DataFolderPath is not null)
        {
            await CarregarDadosAsync();
            return;
        }

        AtualizarDashboard();
        SetStatus("Escolha uma pasta de dados para comecar.");
    }

    private void ConfigurarListas()
    {
        _clientesView = CollectionViewSource.GetDefaultView(_clientes);
        _clientesView.Filter = FiltrarCliente;
        _projetosView = CollectionViewSource.GetDefaultView(_projetos);
        _projetosView.Filter = FiltrarProjeto;
        _tarefasView = CollectionViewSource.GetDefaultView(_tarefas);
        _tarefasView.Filter = FiltrarTarefa;
        _pagamentosView = CollectionViewSource.GetDefaultView(_pagamentos);
        _pagamentosView.Filter = FiltrarPagamento;

        ClientesDataGrid.ItemsSource = _clientesView;
        ProjetosDataGrid.ItemsSource = _projetosView;
        TarefasDataGrid.ItemsSource = _tarefasView;
        PagamentosDataGrid.ItemsSource = _pagamentosView;

        ClienteProjetosListBox.ItemsSource = _projetos;
        ProjetoClientesListBox.ItemsSource = _clientes;
        TarefaProjetoComboBox.ItemsSource = _projetos;
        TarefaClienteComboBox.ItemsSource = _tarefaClientesDisponiveis;
        PagamentoClienteComboBox.ItemsSource = _pagamentoClientesDisponiveis;
        PagamentoProjetoComboBox.ItemsSource = _pagamentoProjetosDisponiveis;
    }

    private void ConfigurarCombos()
    {
        ClienteSituacaoComboBox.ItemsSource = EnumDisplay.GetOptions<SituacaoCliente>();
        ClienteDocumentoTipoComboBox.ItemsSource = EnumDisplay.GetOptions<TipoDocumento>();
        ClienteFiltroSituacaoComboBox.ItemsSource = EnumDisplay.GetFilterOptions<SituacaoCliente>("Todas");
        ClienteFiltroSituacaoComboBox.SelectedIndex = 0;
        ProjetoTipoComboBox.ItemsSource = EnumDisplay.GetOptions<TipoProjetoAmbiental>();
        AtualizarOpcoesSituacaoProjetoPorDataFinal();
        ProjetoFiltroTipoComboBox.ItemsSource = EnumDisplay.GetFilterOptions<TipoProjetoAmbiental>("Todos");
        ProjetoFiltroTipoComboBox.SelectedIndex = 0;
        ProjetoFiltroSituacaoComboBox.ItemsSource = EnumDisplay.GetFilterOptions<SituacaoProjeto>("Todas");
        ProjetoFiltroSituacaoComboBox.SelectedIndex = 0;
        AtualizarOpcoesSituacaoTarefaPorDataFinal();
        TarefaFiltroSituacaoComboBox.ItemsSource = EnumDisplay.GetFilterOptions<SituacaoTarefa>("Todas");
        TarefaFiltroSituacaoComboBox.SelectedIndex = 0;
        PagamentoFormaComboBox.ItemsSource = EnumDisplay.GetOptions<FormaPagamento>();
        PagamentoSituacaoComboBox.ItemsSource = EnumDisplay.GetOptions<SituacaoPagamento>();
        PagamentoFiltroFormaComboBox.ItemsSource = EnumDisplay.GetFilterOptions<FormaPagamento>("Todas");
        PagamentoFiltroFormaComboBox.SelectedIndex = 0;
        PagamentoFiltroSituacaoComboBox.ItemsSource = EnumDisplay.GetFilterOptions<SituacaoPagamento>("Todas");
        PagamentoFiltroSituacaoComboBox.SelectedIndex = 0;
    }

    private async void ChooseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Escolha a pasta onde os dados do Gestor Ambiental serao salvos",
            InitialDirectory = _dataFolderProvider.DataFolderPath
                ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog(this) == true)
        {
            _dataFolderProvider.UseFolder(dialog.FolderName);
            AtualizarPastaSelecionada();
            await CarregarDadosAsync();
        }
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TemPastaSelecionada())
        {
            return;
        }

        await CarregarDadosAsync();
    }

    private void DashboardPeriodoPicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        AtualizarDashboard();
    }

    private void LimparFiltroDashboardButton_Click(object sender, RoutedEventArgs e)
    {
        DashboardDataInicialPicker.SelectedDate = null;
        DashboardDataFinalPicker.SelectedDate = null;
        AtualizarDashboard();
    }

    private void DashboardChart_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        AtualizarDashboard();
    }

    private void DashboardNavButton_Click(object sender, RoutedEventArgs e)
    {
        MostrarTela(DashboardView, "Dashboard");
    }

    private void ClientesNavButton_Click(object sender, RoutedEventArgs e)
    {
        MostrarTela(ClientesView, "Clientes");
    }

    private void ProjetosNavButton_Click(object sender, RoutedEventArgs e)
    {
        MostrarTela(ProjetosView, "Projetos");
    }

    private void TarefasNavButton_Click(object sender, RoutedEventArgs e)
    {
        MostrarTela(TarefasView, "Tarefas");
    }

    private void PagamentosNavButton_Click(object sender, RoutedEventArgs e)
    {
        MostrarTela(PagamentosView, "Pagamentos");
    }

    private async Task CarregarDadosAsync()
    {
        if (!TemPastaSelecionada())
        {
            return;
        }

        try
        {
            SetStatus("Carregando dados...");

            var clientes = (await _dataStore.Clientes.ListarAsync()).ToList();
            var projetos = (await _dataStore.Projetos.ListarAsync()).ToList();
            var tarefas = (await _dataStore.Tarefas.ListarAsync()).ToList();
            var pagamentos = (await _dataStore.Pagamentos.ListarAsync()).ToList();

            SincronizarPagamentosNosProjetos(projetos, pagamentos);
            SincronizarProjetosNosClientes(clientes, projetos);
            AtualizarDisplaysDeAssociacoes(clientes, projetos);
            AtualizarDisplaysDeTarefas(tarefas, clientes, projetos);
            AtualizarDisplaysDePagamentos(pagamentos, clientes, projetos);

            TrocarItens(_clientes, clientes.OrderBy(cliente => cliente.Nome));
            TrocarItens(_projetos, projetos.OrderBy(projeto => projeto.Nome));
            TrocarItens(_tarefas, tarefas.OrderBy(tarefa => tarefa.DataPrevisao).ThenBy(tarefa => tarefa.Id));
            TrocarItens(_pagamentos, pagamentos.OrderByDescending(pagamento => pagamento.DataPagamento));
            AtualizarVisualizacoes();
            LimparFormularioCliente();
            LimparFormularioProjeto();
            LimparFormularioTarefa();
            LimparFormularioPagamento();
            SetStatus("Dados carregados.");
        }
        catch (Exception ex)
        {
            MostrarErro("Nao foi possivel carregar os dados.", ex);
        }
    }

    private async void SalvarClienteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_dataFolderProvider.DataFolderPath is null)
        {
            MostrarErroCliente("Escolha uma pasta de dados antes de salvar o cliente.");
            return;
        }

        var nome = ClienteNomeTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            MostrarErroCliente("Informe o nome do cliente.");
            return;
        }

        var tipoDocumento = ObterValorEnum(ClienteDocumentoTipoComboBox, TipoDocumento.OUTRO);
        var documento = new Documento
        {
            Id = _clienteSelecionado?.Documentos.FirstOrDefault()?.Id ?? 0,
            Numero = NormalizarDocumentoParaSalvar(ClienteDocumentoTextBox.Text, tipoDocumento),
            Tipo = tipoDocumento,
            Principal = true
        };

        if (!documento.Validar())
        {
            MostrarErroCliente(ObterMotivoDocumentoInvalido(documento));
            return;
        }

        var emails = new List<Email>();
        if (!string.IsNullOrWhiteSpace(ClienteEmailTextBox.Text))
        {
            var email = new Email
            {
                Id = _clienteSelecionado?.Emails.FirstOrDefault()?.Id ?? 0,
                Endereco = ClienteEmailTextBox.Text.Trim(),
                Principal = true,
                Verificado = _clienteSelecionado?.Emails.FirstOrDefault()?.Verificado ?? false
            };

            if (!email.Validar())
            {
                MostrarErroCliente("Informe um email valido.");
                return;
            }

            emails.Add(email);
        }

        var telefones = new List<Telefone>();
        if (!string.IsNullOrWhiteSpace(ClienteTelefoneDddTextBox.Text)
            || !string.IsNullOrWhiteSpace(ClienteTelefoneNumeroTextBox.Text))
        {
            var telefone = new Telefone
            {
                Id = _clienteSelecionado?.Telefones.FirstOrDefault()?.Id ?? 0,
                Ddi = ClienteTelefoneDdiTextBox.Text.Trim(),
                Ddd = ClienteTelefoneDddTextBox.Text.Trim(),
                Numero = ClienteTelefoneNumeroTextBox.Text.Trim(),
                Principal = true,
                Tipo = TipoTelefone.CELULAR
            };

            if (!telefone.Validar())
            {
                MostrarErroCliente("Informe um telefone valido com DDI, DDD e numero.");
                return;
            }

            telefones.Add(telefone);
        }

        var projetosSelecionadosIds = ObterProjetosSelecionadosNoCliente();

        try
        {
            var editando = _clienteSelecionado is not null;

            if (_clienteSelecionado is not null)
            {
                var motivoBloqueio = await ObterMotivoRemocaoProjetosDoClienteBloqueadaAsync(
                    _clienteSelecionado.Id,
                    _clienteSelecionado.Projetos.Select(vinculo => vinculo.ProjetoId),
                    projetosSelecionadosIds);

                if (motivoBloqueio is not null)
                {
                    MostrarErroCliente(motivoBloqueio);
                    return;
                }
            }

            var cliente = _clienteSelecionado ?? new Cliente { DataCadastro = DateTime.Today };
            cliente.Nome = nome;
            cliente.Situacao = ObterValorEnum(ClienteSituacaoComboBox, SituacaoCliente.ATIVO);
            cliente.Documentos = [documento];
            cliente.Emails = emails;
            cliente.Telefones = telefones;

            var salvo = await _dataStore.Clientes.SalvarAsync(cliente);
            await AtualizarProjetosDoClienteAsync(salvo.Id, projetosSelecionadosIds);
            await CarregarDadosAsync();

            var mensagem = editando
                ? "Cliente atualizado com sucesso."
                : "Cliente cadastrado com sucesso.";
            LimparFormularioCliente();
            SetStatus(mensagem);
            MessageBox.Show(mensagem, "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MostrarErro("Nao foi possivel salvar o cliente.", ex);
        }
    }

    private void LimparClienteButton_Click(object sender, RoutedEventArgs e)
    {
        LimparFormularioCliente();
    }

    private async void ExcluirClienteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TemPastaSelecionada() || _clienteSelecionado is null)
        {
            SetStatus("Selecione um cliente para excluir.");
            return;
        }

        if (MessageBox.Show("Excluir o cliente selecionado?", "Confirmar exclusao", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var clienteId = _clienteSelecionado.Id;
            await AtualizarProjetosDoClienteAsync(clienteId, []);
            await _dataStore.Clientes.ExcluirAsync(_clienteSelecionado.Id);
            await CarregarDadosAsync();
            SetStatus("Cliente excluido.");
        }
        catch (Exception ex)
        {
            MostrarErro("Nao foi possivel excluir o cliente.", ex);
        }
    }

    private void ClientesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _clienteSelecionado = ClientesDataGrid.SelectedItem as Cliente;

        if (_clienteSelecionado is not null)
        {
            PreencherFormularioCliente(_clienteSelecionado);
            SetStatus($"Editando cliente #{_clienteSelecionado.Id}. Ao salvar, as informacoes atuais serao sobrescritas.");
        }
    }

    private void ClienteFiltro_TextChanged(object sender, TextChangedEventArgs e)
    {
        _clientesView?.Refresh();
    }

    private void ClienteFiltro_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _clientesView?.Refresh();
    }

    private void LimparFiltrosClientesButton_Click(object sender, RoutedEventArgs e)
    {
        ClienteFiltroNomeTextBox.Text = string.Empty;
        ClienteFiltroDocumentoTextBox.Text = string.Empty;
        ClienteFiltroEmailTextBox.Text = string.Empty;
        ClienteFiltroTelefoneTextBox.Text = string.Empty;
        ClienteFiltroSituacaoComboBox.SelectedIndex = 0;
        _clientesView?.Refresh();
    }

    private void ExportarClientesButton_Click(object sender, RoutedEventArgs e)
    {
        ExportarTabela(
            "clientes",
            "Clientes",
            ObterItensVisiveis(_clientesView, _clientes),
            [
                new("ID", cliente => cliente.Id),
                new("Nome", cliente => cliente.Nome),
                new("Situacao", cliente => cliente.SituacaoDisplay),
                new("Tipo doc.", cliente => cliente.TipoDocumentoPrincipalDisplay),
                new("Documento", cliente => cliente.DocumentoPrincipalFormatado),
                new("Email", cliente => cliente.EmailPrincipalEndereco),
                new("Telefone", cliente => cliente.TelefonePrincipalFormatado),
                new("Projetos associados", cliente => cliente.ProjetosAssociadosDisplay),
                new("Cadastro", cliente => FormatarData(cliente.DataCadastro))
            ]);
    }

    private void ClienteDocumentoTipoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AplicarMascaraDocumento();
    }

    private void ClienteDocumentoTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarMascaraDocumento();
    }

    private async void SalvarProjetoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_dataFolderProvider.DataFolderPath is null)
        {
            MostrarErroProjeto("Escolha uma pasta de dados antes de salvar o projeto.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ProjetoNomeTextBox.Text))
        {
            MostrarErroProjeto("Informe o nome do projeto.");
            return;
        }

        if (!TentarLerDecimal(ProjetoValorTextBox.Text, out var valorContratado))
        {
            MostrarErroProjeto("Informe um valor contratado valido.");
            return;
        }

        if (!TentarLerDecimal(ProjetoAreaTextBox.Text, out var areaAfetada))
        {
            MostrarErroProjeto("Informe uma area afetada valida.");
            return;
        }

        var endereco = new Endereco
        {
            Id = _projetoSelecionado?.Endereco.Id ?? 0,
            Cep = ProjetoCepTextBox.Text.Trim(),
            Logradouro = ProjetoLogradouroTextBox.Text.Trim(),
            Numero = ProjetoNumeroEnderecoTextBox.Text.Trim(),
            Bairro = ProjetoBairroTextBox.Text.Trim(),
            Cidade = ProjetoCidadeTextBox.Text.Trim(),
            Estado = ProjetoEstadoTextBox.Text.Trim(),
            Pais = string.IsNullOrWhiteSpace(ProjetoPaisTextBox.Text) ? "Brasil" : ProjetoPaisTextBox.Text.Trim()
        };

        if (!endereco.Validar())
        {
            MostrarErroProjeto("Informe um CEP com 8 digitos ou deixe o campo CEP vazio.");
            return;
        }

        try
        {
            var editando = _projetoSelecionado is not null;
            var projeto = _projetoSelecionado ?? new Projeto();
            var clientesSelecionadosIds = ObterClientesSelecionadosNoProjeto();
            var vinculosAnteriores = projeto.Clientes.ToDictionary(vinculo => vinculo.ClienteId);
            var motivoBloqueio = await ObterMotivoRemocaoClientesDoProjetoBloqueadaAsync(
                projeto.Id,
                vinculosAnteriores.Keys,
                clientesSelecionadosIds);

            if (motivoBloqueio is not null)
            {
                MostrarErroProjeto(motivoBloqueio);
                return;
            }

            var dataInicio = ProjetoDataInicioPicker.SelectedDate ?? DateTime.Today;
            var dataFinal = ProjetoDataFinalPicker.SelectedDate;

            if (dataFinal is not null && dataFinal.Value.Date < dataInicio.Date)
            {
                MostrarErroProjeto("A data final nao pode ser anterior a data de inicio.");
                return;
            }

            var situacaoProjeto = ObterValorEnum(ProjetoSituacaoComboBox, SituacaoProjeto.PLANEJADO);
            if (dataFinal is not null && situacaoProjeto != SituacaoProjeto.CANCELADO)
            {
                situacaoProjeto = SituacaoProjeto.CONCLUIDO;
            }

            projeto.Nome = ProjetoNomeTextBox.Text.Trim();
            projeto.Descricao = ProjetoDescricaoTextBox.Text.Trim();
            projeto.TipoAmbiental = ObterValorEnum(ProjetoTipoComboBox, TipoProjetoAmbiental.OUTROS);
            projeto.Situacao = situacaoProjeto;
            projeto.ValorContratado = valorContratado;
            projeto.AreaAfetadaM2 = areaAfetada;
            projeto.DescricaoImpactoAmbiental = ProjetoImpactoTextBox.Text.Trim();
            projeto.DataInicio = dataInicio.Date;
            projeto.DataPrevistaFim = ProjetoDataPrevistaFimPicker.SelectedDate;
            projeto.DataFinal = dataFinal?.Date;
            projeto.Endereco = endereco;
            projeto.Clientes = CriarVinculosProjetoClientes(projeto.Id, clientesSelecionadosIds, vinculosAnteriores);

            var salvo = await _dataStore.Projetos.SalvarAsync(projeto);

            foreach (var vinculo in salvo.Clientes)
            {
                vinculo.ProjetoId = salvo.Id;
            }

            await _dataStore.Projetos.SalvarAsync(salvo);
            await CarregarDadosAsync();

            var mensagem = editando
                ? "Projeto atualizado com sucesso."
                : "Projeto cadastrado com sucesso.";
            LimparFormularioProjeto();
            SetStatus(mensagem);
            MessageBox.Show(mensagem, "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MostrarErro("Nao foi possivel salvar o projeto.", ex);
        }
    }

    private async void BuscarCepProjetoButton_Click(object sender, RoutedEventArgs e)
    {
        var cep = ProjetoCepTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(cep))
        {
            MostrarErroCep("Informe um CEP para buscar o endereco.");
            return;
        }

        try
        {
            LimparCamposEnderecoProjeto();
            SetStatus("Consultando CEP no ViaCEP...");
            var endereco = await _viaCepAddressLookup.ConsultarAsync(cep);

            if (endereco is null)
            {
                MostrarErroCep("CEP nao encontrado no ViaCEP.");
                return;
            }

            ProjetoCepTextBox.Text = endereco.Cep;
            ProjetoLogradouroTextBox.Text = endereco.Logradouro;
            ProjetoBairroTextBox.Text = endereco.Bairro;
            ProjetoCidadeTextBox.Text = endereco.Cidade;
            ProjetoEstadoTextBox.Text = endereco.Estado;
            ProjetoPaisTextBox.Text = endereco.Pais;
            SetStatus("Endereco preenchido pelo ViaCEP.");
        }
        catch (Exception ex)
        {
            MostrarErro("Nao foi possivel consultar o CEP no ViaCEP.", ex);
        }
    }

    private void LimparProjetoButton_Click(object sender, RoutedEventArgs e)
    {
        LimparFormularioProjeto();
    }

    private async void ExcluirProjetoButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TemPastaSelecionada() || _projetoSelecionado is null)
        {
            SetStatus("Selecione um projeto para excluir.");
            return;
        }

        if (MessageBox.Show("Excluir o projeto selecionado?", "Confirmar exclusao", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _dataStore.Projetos.ExcluirAsync(_projetoSelecionado.Id);
            await CarregarDadosAsync();
            SetStatus("Projeto excluido.");
        }
        catch (Exception ex)
        {
            MostrarErro("Nao foi possivel excluir o projeto.", ex);
        }
    }

    private void ProjetosDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _projetoSelecionado = ProjetosDataGrid.SelectedItem as Projeto;

        if (_projetoSelecionado is not null)
        {
            PreencherFormularioProjeto(_projetoSelecionado);
            SetStatus($"Editando projeto #{_projetoSelecionado.Id}. Ao salvar, as informacoes atuais serao sobrescritas.");
        }
    }

    private void ProjetoFiltro_TextChanged(object sender, TextChangedEventArgs e)
    {
        _projetosView?.Refresh();
    }

    private void ProjetoFiltro_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _projetosView?.Refresh();
    }

    private void LimparFiltrosProjetosButton_Click(object sender, RoutedEventArgs e)
    {
        ProjetoFiltroNomeTextBox.Text = string.Empty;
        ProjetoFiltroClienteTextBox.Text = string.Empty;
        ProjetoFiltroEnderecoTextBox.Text = string.Empty;
        ProjetoFiltroTipoComboBox.SelectedIndex = 0;
        ProjetoFiltroSituacaoComboBox.SelectedIndex = 0;
        _projetosView?.Refresh();
    }

    private void ExportarProjetosButton_Click(object sender, RoutedEventArgs e)
    {
        ExportarTabela(
            "projetos",
            "Projetos",
            ObterItensVisiveis(_projetosView, _projetos),
            [
                new("ID", projeto => projeto.Id),
                new("Nome", projeto => projeto.Nome),
                new("Tipo", projeto => projeto.TipoAmbientalDisplay),
                new("Situacao", projeto => projeto.SituacaoDisplay),
                new("Previsao fim", projeto => FormatarData(projeto.DataPrevistaFim)),
                new("Final", projeto => FormatarData(projeto.DataFinal)),
                new("Prazo", projeto => projeto.SituacaoPrazoDisplay),
                new("Valor contratado", projeto => projeto.ValorContratado.ToString("C", RealCulture)),
                new("Soma pagamentos", projeto => projeto.ValorPago.ToString("C", RealCulture)),
                new("Falta receber", projeto => projeto.SaldoPendente.ToString("C", RealCulture)),
                new("Clientes associados", projeto => projeto.ClientesAssociadosDisplay),
                new("Cidade", projeto => projeto.Endereco.Cidade)
            ]);
    }

    private void ProjetoValorTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarMascaraMoedaProjeto();
    }

    private void ProjetoCepTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarMascaraCepProjeto();
    }

    private void ProjetoDataFinalPicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        var possuiDataFinal = ProjetoDataFinalPicker.SelectedDate is not null;
        AtualizarOpcoesSituacaoProjetoPorDataFinal(
            definirConcluido: possuiDataFinal,
            definirEmAndamentoSeConcluidoSemDataFinal: !possuiDataFinal);
    }

    private void TarefaProjetoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AtualizarOpcoesClienteDaTarefa();
    }

    private void TarefaDataFinalPicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        var possuiDataFinal = TarefaDataFinalPicker.SelectedDate is not null;
        AtualizarOpcoesSituacaoTarefaPorDataFinal(
            definirConcluido: possuiDataFinal,
            definirEmAndamentoSeConcluidoSemDataFinal: !possuiDataFinal);
    }

    private async void SalvarTarefaButton_Click(object sender, RoutedEventArgs e)
    {
        if (_dataFolderProvider.DataFolderPath is null)
        {
            MostrarErroTarefa("Escolha uma pasta de dados antes de salvar a tarefa.");
            return;
        }

        var descricao = TarefaDescricaoTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(descricao))
        {
            MostrarErroTarefa("Informe a descricao da tarefa.");
            return;
        }

        if (TarefaProjetoComboBox.SelectedItem is not Projeto projeto)
        {
            MostrarErroTarefa("Selecione o projeto da tarefa.");
            return;
        }

        if (TarefaDataInicioPicker.SelectedDate is not DateTime dataInicio)
        {
            MostrarErroTarefa("Informe a data de inicio da tarefa.");
            return;
        }

        if (TarefaDataPrevisaoPicker.SelectedDate is not DateTime dataPrevisao)
        {
            MostrarErroTarefa("Informe a data de previsao da tarefa.");
            return;
        }

        if (dataPrevisao.Date < dataInicio.Date)
        {
            MostrarErroTarefa("A data de previsao nao pode ser anterior a data de inicio.");
            return;
        }

        var dataFinal = TarefaDataFinalPicker.SelectedDate;

        if (dataFinal is not null && dataFinal.Value.Date < dataInicio.Date)
        {
            MostrarErroTarefa("A data final nao pode ser anterior a data de inicio.");
            return;
        }

        var clienteSelecionado = TarefaClienteComboBox.SelectedItem as Cliente;
        int? clienteId = clienteSelecionado is null || clienteSelecionado.Id <= 0
            ? null
            : clienteSelecionado.Id;

        if (clienteId is not null && !ProjetoTemCliente(projeto, clienteId.Value))
        {
            MostrarErroTarefa("O cliente selecionado precisa estar associado ao projeto da tarefa.");
            return;
        }

        try
        {
            var editando = _tarefaSelecionada is not null;
            var tarefa = _tarefaSelecionada ?? new Tarefa();
            var situacaoTarefa = ObterValorEnum(TarefaSituacaoComboBox, SituacaoTarefa.PLANEJADO);
            if (dataFinal is not null && situacaoTarefa != SituacaoTarefa.CANCELADO)
            {
                situacaoTarefa = SituacaoTarefa.CONCLUIDO;
            }

            tarefa.Descricao = descricao;
            tarefa.ProjetoId = projeto.Id;
            tarefa.ClienteId = clienteId;
            tarefa.Situacao = situacaoTarefa;
            tarefa.DataInicio = dataInicio.Date;
            tarefa.DataPrevisao = dataPrevisao.Date;
            tarefa.DataFinal = dataFinal?.Date;

            await _dataStore.Tarefas.SalvarAsync(tarefa);
            await CarregarDadosAsync();

            var mensagem = editando
                ? "Tarefa atualizada com sucesso."
                : "Tarefa cadastrada com sucesso.";
            LimparFormularioTarefa();
            SetStatus(mensagem);
            MessageBox.Show(mensagem, "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MostrarErro("Nao foi possivel salvar a tarefa.", ex);
        }
    }

    private void LimparTarefaButton_Click(object sender, RoutedEventArgs e)
    {
        LimparFormularioTarefa();
    }

    private async void ExcluirTarefaButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TemPastaSelecionada() || _tarefaSelecionada is null)
        {
            SetStatus("Selecione uma tarefa para excluir.");
            return;
        }

        if (MessageBox.Show("Excluir a tarefa selecionada?", "Confirmar exclusao", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _dataStore.Tarefas.ExcluirAsync(_tarefaSelecionada.Id);
            await CarregarDadosAsync();
            var mensagem = "Tarefa excluida com sucesso.";
            SetStatus(mensagem);
            MessageBox.Show(mensagem, "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MostrarErro("Nao foi possivel excluir a tarefa.", ex);
        }
    }

    private void TarefasDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _tarefaSelecionada = TarefasDataGrid.SelectedItem as Tarefa;

        if (_tarefaSelecionada is not null)
        {
            PreencherFormularioTarefa(_tarefaSelecionada);
            SetStatus($"Editando tarefa #{_tarefaSelecionada.Id}. Ao salvar, as informacoes atuais serao sobrescritas.");
        }
    }

    private void TarefaFiltro_TextChanged(object sender, TextChangedEventArgs e)
    {
        _tarefasView?.Refresh();
    }

    private void TarefaFiltro_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _tarefasView?.Refresh();
    }

    private void LimparFiltrosTarefasButton_Click(object sender, RoutedEventArgs e)
    {
        TarefaFiltroDescricaoTextBox.Text = string.Empty;
        TarefaFiltroProjetoTextBox.Text = string.Empty;
        TarefaFiltroClienteTextBox.Text = string.Empty;
        TarefaFiltroInicioTextBox.Text = string.Empty;
        TarefaFiltroPrevisaoTextBox.Text = string.Empty;
        TarefaFiltroFinalTextBox.Text = string.Empty;
        TarefaFiltroSituacaoComboBox.SelectedIndex = 0;
        _tarefasView?.Refresh();
    }

    private void ExportarTarefasButton_Click(object sender, RoutedEventArgs e)
    {
        ExportarTabela(
            "tarefas",
            "Tarefas",
            ObterItensVisiveis(_tarefasView, _tarefas),
            [
                new("ID", tarefa => tarefa.Id),
                new("Descricao", tarefa => tarefa.Descricao),
                new("Projeto", tarefa => tarefa.ProjetoDisplay),
                new("Cliente", tarefa => tarefa.ClienteDisplay),
                new("Situacao", tarefa => tarefa.SituacaoDisplay),
                new("Inicio", tarefa => FormatarData(tarefa.DataInicio)),
                new("Previsao", tarefa => FormatarData(tarefa.DataPrevisao)),
                new("Final", tarefa => FormatarData(tarefa.DataFinal)),
                new("Prazo", tarefa => tarefa.SituacaoPrazoDisplay)
            ]);
    }

    private void PagamentoProjetoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AtualizarOpcoesPagamentoPorAssociacao();
    }

    private void PagamentoClienteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AtualizarOpcoesPagamentoPorAssociacao();
    }

    private async void SalvarPagamentoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_dataFolderProvider.DataFolderPath is null)
        {
            MostrarErroPagamento("Escolha uma pasta de dados antes de salvar o pagamento.");
            return;
        }

        if (!TentarLerDecimal(PagamentoValorTextBox.Text, out var valorTotal) || valorTotal <= 0)
        {
            MostrarErroPagamento("Informe um valor de pagamento valido.");
            return;
        }

        var projeto = PagamentoProjetoComboBox.SelectedItem as Projeto;
        var cliente = PagamentoClienteComboBox.SelectedItem as Cliente;

        if (projeto is null && cliente is null)
        {
            MostrarErroPagamento("Associe o pagamento a um projeto, a um cliente, ou a ambos.");
            return;
        }

        try
        {
            var editando = _pagamentoSelecionado is not null;
            var pagamento = _pagamentoSelecionado ?? new Pagamento();
            var pagamentoProjetoAnterior = pagamento.Projetos.FirstOrDefault();
            var pagamentoClienteAnterior = pagamento.Clientes.FirstOrDefault();

            pagamento.ValorTotal = valorTotal;
            pagamento.FormaPagamento = ObterValorEnum(PagamentoFormaComboBox, FormaPagamento.PIX);
            pagamento.Situacao = ObterValorEnum(PagamentoSituacaoComboBox, SituacaoPagamento.PENDENTE);
            pagamento.DataPagamento = PagamentoDataPicker.SelectedDate ?? DateTime.Today;
            pagamento.DataVencimento = pagamento.DataPagamento;
            pagamento.Observacao = PagamentoObservacaoTextBox.Text.Trim();
            pagamento.Projetos = projeto is null
                ? []
                : [
                    new PagamentoProjeto
                    {
                        Id = pagamentoProjetoAnterior?.Id ?? 0,
                        PagamentoId = pagamento.Id,
                        ProjetoId = projeto.Id,
                        ValorAssociado = valorTotal
                    }
                ];
            pagamento.Clientes = cliente is null
                ? []
                : [
                    new PagamentoCliente
                    {
                        Id = pagamentoClienteAnterior?.Id ?? 0,
                        PagamentoId = pagamento.Id,
                        ClienteId = cliente.Id,
                        ValorAssociado = valorTotal
                    }
                ];

            if (!pagamento.ValidarAssociacao())
            {
                MostrarErroPagamento("Associe o pagamento antes de salvar.");
                return;
            }

            var salvo = await _dataStore.Pagamentos.SalvarAsync(pagamento);

            foreach (var associado in salvo.Projetos)
            {
                associado.PagamentoId = salvo.Id;
            }

            foreach (var associado in salvo.Clientes)
            {
                associado.PagamentoId = salvo.Id;
            }

            await _dataStore.Pagamentos.SalvarAsync(salvo);
            await CarregarDadosAsync();
            var mensagem = editando
                ? "Pagamento atualizado com sucesso."
                : "Pagamento lancado com sucesso.";
            LimparFormularioPagamento();
            SetStatus(mensagem);
            MessageBox.Show(mensagem, "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MostrarErro("Nao foi possivel salvar o pagamento.", ex);
        }
    }

    private void LimparPagamentoButton_Click(object sender, RoutedEventArgs e)
    {
        LimparFormularioPagamento();
    }

    private async void ExcluirPagamentoButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TemPastaSelecionada() || _pagamentoSelecionado is null)
        {
            SetStatus("Selecione um pagamento para excluir.");
            return;
        }

        if (MessageBox.Show("Excluir o pagamento selecionado?", "Confirmar exclusao", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _dataStore.Pagamentos.ExcluirAsync(_pagamentoSelecionado.Id);
            await CarregarDadosAsync();
            SetStatus("Pagamento excluido.");
        }
        catch (Exception ex)
        {
            MostrarErro("Nao foi possivel excluir o pagamento.", ex);
        }
    }

    private void PagamentosDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _pagamentoSelecionado = PagamentosDataGrid.SelectedItem as Pagamento;

        if (_pagamentoSelecionado is not null)
        {
            PreencherFormularioPagamento(_pagamentoSelecionado);
            SetStatus($"Editando pagamento #{_pagamentoSelecionado.Id}. Ao salvar, as informacoes atuais serao sobrescritas.");
        }
    }

    private void PagamentoFiltro_TextChanged(object sender, TextChangedEventArgs e)
    {
        _pagamentosView?.Refresh();
    }

    private void PagamentoFiltro_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _pagamentosView?.Refresh();
    }

    private void LimparFiltrosPagamentosButton_Click(object sender, RoutedEventArgs e)
    {
        PagamentoFiltroValorTextBox.Text = string.Empty;
        PagamentoFiltroDataTextBox.Text = string.Empty;
        PagamentoFiltroProjetoTextBox.Text = string.Empty;
        PagamentoFiltroClienteTextBox.Text = string.Empty;
        PagamentoFiltroObservacaoTextBox.Text = string.Empty;
        PagamentoFiltroFormaComboBox.SelectedIndex = 0;
        PagamentoFiltroSituacaoComboBox.SelectedIndex = 0;
        _pagamentosView?.Refresh();
    }

    private void ExportarPagamentosButton_Click(object sender, RoutedEventArgs e)
    {
        ExportarTabela(
            "pagamentos",
            "Pagamentos",
            ObterItensVisiveis(_pagamentosView, _pagamentos),
            [
                new("ID", pagamento => pagamento.Id),
                new("Valor", pagamento => pagamento.ValorTotal.ToString("C", RealCulture)),
                new("Forma", pagamento => pagamento.FormaPagamentoDisplay),
                new("Situacao", pagamento => pagamento.SituacaoDisplay),
                new("Pagamento", pagamento => FormatarData(pagamento.DataPagamento)),
                new("Projeto", pagamento => pagamento.ProjetosAssociadosDisplay),
                new("Cliente", pagamento => pagamento.ClientesAssociadosDisplay),
                new("Observacao", pagamento => pagamento.Observacao)
            ]);
    }

    private void PagamentoValorTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        AplicarMascaraMoedaPagamento();
    }

    private void PreencherFormularioCliente(Cliente cliente)
    {
        ClienteFormTitleTextBlock.Text = $"Editando cliente #{cliente.Id}";
        ClienteEditWarningBorder.Visibility = Visibility.Visible;
        ClienteNomeTextBox.Text = cliente.Nome;
        ClienteSituacaoComboBox.SelectedValue = cliente.Situacao;

        var documento = cliente.Documentos.FirstOrDefault();
        ClienteDocumentoTipoComboBox.SelectedValue = documento?.Tipo ?? TipoDocumento.CPF;
        ClienteDocumentoTextBox.Text = documento?.Numero ?? string.Empty;
        AplicarMascaraDocumento();

        var email = cliente.Emails.FirstOrDefault();
        ClienteEmailTextBox.Text = email?.Endereco ?? string.Empty;

        var telefone = cliente.Telefones.FirstOrDefault();
        ClienteTelefoneDdiTextBox.Text = telefone?.Ddi ?? "55";
        ClienteTelefoneDddTextBox.Text = telefone?.Ddd ?? string.Empty;
        ClienteTelefoneNumeroTextBox.Text = telefone?.Numero ?? string.Empty;
        SelecionarProjetosDoCliente(cliente);
    }

    private void LimparFormularioCliente()
    {
        _clienteSelecionado = null;
        ClientesDataGrid.SelectedItem = null;
        ClienteFormTitleTextBlock.Text = "Cadastro de cliente";
        ClienteEditWarningBorder.Visibility = Visibility.Collapsed;
        ClienteNomeTextBox.Text = string.Empty;
        ClienteSituacaoComboBox.SelectedValue = SituacaoCliente.ATIVO;
        ClienteDocumentoTipoComboBox.SelectedValue = TipoDocumento.CPF;
        ClienteDocumentoTextBox.Text = string.Empty;
        ClienteEmailTextBox.Text = string.Empty;
        ClienteTelefoneDdiTextBox.Text = "55";
        ClienteTelefoneDddTextBox.Text = string.Empty;
        ClienteTelefoneNumeroTextBox.Text = string.Empty;
        ClienteProjetosListBox.UnselectAll();
    }

    private void PreencherFormularioProjeto(Projeto projeto)
    {
        ProjetoFormTitleTextBlock.Text = $"Editando projeto #{projeto.Id}";
        ProjetoEditWarningBorder.Visibility = Visibility.Visible;
        ProjetoNomeTextBox.Text = projeto.Nome;
        ProjetoDescricaoTextBox.Text = projeto.Descricao;
        ProjetoTipoComboBox.SelectedValue = projeto.TipoAmbiental;
        DefinirValorProjeto(projeto.ValorContratado);
        ProjetoAreaTextBox.Text = projeto.AreaAfetadaM2.ToString("N2", CultureInfo.CurrentCulture);
        ProjetoImpactoTextBox.Text = projeto.DescricaoImpactoAmbiental;
        ProjetoDataInicioPicker.SelectedDate = projeto.DataInicio;
        ProjetoDataPrevistaFimPicker.SelectedDate = projeto.DataPrevistaFim;
        ProjetoDataFinalPicker.SelectedDate = projeto.DataFinal;
        AtualizarOpcoesSituacaoProjetoPorDataFinal();
        ProjetoSituacaoComboBox.SelectedValue = projeto.DataFinal is not null
            && projeto.Situacao is not (SituacaoProjeto.CONCLUIDO or SituacaoProjeto.CANCELADO)
                ? SituacaoProjeto.CONCLUIDO
                : projeto.Situacao;

        SelecionarClientesDoProjeto(projeto);

        ProjetoCepTextBox.Text = projeto.Endereco.Cep;
        AplicarMascaraCepProjeto();
        ProjetoLogradouroTextBox.Text = projeto.Endereco.Logradouro;
        ProjetoNumeroEnderecoTextBox.Text = projeto.Endereco.Numero;
        ProjetoBairroTextBox.Text = projeto.Endereco.Bairro;
        ProjetoCidadeTextBox.Text = projeto.Endereco.Cidade;
        ProjetoEstadoTextBox.Text = projeto.Endereco.Estado;
        ProjetoPaisTextBox.Text = projeto.Endereco.Pais;
    }

    private void LimparFormularioProjeto()
    {
        _projetoSelecionado = null;
        ProjetosDataGrid.SelectedItem = null;
        ProjetoFormTitleTextBlock.Text = "Cadastro de projeto";
        ProjetoEditWarningBorder.Visibility = Visibility.Collapsed;
        ProjetoNomeTextBox.Text = string.Empty;
        ProjetoDescricaoTextBox.Text = string.Empty;
        ProjetoTipoComboBox.SelectedValue = TipoProjetoAmbiental.OUTROS;
        ProjetoSituacaoComboBox.SelectedValue = SituacaoProjeto.PLANEJADO;
        ProjetoClientesListBox.UnselectAll();
        DefinirValorProjeto(0M);
        ProjetoAreaTextBox.Text = "0";
        ProjetoImpactoTextBox.Text = string.Empty;
        ProjetoDataInicioPicker.SelectedDate = DateTime.Today;
        ProjetoDataPrevistaFimPicker.SelectedDate = null;
        ProjetoDataFinalPicker.SelectedDate = null;
        AtualizarOpcoesSituacaoProjetoPorDataFinal();
        ProjetoSituacaoComboBox.SelectedValue = SituacaoProjeto.PLANEJADO;
        ProjetoCepTextBox.Text = string.Empty;
        LimparCamposEnderecoProjeto("Brasil");
    }

    private void LimparCamposEnderecoProjeto(string paisPadrao = "")
    {
        ProjetoLogradouroTextBox.Text = string.Empty;
        ProjetoNumeroEnderecoTextBox.Text = string.Empty;
        ProjetoBairroTextBox.Text = string.Empty;
        ProjetoCidadeTextBox.Text = string.Empty;
        ProjetoEstadoTextBox.Text = string.Empty;
        ProjetoPaisTextBox.Text = paisPadrao;
    }

    private void PreencherFormularioTarefa(Tarefa tarefa)
    {
        TarefaFormTitleTextBlock.Text = $"Editando tarefa #{tarefa.Id}";
        TarefaEditWarningBorder.Visibility = Visibility.Visible;
        TarefaDescricaoTextBox.Text = tarefa.Descricao;
        TarefaProjetoComboBox.SelectedItem = _projetos.FirstOrDefault(projeto => projeto.Id == tarefa.ProjetoId);
        AtualizarOpcoesClienteDaTarefa();
        TarefaClienteComboBox.SelectedItem = tarefa.ClienteId is null
            ? _tarefaClientesDisponiveis.FirstOrDefault(cliente => cliente.Id == 0)
            : _tarefaClientesDisponiveis.FirstOrDefault(cliente => cliente.Id == tarefa.ClienteId.Value);
        TarefaDataInicioPicker.SelectedDate = tarefa.DataInicio;
        TarefaDataPrevisaoPicker.SelectedDate = tarefa.DataPrevisao;
        TarefaDataFinalPicker.SelectedDate = tarefa.DataFinal;
        AtualizarOpcoesSituacaoTarefaPorDataFinal();
        TarefaSituacaoComboBox.SelectedValue = tarefa.DataFinal is not null
            && tarefa.Situacao is not (SituacaoTarefa.CONCLUIDO or SituacaoTarefa.CANCELADO)
                ? SituacaoTarefa.CONCLUIDO
                : tarefa.Situacao;
    }

    private void LimparFormularioTarefa()
    {
        _tarefaSelecionada = null;
        TarefasDataGrid.SelectedItem = null;
        TarefaFormTitleTextBlock.Text = "Cadastro de tarefa";
        TarefaEditWarningBorder.Visibility = Visibility.Collapsed;
        TarefaDescricaoTextBox.Text = string.Empty;
        TarefaProjetoComboBox.SelectedItem = null;
        AtualizarOpcoesClienteDaTarefa();
        TarefaClienteComboBox.SelectedItem = _tarefaClientesDisponiveis.FirstOrDefault(cliente => cliente.Id == 0);
        TarefaDataInicioPicker.SelectedDate = DateTime.Today;
        TarefaDataPrevisaoPicker.SelectedDate = DateTime.Today;
        TarefaDataFinalPicker.SelectedDate = null;
        AtualizarOpcoesSituacaoTarefaPorDataFinal();
        TarefaSituacaoComboBox.SelectedValue = SituacaoTarefa.PLANEJADO;
    }

    private void PreencherFormularioPagamento(Pagamento pagamento)
    {
        PagamentoFormTitleTextBlock.Text = $"Editando pagamento #{pagamento.Id}";
        PagamentoEditWarningBorder.Visibility = Visibility.Visible;
        DefinirValorPagamento(pagamento.ValorTotal);
        PagamentoFormaComboBox.SelectedValue = pagamento.FormaPagamento;
        PagamentoSituacaoComboBox.SelectedValue = pagamento.Situacao;
        PagamentoDataPicker.SelectedDate = pagamento.DataPagamento;
        PagamentoObservacaoTextBox.Text = pagamento.Observacao;

        var projetoId = pagamento.Projetos.FirstOrDefault()?.ProjetoId;
        AtualizarOpcoesPagamentoPorAssociacao();
        PagamentoProjetoComboBox.SelectedItem = _pagamentoProjetosDisponiveis.FirstOrDefault(projeto => projeto.Id == projetoId);
        AtualizarOpcoesPagamentoPorAssociacao();

        var clienteId = pagamento.Clientes.FirstOrDefault()?.ClienteId;
        PagamentoClienteComboBox.SelectedItem = _pagamentoClientesDisponiveis.FirstOrDefault(cliente => cliente.Id == clienteId);
        AtualizarOpcoesPagamentoPorAssociacao();
    }

    private void LimparFormularioPagamento()
    {
        _pagamentoSelecionado = null;
        PagamentosDataGrid.SelectedItem = null;
        PagamentoFormTitleTextBlock.Text = "Lancamento de pagamento";
        PagamentoEditWarningBorder.Visibility = Visibility.Collapsed;
        DefinirValorPagamento(0M);
        PagamentoFormaComboBox.SelectedValue = FormaPagamento.PIX;
        PagamentoSituacaoComboBox.SelectedValue = SituacaoPagamento.PENDENTE;
        PagamentoDataPicker.SelectedDate = DateTime.Today;
        PagamentoProjetoComboBox.SelectedItem = null;
        PagamentoClienteComboBox.SelectedItem = null;
        AtualizarOpcoesPagamentoPorAssociacao();
        PagamentoObservacaoTextBox.Text = string.Empty;
    }

    private void SelecionarCliente(int id)
    {
        ClientesDataGrid.SelectedItem = _clientes.FirstOrDefault(cliente => cliente.Id == id);
        ClientesDataGrid.ScrollIntoView(ClientesDataGrid.SelectedItem);
    }

    private void SelecionarProjeto(int id)
    {
        ProjetosDataGrid.SelectedItem = _projetos.FirstOrDefault(projeto => projeto.Id == id);
        ProjetosDataGrid.ScrollIntoView(ProjetosDataGrid.SelectedItem);
    }

    private void SelecionarPagamento(int id)
    {
        PagamentosDataGrid.SelectedItem = _pagamentos.FirstOrDefault(pagamento => pagamento.Id == id);
        PagamentosDataGrid.ScrollIntoView(PagamentosDataGrid.SelectedItem);
    }

    private void AtualizarDashboard()
    {
        if (DashboardValorContratadoPeriodoTextBlock is null
            || DashboardRecebidoLineCanvas is null
            || DashboardProjetosPrazoPieCanvas is null
            || DashboardTarefasPrazoPieCanvas is null)
        {
            return;
        }

        var (dataInicial, dataFinal, periodoValido) = ObterPeriodoDashboard();
        AtualizarResumoPeriodoDashboard(dataInicial, dataFinal, periodoValido);

        var projetosNoPeriodo = periodoValido
            ? _projetos.Where(projeto => EstaNoPeriodo(projeto.DataInicio, dataInicial, dataFinal)).ToList()
            : new List<Projeto>();
        var clientesNoPeriodo = periodoValido
            ? _clientes.Where(cliente => EstaNoPeriodo(cliente.DataCadastro, dataInicial, dataFinal)).ToList()
            : new List<Cliente>();
        var pagamentosRecebidosNoPeriodo = periodoValido
            ? _pagamentos
                .Where(PagamentoContaComoRecebido)
                .Where(pagamento => EstaNoPeriodo(pagamento.DataPagamento, dataInicial, dataFinal))
                .ToList()
            : new List<Pagamento>();
        var projetosConcluidosNoPeriodo = periodoValido
            ? _projetos.Count(projeto =>
                projeto.Situacao == SituacaoProjeto.CONCLUIDO
                && EstaNoPeriodo(projeto.DataFinal, dataInicial, dataFinal))
            : 0;
        var tarefasConcluidasNoPeriodo = periodoValido
            ? _tarefas.Count(tarefa =>
                tarefa.Situacao == SituacaoTarefa.CONCLUIDO
                && EstaNoPeriodo(tarefa.DataFinal, dataInicial, dataFinal))
            : 0;

        DashboardValorContratadoPeriodoTextBlock.Text = projetosNoPeriodo
            .Sum(projeto => projeto.ValorContratado)
            .ToString("C", RealCulture);
        DashboardValorRecebidoPeriodoTextBlock.Text = pagamentosRecebidosNoPeriodo
            .Sum(pagamento => pagamento.ValorTotal)
            .ToString("C", RealCulture);
        DashboardValorPendenteTextBlock.Text = _projetos
            .Sum(projeto => projeto.CalcularSaldoPendente())
            .ToString("C", RealCulture);
        DashboardClientesPeriodoTextBlock.Text = clientesNoPeriodo.Count.ToString("N0", CultureInfo.CurrentCulture);
        DashboardProjetosConcluidosPeriodoTextBlock.Text = projetosConcluidosNoPeriodo.ToString("N0", CultureInfo.CurrentCulture);
        DashboardTarefasConcluidasPeriodoTextBlock.Text = tarefasConcluidasNoPeriodo.ToString("N0", CultureInfo.CurrentCulture);

        DesenharGraficoLinhaRecebido(pagamentosRecebidosNoPeriodo);
        DesenharGraficoPizza(
            DashboardProjetosPrazoPieCanvas,
            DashboardProjetosPrazoLegendPanel,
            CriarFatiasPrazo(_projetos
                .Where(projeto => projeto.DataFinal is null
                    && projeto.Situacao is not (SituacaoProjeto.CONCLUIDO or SituacaoProjeto.CANCELADO))
                .Select(projeto => projeto.SituacaoPrazoDisplay)));
        DesenharGraficoPizza(
            DashboardTarefasPrazoPieCanvas,
            DashboardTarefasPrazoLegendPanel,
            CriarFatiasPrazo(_tarefas
                .Where(tarefa => tarefa.DataFinal is null
                    && tarefa.Situacao is not (SituacaoTarefa.CONCLUIDO or SituacaoTarefa.CANCELADO))
                .Select(tarefa => tarefa.SituacaoPrazoDisplay)));
    }

    private (DateTime? DataInicial, DateTime? DataFinal, bool Valido) ObterPeriodoDashboard()
    {
        var dataInicial = DashboardDataInicialPicker.SelectedDate?.Date;
        var dataFinal = DashboardDataFinalPicker.SelectedDate?.Date;
        var periodoValido = dataInicial is null || dataFinal is null || dataInicial <= dataFinal;

        return (dataInicial, dataFinal, periodoValido);
    }

    private void AtualizarResumoPeriodoDashboard(DateTime? dataInicial, DateTime? dataFinal, bool periodoValido)
    {
        DashboardPeriodoResumoTextBlock.Foreground = periodoValido
            ? DashboardTextBrush
            : DashboardVenceHojeBrush;

        if (!periodoValido)
        {
            DashboardPeriodoResumoTextBlock.Text = "Periodo invalido: a data inicial deve ser menor ou igual a data final.";
        }
        else if (dataInicial is null && dataFinal is null)
        {
            DashboardPeriodoResumoTextBlock.Text = "Todo o periodo disponivel";
        }
        else if (dataInicial is not null && dataFinal is null)
        {
            DashboardPeriodoResumoTextBlock.Text = $"A partir de {dataInicial:dd/MM/yyyy}";
        }
        else if (dataInicial is null && dataFinal is not null)
        {
            DashboardPeriodoResumoTextBlock.Text = $"Ate {dataFinal:dd/MM/yyyy}";
        }
        else
        {
            DashboardPeriodoResumoTextBlock.Text = $"{dataInicial:dd/MM/yyyy} ate {dataFinal:dd/MM/yyyy}";
        }
    }

    private static bool EstaNoPeriodo(DateTime data, DateTime? dataInicial, DateTime? dataFinal)
    {
        var dataReferencia = data.Date;

        return (dataInicial is null || dataReferencia >= dataInicial.Value)
            && (dataFinal is null || dataReferencia <= dataFinal.Value);
    }

    private static bool EstaNoPeriodo(DateTime? data, DateTime? dataInicial, DateTime? dataFinal)
    {
        return data is not null && EstaNoPeriodo(data.Value, dataInicial, dataFinal);
    }

    private static bool PagamentoContaComoRecebido(Pagamento pagamento)
    {
        return pagamento.Situacao is SituacaoPagamento.PAGO or SituacaoPagamento.PARCIAL;
    }

    private void DesenharGraficoLinhaRecebido(IReadOnlyCollection<Pagamento> pagamentos)
    {
        DashboardRecebidoLineCanvas.Children.Clear();

        var largura = Math.Max(DashboardRecebidoLineCanvas.ActualWidth, 360);
        var altura = Math.Max(DashboardRecebidoLineCanvas.ActualHeight, 220);
        const double margemEsquerda = 74;
        const double margemDireita = 24;
        const double margemSuperior = 20;
        const double margemInferior = 44;
        var areaLargura = largura - margemEsquerda - margemDireita;
        var areaAltura = altura - margemSuperior - margemInferior;
        var baseY = margemSuperior + areaAltura;

        DesenharEixosLinha(DashboardRecebidoLineCanvas, margemEsquerda, margemSuperior, baseY, areaLargura, areaAltura);

        var pontos = pagamentos
            .GroupBy(pagamento => pagamento.DataPagamento.Date)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new
            {
                Data = grupo.Key,
                Valor = grupo.Sum(pagamento => pagamento.ValorTotal)
            })
            .ToList();

        if (pontos.Count == 0)
        {
            AdicionarTextoCanvas(
                DashboardRecebidoLineCanvas,
                "Sem pagamentos no periodo",
                largura / 2 - 82,
                altura / 2 - 12,
                14,
                DashboardTextBrush);
            return;
        }

        decimal acumulado = 0M;
        var pontosAcumulados = pontos
            .Select(ponto =>
            {
                acumulado += ponto.Valor;
                return new
                {
                    ponto.Data,
                    ValorAcumulado = acumulado
                };
            })
            .ToList();
        var dataMinima = pontosAcumulados.First().Data;
        var dataMaxima = pontosAcumulados.Last().Data;
        var valorMaximo = pontosAcumulados.Max(ponto => ponto.ValorAcumulado);
        if (valorMaximo <= 0)
        {
            AdicionarTextoCanvas(
                DashboardRecebidoLineCanvas,
                "Sem pagamentos no periodo",
                largura / 2 - 82,
                altura / 2 - 12,
                14,
                DashboardTextBrush);
            return;
        }

        var intervaloDias = Math.Max(1, (dataMaxima - dataMinima).Days);

        for (var i = 0; i <= 4; i++)
        {
            var percentual = i / 4D;
            var y = baseY - percentual * areaAltura;
            var valor = valorMaximo * i / 4M;

            AdicionarTextoCanvas(
                DashboardRecebidoLineCanvas,
                valor.ToString("C0", RealCulture),
                4,
                y - 9,
                11,
                DashboardTextBrush);
        }

        var polyline = new Polyline
        {
            Stroke = DashboardLineBrush,
            StrokeThickness = 3
        };
        var pontosRenderizados = new List<(Point Point, decimal Valor)>();

        foreach (var ponto in pontosAcumulados)
        {
            var x = dataMinima == dataMaxima
                ? margemEsquerda + areaLargura / 2
                : margemEsquerda + (ponto.Data - dataMinima).Days / (double)intervaloDias * areaLargura;
            var y = baseY - decimal.ToDouble(ponto.ValorAcumulado / valorMaximo) * areaAltura;
            var point = new Point(x, y);

            polyline.Points.Add(point);
            pontosRenderizados.Add((point, ponto.ValorAcumulado));
        }

        DashboardRecebidoLineCanvas.Children.Add(polyline);

        foreach (var (point, valorAcumulado) in pontosRenderizados)
        {
            var pontoMarcador = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = DashboardLineBrush,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };

            Canvas.SetLeft(pontoMarcador, point.X - 4);
            Canvas.SetTop(pontoMarcador, point.Y - 4);
            DashboardRecebidoLineCanvas.Children.Add(pontoMarcador);

            AdicionarTextoCanvas(
                DashboardRecebidoLineCanvas,
                valorAcumulado.ToString("C0", RealCulture),
                Math.Min(point.X + 8, largura - 82),
                Math.Max(2, point.Y - 18),
                11,
                DashboardTextBrush);
        }

        AdicionarTextoCanvas(
            DashboardRecebidoLineCanvas,
            dataMinima.ToString("dd/MM/yyyy", RealCulture),
            margemEsquerda,
            baseY + 12,
            11,
            DashboardTextBrush);
        AdicionarTextoCanvas(
            DashboardRecebidoLineCanvas,
            dataMaxima.ToString("dd/MM/yyyy", RealCulture),
            Math.Max(margemEsquerda, margemEsquerda + areaLargura - 72),
            baseY + 12,
            11,
            DashboardTextBrush);
    }

    private static void DesenharEixosLinha(Canvas canvas, double margemEsquerda, double margemSuperior, double baseY, double areaLargura, double areaAltura)
    {
        canvas.Children.Add(new Line
        {
            X1 = margemEsquerda,
            X2 = margemEsquerda,
            Y1 = margemSuperior,
            Y2 = baseY,
            Stroke = DashboardGridBrush,
            StrokeThickness = 1.2
        });
        canvas.Children.Add(new Line
        {
            X1 = margemEsquerda,
            X2 = margemEsquerda + areaLargura,
            Y1 = baseY,
            Y2 = baseY,
            Stroke = DashboardGridBrush,
            StrokeThickness = 1.2
        });
    }

    private static IReadOnlyList<DashboardPrazoSlice> CriarFatiasPrazo(IEnumerable<string> situacoesPrazo)
    {
        return situacoesPrazo
            .GroupBy(situacao => situacao)
            .Select(grupo => new
            {
                Nome = grupo.Key,
                Quantidade = grupo.Count()
            })
            .OrderBy(item => ObterOrdemPrazoDashboard(item.Nome))
            .ThenBy(item => item.Nome)
            .Select(item => new DashboardPrazoSlice(
                item.Nome,
                item.Quantidade,
                ObterCorPrazoDashboard(item.Nome)))
            .ToList();
    }

    private static int ObterOrdemPrazoDashboard(string situacaoPrazo)
    {
        return situacaoPrazo switch
        {
            "Vence hoje" => 0,
            "Vence em 7 dias" => 1,
            "No prazo" => 2,
            _ => 3
        };
    }

    private static Brush ObterCorPrazoDashboard(string situacaoPrazo)
    {
        return situacaoPrazo switch
        {
            "Vence hoje" => DashboardVenceHojeBrush,
            "Vence em 7 dias" => DashboardVenceSeteBrush,
            "No prazo" => DashboardNoPrazoBrush,
            _ => DashboardFallbackBrush
        };
    }

    private static void DesenharGraficoPizza(Canvas canvas, Panel legenda, IReadOnlyList<DashboardPrazoSlice> fatias)
    {
        canvas.Children.Clear();
        legenda.Children.Clear();

        var largura = Math.Max(canvas.ActualWidth, canvas.Width);
        var altura = Math.Max(canvas.ActualHeight, canvas.Height);
        var diametro = Math.Max(120, Math.Min(largura, altura) - 12);
        var raio = diametro / 2;
        var centroX = largura / 2;
        var centroY = altura / 2;
        var total = fatias.Sum(fatia => fatia.Quantidade);

        if (total == 0)
        {
            AdicionarTextoCanvas(canvas, "Sem dados", centroX - 34, centroY - 10, 14, DashboardTextBrush);
            return;
        }

        if (fatias.Count == 1)
        {
            var fatiaUnica = fatias[0];
            var ellipse = new Ellipse
            {
                Width = diametro,
                Height = diametro,
                Fill = fatiaUnica.Cor,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(ellipse, centroX - raio);
            Canvas.SetTop(ellipse, centroY - raio);
            canvas.Children.Add(ellipse);
        }
        else
        {
            var anguloInicial = -90D;

            foreach (var fatia in fatias)
            {
                var anguloVarredura = fatia.Quantidade / (double)total * 360D;
                canvas.Children.Add(CriarFatiaPizza(centroX, centroY, raio, anguloInicial, anguloVarredura, fatia.Cor));
                anguloInicial += anguloVarredura;
            }
        }

        foreach (var fatia in fatias)
        {
            var linhaLegenda = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 4)
            };
            linhaLegenda.Children.Add(new Border
            {
                Width = 13,
                Height = 13,
                Background = fatia.Cor,
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 2, 8, 0)
            });
            linhaLegenda.Children.Add(new TextBlock
            {
                Text = $"{fatia.Nome}: {fatia.Quantidade:N0}",
                Foreground = DashboardTextBrush,
                FontSize = 13
            });
            legenda.Children.Add(linhaLegenda);
        }
    }

    private static Path CriarFatiaPizza(double centroX, double centroY, double raio, double anguloInicial, double anguloVarredura, Brush cor)
    {
        var pontoInicial = ObterPontoNoCirculo(centroX, centroY, raio, anguloInicial);
        var pontoFinal = ObterPontoNoCirculo(centroX, centroY, raio, anguloInicial + anguloVarredura);
        var figura = new PathFigure
        {
            StartPoint = new Point(centroX, centroY),
            IsClosed = true
        };
        figura.Segments.Add(new LineSegment(pontoInicial, true));
        figura.Segments.Add(new ArcSegment(
            pontoFinal,
            new Size(raio, raio),
            0,
            anguloVarredura > 180,
            SweepDirection.Clockwise,
            true));

        var geometria = new PathGeometry();
        geometria.Figures.Add(figura);

        return new Path
        {
            Data = geometria,
            Fill = cor,
            Stroke = Brushes.White,
            StrokeThickness = 1
        };
    }

    private static Point ObterPontoNoCirculo(double centroX, double centroY, double raio, double anguloGraus)
    {
        var anguloRadianos = Math.PI * anguloGraus / 180D;

        return new Point(
            centroX + raio * Math.Cos(anguloRadianos),
            centroY + raio * Math.Sin(anguloRadianos));
    }

    private static void AdicionarTextoCanvas(Canvas canvas, string texto, double esquerda, double topo, double tamanhoFonte, Brush cor)
    {
        var textBlock = new TextBlock
        {
            Text = texto,
            Foreground = cor,
            FontSize = tamanhoFonte
        };

        Canvas.SetLeft(textBlock, esquerda);
        Canvas.SetTop(textBlock, topo);
        canvas.Children.Add(textBlock);
    }

    private sealed record DashboardPrazoSlice(string Nome, int Quantidade, Brush Cor);

    private void AtualizarPastaSelecionada()
    {
        var pasta = _dataFolderProvider.DataFolderPath;
        SidebarFolderTextBlock.Text = pasta ?? "Nao selecionada";
        DataFolderTextBlock.Text = pasta is null
            ? "Escolha uma pasta para carregar e salvar os dados."
            : $"Dados em: {pasta}";
    }

    private void MostrarTela(UIElement tela, string titulo)
    {
        DashboardView.Visibility = Visibility.Collapsed;
        ClientesView.Visibility = Visibility.Collapsed;
        ProjetosView.Visibility = Visibility.Collapsed;
        TarefasView.Visibility = Visibility.Collapsed;
        PagamentosView.Visibility = Visibility.Collapsed;

        tela.Visibility = Visibility.Visible;
        PageTitleTextBlock.Text = titulo;
        AtualizarMenuAtivo(tela);
        AtualizarVisualizacoes();
        SetStatus(string.Empty);
    }

    private void AtualizarMenuAtivo(UIElement tela)
    {
        DashboardNavButton.Tag = tela == DashboardView ? "Ativo" : null;
        ClientesNavButton.Tag = tela == ClientesView ? "Ativo" : null;
        ProjetosNavButton.Tag = tela == ProjetosView ? "Ativo" : null;
        TarefasNavButton.Tag = tela == TarefasView ? "Ativo" : null;
        PagamentosNavButton.Tag = tela == PagamentosView ? "Ativo" : null;
    }

    private void AtualizarVisualizacoes()
    {
        AtualizarDisplaysDeAssociacoes(_clientes, _projetos);
        AtualizarDisplaysDeTarefas(_tarefas, _clientes, _projetos);
        AtualizarDisplaysDePagamentos(_pagamentos, _clientes, _projetos);
        _clientesView?.Refresh();
        _projetosView?.Refresh();
        _tarefasView?.Refresh();
        _pagamentosView?.Refresh();
        ClientesDataGrid.Items.Refresh();
        ProjetosDataGrid.Items.Refresh();
        TarefasDataGrid.Items.Refresh();
        PagamentosDataGrid.Items.Refresh();
        AtualizarOpcoesClienteDaTarefa();
        AtualizarOpcoesPagamentoPorAssociacao();
        AtualizarDashboard();
    }

    private bool TemPastaSelecionada()
    {
        if (_dataFolderProvider.DataFolderPath is not null)
        {
            return true;
        }

        SetStatus("Escolha uma pasta de dados antes de continuar.");
        return false;
    }

    private static void TrocarItens<T>(ObservableCollection<T> collection, IEnumerable<T> itens)
    {
        collection.Clear();

        foreach (var item in itens)
        {
            collection.Add(item);
        }
    }

    private bool FiltrarCliente(object item)
    {
        if (item is not Cliente cliente)
        {
            return false;
        }

        var situacaoFiltro = ObterValorFiltroEnum<SituacaoCliente>(ClienteFiltroSituacaoComboBox);

        return Contem(cliente.Nome, ClienteFiltroNomeTextBox.Text)
            && (situacaoFiltro is null || cliente.Situacao == situacaoFiltro)
            && (Contem(cliente.DocumentoPrincipalFormatado, ClienteFiltroDocumentoTextBox.Text)
                || Contem(cliente.DocumentoPrincipal?.Numero ?? string.Empty, ClienteFiltroDocumentoTextBox.Text))
            && Contem(cliente.EmailPrincipalEndereco, ClienteFiltroEmailTextBox.Text)
            && (Contem(cliente.TelefonePrincipalFormatado, ClienteFiltroTelefoneTextBox.Text)
                || Contem(cliente.TelefonePrincipal?.Numero ?? string.Empty, ClienteFiltroTelefoneTextBox.Text));
    }

    private bool FiltrarProjeto(object item)
    {
        if (item is not Projeto projeto)
        {
            return false;
        }

        var tipoFiltro = ObterValorFiltroEnum<TipoProjetoAmbiental>(ProjetoFiltroTipoComboBox);
        var situacaoFiltro = ObterValorFiltroEnum<SituacaoProjeto>(ProjetoFiltroSituacaoComboBox);
        var clienteNome = ObterNomesClientesDoProjeto(projeto);
        var endereco = projeto.Endereco.Formatar();

        return Contem(projeto.Nome, ProjetoFiltroNomeTextBox.Text)
            && (tipoFiltro is null || projeto.TipoAmbiental == tipoFiltro)
            && (situacaoFiltro is null || projeto.Situacao == situacaoFiltro)
            && Contem(clienteNome, ProjetoFiltroClienteTextBox.Text)
            && Contem(endereco, ProjetoFiltroEnderecoTextBox.Text);
    }

    private bool FiltrarTarefa(object item)
    {
        if (item is not Tarefa tarefa)
        {
            return false;
        }

        var situacaoFiltro = ObterValorFiltroEnum<SituacaoTarefa>(TarefaFiltroSituacaoComboBox);

        return Contem(tarefa.Descricao, TarefaFiltroDescricaoTextBox.Text)
            && Contem(tarefa.ProjetoDisplay, TarefaFiltroProjetoTextBox.Text)
            && Contem(tarefa.ClienteDisplay, TarefaFiltroClienteTextBox.Text)
            && (situacaoFiltro is null || tarefa.Situacao == situacaoFiltro)
            && Contem(FormatarData(tarefa.DataInicio), TarefaFiltroInicioTextBox.Text)
            && Contem(FormatarData(tarefa.DataPrevisao), TarefaFiltroPrevisaoTextBox.Text)
            && Contem(FormatarData(tarefa.DataFinal), TarefaFiltroFinalTextBox.Text);
    }

    private bool FiltrarPagamento(object item)
    {
        if (item is not Pagamento pagamento)
        {
            return false;
        }

        var formaFiltro = ObterValorFiltroEnum<FormaPagamento>(PagamentoFiltroFormaComboBox);
        var situacaoFiltro = ObterValorFiltroEnum<SituacaoPagamento>(PagamentoFiltroSituacaoComboBox);
        var valor = pagamento.ValorTotal.ToString("C", RealCulture);
        var dataPagamento = pagamento.DataPagamento.ToString("dd/MM/yyyy", RealCulture);

        return Contem(valor, PagamentoFiltroValorTextBox.Text)
            && (formaFiltro is null || pagamento.FormaPagamento == formaFiltro)
            && (situacaoFiltro is null || pagamento.Situacao == situacaoFiltro)
            && Contem(dataPagamento, PagamentoFiltroDataTextBox.Text)
            && Contem(pagamento.ProjetosAssociadosDisplay, PagamentoFiltroProjetoTextBox.Text)
            && Contem(pagamento.ClientesAssociadosDisplay, PagamentoFiltroClienteTextBox.Text)
            && Contem(pagamento.Observacao, PagamentoFiltroObservacaoTextBox.Text);
    }

    private void AplicarMascaraDocumento()
    {
        if (_aplicandoMascaraDocumento || ClienteDocumentoTextBox is null)
        {
            return;
        }

        var tipo = ObterValorEnum(ClienteDocumentoTipoComboBox, TipoDocumento.OUTRO);
        ClienteDocumentoTextBox.IsEnabled = tipo != TipoDocumento.SEM_DOCUMENTO;

        if (tipo == TipoDocumento.SEM_DOCUMENTO)
        {
            _aplicandoMascaraDocumento = true;
            ClienteDocumentoTextBox.Text = string.Empty;
            _aplicandoMascaraDocumento = false;
            return;
        }

        var textoOriginal = ClienteDocumentoTextBox.Text;
        var textoFormatado = FormatarDocumentoParaEntrada(textoOriginal, tipo);

        if (textoOriginal == textoFormatado)
        {
            return;
        }

        _aplicandoMascaraDocumento = true;
        ClienteDocumentoTextBox.Text = textoFormatado;
        ClienteDocumentoTextBox.CaretIndex = ClienteDocumentoTextBox.Text.Length;
        _aplicandoMascaraDocumento = false;
    }

    private static string FormatarDocumentoParaEntrada(string texto, TipoDocumento tipo)
    {
        if (tipo == TipoDocumento.OUTRO)
        {
            return texto;
        }

        if (tipo == TipoDocumento.PASSAPORTE)
        {
            var alfanumerico = new string(texto.Where(char.IsLetterOrDigit).Take(8).ToArray()).ToUpperInvariant();
            return alfanumerico.Length <= 2
                ? alfanumerico
                : $"{alfanumerico[..2]} {alfanumerico[2..]}";
        }

        var digitos = ApenasDigitos(texto);

        return tipo switch
        {
            TipoDocumento.CPF => FormatarCpfParcial(digitos),
            TipoDocumento.CNPJ => FormatarCnpjParcial(digitos),
            TipoDocumento.RG => FormatarRgParcial(digitos),
            _ => texto
        };
    }

    private static string NormalizarDocumentoParaSalvar(string texto, TipoDocumento tipo)
    {
        return tipo switch
        {
            TipoDocumento.CPF or TipoDocumento.CNPJ or TipoDocumento.RG => ApenasDigitos(texto),
            TipoDocumento.PASSAPORTE => new string(texto.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant(),
            TipoDocumento.SEM_DOCUMENTO => string.Empty,
            _ => texto.Trim()
        };
    }

    private static string ObterMotivoDocumentoInvalido(Documento documento)
    {
        return documento.Tipo switch
        {
            TipoDocumento.CPF => "CPF invalido. Informe 11 digitos com verificadores validos.",
            TipoDocumento.CNPJ => "CNPJ invalido. Informe 14 digitos com verificadores validos.",
            TipoDocumento.RG => "RG invalido. Informe pelo menos 5 caracteres.",
            TipoDocumento.PASSAPORTE => "Passaporte invalido. Informe de 5 a 20 letras ou numeros.",
            TipoDocumento.SEM_DOCUMENTO => "Opcao sem documento nao exige numero.",
            _ => "Documento invalido. Informe um documento principal."
        };
    }

    private static string FormatarCpfParcial(string digitos)
    {
        digitos = Limitar(digitos, 11);

        return digitos.Length switch
        {
            <= 3 => digitos,
            <= 6 => $"{digitos[..3]}.{digitos[3..]}",
            <= 9 => $"{digitos[..3]}.{digitos.Substring(3, 3)}.{digitos[6..]}",
            _ => $"{digitos[..3]}.{digitos.Substring(3, 3)}.{digitos.Substring(6, 3)}-{digitos[9..]}"
        };
    }

    private static string FormatarCnpjParcial(string digitos)
    {
        digitos = Limitar(digitos, 14);

        return digitos.Length switch
        {
            <= 2 => digitos,
            <= 5 => $"{digitos[..2]}.{digitos[2..]}",
            <= 8 => $"{digitos[..2]}.{digitos.Substring(2, 3)}.{digitos[5..]}",
            <= 12 => $"{digitos[..2]}.{digitos.Substring(2, 3)}.{digitos.Substring(5, 3)}/{digitos[8..]}",
            _ => $"{digitos[..2]}.{digitos.Substring(2, 3)}.{digitos.Substring(5, 3)}/{digitos.Substring(8, 4)}-{digitos[12..]}"
        };
    }

    private static string FormatarRgParcial(string digitos)
    {
        digitos = Limitar(digitos, 9);

        return digitos.Length switch
        {
            <= 2 => digitos,
            <= 5 => $"{digitos[..2]}.{digitos[2..]}",
            <= 8 => $"{digitos[..2]}.{digitos.Substring(2, 3)}.{digitos[5..]}",
            _ => $"{digitos[..2]}.{digitos.Substring(2, 3)}.{digitos.Substring(5, 3)}-{digitos[8..]}"
        };
    }

    private static string ApenasDigitos(string texto)
    {
        return new string(texto.Where(char.IsDigit).ToArray());
    }

    private static string Limitar(string texto, int maximo)
    {
        return texto.Length <= maximo ? texto : texto[..maximo];
    }

    private static bool Contem(string valor, string filtro)
    {
        return string.IsNullOrWhiteSpace(filtro)
            || valor.Contains(filtro.Trim(), StringComparison.CurrentCultureIgnoreCase);
    }

    private static string FormatarData(DateTime data)
    {
        return data.ToString("dd/MM/yyyy", RealCulture);
    }

    private static string FormatarData(DateTime? data)
    {
        return data is null ? string.Empty : FormatarData(data.Value);
    }

    private void ExportarTabela<T>(
        string nomeArquivoBase,
        string nomeAba,
        IReadOnlyList<T> linhas,
        IReadOnlyList<XlsxColumn<T>> colunas)
    {
        var dialog = new SaveFileDialog
        {
            Title = $"Exportar {nomeAba}",
            Filter = "Planilha Excel (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"{nomeArquivoBase}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            XlsxExporter.Export(dialog.FileName, nomeAba, colunas, linhas, RealCulture);
            var mensagem = $"{linhas.Count:N0} registros exportados para XLSX.";
            SetStatus(mensagem);
            MessageBox.Show(mensagem, "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MostrarErro("Nao foi possivel exportar a tabela.", ex);
        }
    }

    private static List<T> ObterItensVisiveis<T>(ICollectionView? view, IEnumerable<T> fallback)
    {
        return view is null
            ? fallback.ToList()
            : view.Cast<T>().ToList();
    }

    private string ObterNomesClientesDoProjeto(Projeto projeto)
    {
        if (!string.IsNullOrWhiteSpace(projeto.ClientesAssociadosDisplay))
        {
            return projeto.ClientesAssociadosDisplay;
        }

        var clienteIds = projeto.Clientes.Select(vinculo => vinculo.ClienteId).ToHashSet();
        return string.Join(", ", _clientes
            .Where(cliente => clienteIds.Contains(cliente.Id))
            .Select(cliente => cliente.Nome));
    }

    private void AplicarMascaraMoedaProjeto()
    {
        if (_aplicandoMascaraValorProjeto || ProjetoValorTextBox is null)
        {
            return;
        }

        var digitos = ApenasDigitos(ProjetoValorTextBox.Text);
        var valor = string.IsNullOrWhiteSpace(digitos)
            ? 0M
            : decimal.Parse(digitos, CultureInfo.InvariantCulture) / 100M;
        var textoFormatado = valor.ToString("C", RealCulture);

        if (ProjetoValorTextBox.Text == textoFormatado)
        {
            return;
        }

        _aplicandoMascaraValorProjeto = true;
        ProjetoValorTextBox.Text = textoFormatado;
        ProjetoValorTextBox.CaretIndex = ProjetoValorTextBox.Text.Length;
        _aplicandoMascaraValorProjeto = false;
    }

    private void AplicarMascaraMoedaPagamento()
    {
        if (_aplicandoMascaraValorPagamento || PagamentoValorTextBox is null)
        {
            return;
        }

        var digitos = ApenasDigitos(PagamentoValorTextBox.Text);
        var valor = string.IsNullOrWhiteSpace(digitos)
            ? 0M
            : decimal.Parse(digitos, CultureInfo.InvariantCulture) / 100M;
        var textoFormatado = valor.ToString("C", RealCulture);

        if (PagamentoValorTextBox.Text == textoFormatado)
        {
            return;
        }

        _aplicandoMascaraValorPagamento = true;
        PagamentoValorTextBox.Text = textoFormatado;
        PagamentoValorTextBox.CaretIndex = PagamentoValorTextBox.Text.Length;
        _aplicandoMascaraValorPagamento = false;
    }

    private void AplicarMascaraCepProjeto()
    {
        if (_aplicandoMascaraCepProjeto || ProjetoCepTextBox is null)
        {
            return;
        }

        var textoOriginal = ProjetoCepTextBox.Text;
        var textoFormatado = FormatarCep(textoOriginal);

        if (textoOriginal == textoFormatado)
        {
            return;
        }

        _aplicandoMascaraCepProjeto = true;
        ProjetoCepTextBox.Text = textoFormatado;
        ProjetoCepTextBox.CaretIndex = ProjetoCepTextBox.Text.Length;
        _aplicandoMascaraCepProjeto = false;
    }

    private static string FormatarCep(string texto)
    {
        var digitos = Limitar(ApenasDigitos(texto), 8);

        return digitos.Length <= 5
            ? digitos
            : $"{digitos[..5]}-{digitos[5..]}";
    }

    private void DefinirValorProjeto(decimal valor)
    {
        _aplicandoMascaraValorProjeto = true;
        ProjetoValorTextBox.Text = valor.ToString("C", RealCulture);
        ProjetoValorTextBox.CaretIndex = ProjetoValorTextBox.Text.Length;
        _aplicandoMascaraValorProjeto = false;
    }

    private void DefinirValorPagamento(decimal valor)
    {
        _aplicandoMascaraValorPagamento = true;
        PagamentoValorTextBox.Text = valor.ToString("C", RealCulture);
        PagamentoValorTextBox.CaretIndex = PagamentoValorTextBox.Text.Length;
        _aplicandoMascaraValorPagamento = false;
    }

    private static TEnum ObterValorEnum<TEnum>(ComboBox comboBox, TEnum valorPadrao) where TEnum : struct, Enum
    {
        return comboBox.SelectedValue is TEnum valor
            ? valor
            : comboBox.SelectedItem is EnumOption<TEnum> option
                ? option.Value
                : valorPadrao;
    }

    private static TEnum? ObterValorFiltroEnum<TEnum>(ComboBox comboBox) where TEnum : struct, Enum
    {
        return comboBox.SelectedItem is EnumFilterOption<TEnum> option
            ? option.Value
            : null;
    }

    private List<int> ObterProjetosSelecionadosNoCliente()
    {
        return ClienteProjetosListBox.SelectedItems
            .Cast<Projeto>()
            .Select(projeto => projeto.Id)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    private List<int> ObterClientesSelecionadosNoProjeto()
    {
        return ProjetoClientesListBox.SelectedItems
            .Cast<Cliente>()
            .Select(cliente => cliente.Id)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    private void SelecionarProjetosDoCliente(Cliente cliente)
    {
        var projetoIds = cliente.Projetos.Select(vinculo => vinculo.ProjetoId).ToHashSet();
        ClienteProjetosListBox.UnselectAll();

        foreach (var projeto in _projetos.Where(projeto => projetoIds.Contains(projeto.Id)))
        {
            ClienteProjetosListBox.SelectedItems.Add(projeto);
        }
    }

    private void SelecionarClientesDoProjeto(Projeto projeto)
    {
        var clienteIds = projeto.Clientes.Select(vinculo => vinculo.ClienteId).ToHashSet();
        ProjetoClientesListBox.UnselectAll();

        foreach (var cliente in _clientes.Where(cliente => clienteIds.Contains(cliente.Id)))
        {
            ProjetoClientesListBox.SelectedItems.Add(cliente);
        }
    }

    private async Task AtualizarProjetosDoClienteAsync(int clienteId, IReadOnlyCollection<int> projetosSelecionadosIds)
    {
        var projetos = (await _dataStore.Projetos.ListarAsync()).ToList();
        var selecionados = projetosSelecionadosIds.ToHashSet();
        var motivoBloqueio = await ObterMotivoRemocaoProjetosDoClienteBloqueadaAsync(
            clienteId,
            projetos
                .Where(projeto => projeto.Clientes.Any(vinculo => vinculo.ClienteId == clienteId))
                .Select(projeto => projeto.Id),
            projetosSelecionadosIds);

        if (motivoBloqueio is not null)
        {
            throw new InvalidOperationException(motivoBloqueio);
        }

        foreach (var projeto in projetos)
        {
            var existia = projeto.Clientes.Any(vinculo => vinculo.ClienteId == clienteId);
            var deveExistir = selecionados.Contains(projeto.Id);

            if (deveExistir && !existia)
            {
                projeto.Clientes.Add(new ProjetoCliente
                {
                    ProjetoId = projeto.Id,
                    ClienteId = clienteId,
                    Papel = "Cliente associado",
                    DataVinculo = DateTime.Today
                });
            }

            if (!deveExistir && existia)
            {
                projeto.Clientes.RemoveAll(vinculo => vinculo.ClienteId == clienteId);
            }

            if (existia != deveExistir)
            {
                NormalizarVinculosProjetoClientes(projeto.Clientes, projeto.Id);
                await _dataStore.Projetos.SalvarAsync(projeto);
            }
        }
    }

    private static List<ProjetoCliente> CriarVinculosProjetoClientes(
        int projetoId,
        IReadOnlyCollection<int> clientesSelecionadosIds,
        IReadOnlyDictionary<int, ProjetoCliente> vinculosAnteriores)
    {
        var vinculos = clientesSelecionadosIds
            .Where(id => id > 0)
            .Distinct()
            .Select(clienteId =>
            {
                if (vinculosAnteriores.TryGetValue(clienteId, out var anterior))
                {
                    return anterior;
                }

                return new ProjetoCliente
                {
                    ClienteId = clienteId,
                    DataVinculo = DateTime.Today
                };
            })
            .ToList();

        NormalizarVinculosProjetoClientes(vinculos, projetoId);
        return vinculos;
    }

    private static void NormalizarVinculosProjetoClientes(List<ProjetoCliente> vinculos, int projetoId)
    {
        if (vinculos.Count == 0)
        {
            return;
        }

        var percentual = Math.Round(100M / vinculos.Count, 2);

        for (var i = 0; i < vinculos.Count; i++)
        {
            var vinculo = vinculos[i];
            vinculo.ProjetoId = projetoId;
            vinculo.Principal = i == 0;
            vinculo.Papel = vinculo.Principal ? "Cliente principal" : "Cliente associado";
            vinculo.PercentualResponsabilidade = percentual;
        }
    }

    private void AtualizarOpcoesSituacaoProjetoPorDataFinal(
        bool definirConcluido = false,
        bool definirEmAndamentoSeConcluidoSemDataFinal = false)
    {
        var possuiDataFinal = ProjetoDataFinalPicker.SelectedDate is not null;
        var situacaoAtual = ObterValorEnum(ProjetoSituacaoComboBox, SituacaoProjeto.PLANEJADO);
        var opcoes = EnumDisplay.GetOptions<SituacaoProjeto>();

        if (possuiDataFinal)
        {
            opcoes = opcoes
                .Where(opcao => opcao.Value is SituacaoProjeto.CONCLUIDO or SituacaoProjeto.CANCELADO)
                .ToArray();

            if (definirConcluido || situacaoAtual is not (SituacaoProjeto.CONCLUIDO or SituacaoProjeto.CANCELADO))
            {
                situacaoAtual = SituacaoProjeto.CONCLUIDO;
            }
        }
        else if (definirEmAndamentoSeConcluidoSemDataFinal && situacaoAtual == SituacaoProjeto.CONCLUIDO)
        {
            situacaoAtual = SituacaoProjeto.EM_ANDAMENTO;
        }

        ProjetoSituacaoComboBox.ItemsSource = opcoes;
        ProjetoSituacaoComboBox.SelectedValue = situacaoAtual;
    }

    private void AtualizarOpcoesSituacaoTarefaPorDataFinal(
        bool definirConcluido = false,
        bool definirEmAndamentoSeConcluidoSemDataFinal = false)
    {
        var possuiDataFinal = TarefaDataFinalPicker.SelectedDate is not null;
        var situacaoAtual = ObterValorEnum(TarefaSituacaoComboBox, SituacaoTarefa.PLANEJADO);
        var opcoes = EnumDisplay.GetOptions<SituacaoTarefa>();

        if (possuiDataFinal)
        {
            opcoes = opcoes
                .Where(opcao => opcao.Value is SituacaoTarefa.CONCLUIDO or SituacaoTarefa.CANCELADO)
                .ToArray();

            if (definirConcluido || situacaoAtual is not (SituacaoTarefa.CONCLUIDO or SituacaoTarefa.CANCELADO))
            {
                situacaoAtual = SituacaoTarefa.CONCLUIDO;
            }
        }
        else if (definirEmAndamentoSeConcluidoSemDataFinal && situacaoAtual == SituacaoTarefa.CONCLUIDO)
        {
            situacaoAtual = SituacaoTarefa.EM_ANDAMENTO;
        }

        TarefaSituacaoComboBox.ItemsSource = opcoes;
        TarefaSituacaoComboBox.SelectedValue = situacaoAtual;
    }

    private void AtualizarOpcoesClienteDaTarefa()
    {
        if (_atualizandoOpcoesTarefa)
        {
            return;
        }

        _atualizandoOpcoesTarefa = true;

        try
        {
            var clienteSelecionado = TarefaClienteComboBox.SelectedItem as Cliente;
            var projetoSelecionado = TarefaProjetoComboBox.SelectedItem as Projeto;
            List<Cliente> clientesDisponiveis = projetoSelecionado is null
                ? []
                : _clientes
                    .Where(cliente => ProjetoTemCliente(projetoSelecionado, cliente.Id))
                    .OrderBy(cliente => cliente.Nome)
                    .ToList();
            var clientes = new List<Cliente> { SemClienteTarefa };
            clientes.AddRange(clientesDisponiveis);

            if (clienteSelecionado is not null
                && clienteSelecionado.Id > 0
                && clientes.All(cliente => cliente.Id != clienteSelecionado.Id))
            {
                clienteSelecionado = null;
            }

            TrocarItens(_tarefaClientesDisponiveis, clientes);

            TarefaClienteComboBox.SelectedItem = clienteSelecionado is null || clienteSelecionado.Id <= 0
                ? _tarefaClientesDisponiveis.FirstOrDefault(cliente => cliente.Id == 0)
                : _tarefaClientesDisponiveis.FirstOrDefault(cliente => cliente.Id == clienteSelecionado.Id);
        }
        finally
        {
            _atualizandoOpcoesTarefa = false;
        }
    }

    private void AtualizarOpcoesPagamentoPorAssociacao()
    {
        if (_atualizandoOpcoesPagamento)
        {
            return;
        }

        _atualizandoOpcoesPagamento = true;

        try
        {
            var projetoSelecionado = PagamentoProjetoComboBox.SelectedItem as Projeto;
            var clienteSelecionado = PagamentoClienteComboBox.SelectedItem as Cliente;

            IEnumerable<Projeto> projetosDisponiveis = _projetos;
            IEnumerable<Cliente> clientesDisponiveis = _clientes;

            if (clienteSelecionado is not null)
            {
                projetosDisponiveis = projetosDisponiveis
                    .Where(projeto => ProjetoTemCliente(projeto, clienteSelecionado.Id));
            }

            if (projetoSelecionado is not null)
            {
                clientesDisponiveis = clientesDisponiveis
                    .Where(cliente => ProjetoTemCliente(projetoSelecionado, cliente.Id));
            }

            var projetos = projetosDisponiveis.OrderBy(projeto => projeto.Nome).ToList();
            var clientes = clientesDisponiveis.OrderBy(cliente => cliente.Nome).ToList();

            if (projetoSelecionado is not null && projetos.All(projeto => projeto.Id != projetoSelecionado.Id))
            {
                projetoSelecionado = null;
            }

            if (clienteSelecionado is not null && clientes.All(cliente => cliente.Id != clienteSelecionado.Id))
            {
                clienteSelecionado = null;
            }

            TrocarItens(_pagamentoProjetosDisponiveis, projetos);
            TrocarItens(_pagamentoClientesDisponiveis, clientes);

            PagamentoProjetoComboBox.SelectedItem = projetoSelecionado is null
                ? null
                : _pagamentoProjetosDisponiveis.FirstOrDefault(projeto => projeto.Id == projetoSelecionado.Id);
            PagamentoClienteComboBox.SelectedItem = clienteSelecionado is null
                ? null
                : _pagamentoClientesDisponiveis.FirstOrDefault(cliente => cliente.Id == clienteSelecionado.Id);
        }
        finally
        {
            _atualizandoOpcoesPagamento = false;
        }
    }

    private static bool ProjetoTemCliente(Projeto projeto, int clienteId)
    {
        return projeto.Clientes.Any(vinculo => vinculo.ClienteId == clienteId);
    }

    private async Task<string?> ObterMotivoRemocaoClientesDoProjetoBloqueadaAsync(
        int projetoId,
        IEnumerable<int> clientesAtuaisIds,
        IReadOnlyCollection<int> clientesSelecionadosIds)
    {
        var selecionados = clientesSelecionadosIds.ToHashSet();
        var remocoes = clientesAtuaisIds
            .Where(clienteId => !selecionados.Contains(clienteId))
            .Select(clienteId => (ProjetoId: projetoId, ClienteId: clienteId));

        return await ObterMotivoRemocaoClienteProjetoBloqueadaAsync(remocoes);
    }

    private async Task<string?> ObterMotivoRemocaoProjetosDoClienteBloqueadaAsync(
        int clienteId,
        IEnumerable<int> projetosAtuaisIds,
        IReadOnlyCollection<int> projetosSelecionadosIds)
    {
        var selecionados = projetosSelecionadosIds.ToHashSet();
        var remocoes = projetosAtuaisIds
            .Where(projetoId => !selecionados.Contains(projetoId))
            .Select(projetoId => (ProjetoId: projetoId, ClienteId: clienteId));

        return await ObterMotivoRemocaoClienteProjetoBloqueadaAsync(remocoes);
    }

    private async Task<string?> ObterMotivoRemocaoClienteProjetoBloqueadaAsync(
        IEnumerable<(int ProjetoId, int ClienteId)> remocoes)
    {
        var remocoesNormalizadas = remocoes
            .Where(remocao => remocao.ProjetoId > 0 && remocao.ClienteId > 0)
            .Distinct()
            .ToList();

        if (remocoesNormalizadas.Count == 0)
        {
            return null;
        }

        var pagamentos = (await _dataStore.Pagamentos.ListarAsync()).ToList();

        foreach (var remocao in remocoesNormalizadas)
        {
            var possuiPagamento = pagamentos.Any(pagamento =>
                pagamento.Projetos.Any(projeto => projeto.ProjetoId == remocao.ProjetoId)
                && pagamento.Clientes.Any(cliente => cliente.ClienteId == remocao.ClienteId));

            if (possuiPagamento)
            {
                return CriarMensagemRemocaoClienteProjetoBloqueada(remocao.ClienteId);
            }
        }

        return null;
    }

    private string CriarMensagemRemocaoClienteProjetoBloqueada(int clienteId)
    {
        var nomeCliente = _clientes.FirstOrDefault(cliente => cliente.Id == clienteId)?.Nome
            ?? $"#{clienteId}";

        return $"Esse cliente {nomeCliente} possui pagamentos feitos nesse projeto portanto nao e possivel realizar a remocao, mude o cliente relacionado ao pagamento ou o exclua antes de remover o cliente do projeto.";
    }

    private static void AtualizarDisplaysDeAssociacoes(
        IReadOnlyCollection<Cliente> clientes,
        IReadOnlyCollection<Projeto> projetos)
    {
        var nomesClientes = clientes.ToDictionary(cliente => cliente.Id, cliente => cliente.Nome);
        var nomesProjetos = projetos.ToDictionary(projeto => projeto.Id, projeto => projeto.Nome);

        foreach (var projeto in projetos)
        {
            projeto.ClientesAssociadosDisplay = string.Join(", ", projeto.Clientes
                .Select(vinculo => nomesClientes.TryGetValue(vinculo.ClienteId, out var nome) ? nome : null)
                .OfType<string>()
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .Distinct(StringComparer.CurrentCultureIgnoreCase));
        }

        foreach (var cliente in clientes)
        {
            cliente.ProjetosAssociadosDisplay = string.Join(", ", cliente.Projetos
                .Select(vinculo => nomesProjetos.TryGetValue(vinculo.ProjetoId, out var nome) ? nome : null)
                .OfType<string>()
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .Distinct(StringComparer.CurrentCultureIgnoreCase));
        }
    }

    private static void AtualizarDisplaysDeTarefas(
        IEnumerable<Tarefa> tarefas,
        IReadOnlyCollection<Cliente> clientes,
        IReadOnlyCollection<Projeto> projetos)
    {
        var nomesClientes = clientes.ToDictionary(cliente => cliente.Id, cliente => cliente.Nome);
        var nomesProjetos = projetos.ToDictionary(projeto => projeto.Id, projeto => projeto.Nome);

        foreach (var tarefa in tarefas)
        {
            tarefa.ProjetoDisplay = nomesProjetos.TryGetValue(tarefa.ProjetoId, out var nomeProjeto)
                ? nomeProjeto
                : string.Empty;
            tarefa.ClienteDisplay = tarefa.ClienteId is int clienteId
                && nomesClientes.TryGetValue(clienteId, out var nomeCliente)
                    ? nomeCliente
                    : "Sem cliente";
        }
    }

    private static void AtualizarDisplaysDePagamentos(
        IEnumerable<Pagamento> pagamentos,
        IReadOnlyCollection<Cliente> clientes,
        IReadOnlyCollection<Projeto> projetos)
    {
        var nomesClientes = clientes.ToDictionary(cliente => cliente.Id, cliente => cliente.Nome);
        var nomesProjetos = projetos.ToDictionary(projeto => projeto.Id, projeto => projeto.Nome);

        foreach (var pagamento in pagamentos)
        {
            pagamento.ProjetosAssociadosDisplay = string.Join(", ", pagamento.Projetos
                .Select(vinculo => nomesProjetos.TryGetValue(vinculo.ProjetoId, out var nome) ? nome : null)
                .OfType<string>()
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .Distinct(StringComparer.CurrentCultureIgnoreCase));

            pagamento.ClientesAssociadosDisplay = string.Join(", ", pagamento.Clientes
                .Select(vinculo => nomesClientes.TryGetValue(vinculo.ClienteId, out var nome) ? nome : null)
                .OfType<string>()
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .Distinct(StringComparer.CurrentCultureIgnoreCase));
        }
    }

    private static void SincronizarProjetosNosClientes(IReadOnlyCollection<Cliente> clientes, IEnumerable<Projeto> projetos)
    {
        foreach (var cliente in clientes)
        {
            cliente.Projetos.Clear();
        }

        foreach (var projeto in projetos)
        {
            foreach (var vinculo in projeto.Clientes)
            {
                var cliente = clientes.FirstOrDefault(item => item.Id == vinculo.ClienteId);

                if (cliente is not null)
                {
                    cliente.Projetos.Add(new ProjetoCliente
                    {
                        Id = vinculo.Id,
                        ProjetoId = projeto.Id,
                        ClienteId = cliente.Id,
                        Papel = vinculo.Papel,
                        PercentualResponsabilidade = vinculo.PercentualResponsabilidade,
                        DataVinculo = vinculo.DataVinculo,
                        Principal = vinculo.Principal
                    });
                }
            }
        }
    }

    private static void SincronizarPagamentosNosProjetos(IReadOnlyCollection<Projeto> projetos, IEnumerable<Pagamento> pagamentos)
    {
        foreach (var projeto in projetos)
        {
            projeto.Pagamentos.Clear();
        }

        foreach (var pagamento in pagamentos.Where(pagamento => pagamento.Situacao is SituacaoPagamento.PAGO or SituacaoPagamento.PARCIAL))
        {
            foreach (var associado in pagamento.Projetos)
            {
                var projeto = projetos.FirstOrDefault(item => item.Id == associado.ProjetoId);

                if (projeto is not null)
                {
                    projeto.Pagamentos.Add(associado);
                }
            }
        }
    }

    private static bool TentarLerDecimal(string texto, out decimal valor)
    {
        var entrada = texto.Trim();
        var estilos = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;

        return decimal.TryParse(entrada, estilos, CultureInfo.CurrentCulture, out valor)
            || decimal.TryParse(entrada, estilos, CultureInfo.GetCultureInfo("pt-BR"), out valor)
            || decimal.TryParse(entrada.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    private void SetStatus(string mensagem)
    {
        StatusMessageTextBlock.Text = mensagem;
    }

    private void MostrarErro(string mensagem, Exception ex)
    {
        SetStatus(mensagem);
        MessageBox.Show($"{mensagem}\n\n{ex.Message}", "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void MostrarErroCliente(string motivo)
    {
        var mensagem = $"Nao foi possivel salvar o cliente.\n\nMotivo: {motivo}";
        SetStatus(motivo);
        MessageBox.Show(mensagem, "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void MostrarErroProjeto(string motivo)
    {
        var mensagem = $"Nao foi possivel salvar o projeto.\n\nMotivo: {motivo}";
        SetStatus(motivo);
        MessageBox.Show(mensagem, "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void MostrarErroTarefa(string motivo)
    {
        var mensagem = $"Nao foi possivel salvar a tarefa.\n\nMotivo: {motivo}";
        SetStatus(motivo);
        MessageBox.Show(mensagem, "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void MostrarErroPagamento(string motivo)
    {
        var mensagem = $"Nao foi possivel salvar o pagamento.\n\nMotivo: {motivo}";
        SetStatus(motivo);
        MessageBox.Show(mensagem, "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void MostrarErroCep(string motivo)
    {
        var mensagem = $"Nao foi possivel consultar o CEP.\n\nMotivo: {motivo}";
        SetStatus(motivo);
        MessageBox.Show(mensagem, "Gestor Ambiental", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
