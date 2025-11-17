using Avalia_.Data.Models;
using Avalia_.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;   // para DisplayAlert

namespace Avalia_.ViewModels;

public partial class CadastroUnidadesViewModel : ObservableObject
{
    private readonly SupabaseService _supa;

    [ObservableProperty] private string nome = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private bool showStatus;

    public ObservableCollection<Unidade> Unidades { get; } = new();
    [ObservableProperty] private Unidade? unidadeSelecionada;

    public const string PrefUnidadeIdKey = "unidade_atual_id";
    public const string PrefUnidadeNomeKey = "unidade_atual_nome";

    // controle de edição
    private int? _idUnidadeEditando;

    public CadastroUnidadesViewModel(SupabaseService supa)
    {
        _supa = supa;
    }

    [RelayCommand]
    public async Task CarregarUnidadesAsync()
    {
        await RecarregarUnidadesAsync();
    }

    private async Task RecarregarUnidadesAsync()
    {
        try
        {
            IsBusy = true;
            await _supa.InitializeAsync();

            Unidades.Clear();
            var lista = await _supa.GetUnidadesAsync();
            foreach (var u in lista)
                Unidades.Add(u);

            // pré-seleciona a unidade já vinculada (se existir)
            var atualId = Preferences.Get(PrefUnidadeIdKey, 0);
            if (atualId > 0)
                UnidadeSelecionada = Unidades.FirstOrDefault(u => u.IdUnidade == atualId)
                                     ?? Unidades.FirstOrDefault();
            else
                UnidadeSelecionada = Unidades.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Status($"Erro ao carregar unidades: {ex.Message}", true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            Status("Informe o nome da unidade.", erro: true);
            return;
        }

        try
        {
            IsBusy = true;
            await _supa.InitializeAsync(); // idempotente

            if (_idUnidadeEditando is null)
            {
                // NOVA UNIDADE
                var criada = await _supa.AddUnidadeAsync(Nome);
                if (criada is null)
                {
                    Status("Não foi possível salvar. Tente novamente.", erro: true);
                    return;
                }

                Status($"Unidade \"{criada.Nome}\" salva com sucesso! (ID: {criada.IdUnidade})");
            }
            else
            {
                // EDIÇÃO
                var atualizada = await _supa.UpdateUnidadeAsync(_idUnidadeEditando.Value, Nome);
                if (atualizada is null)
                {
                    Status("Não foi possível atualizar a unidade.", erro: true);
                    return;
                }

                Status($"Unidade \"{atualizada.Nome}\" atualizada com sucesso!");
            }

            // recarrega a lista
            await RecarregarUnidadesAsync();

            // limpa campos / modo edição
            Nome = string.Empty;
            _idUnidadeEditando = null;
        }
        catch (Exception ex)
        {
            Status($"Erro ao salvar: {ex.Message}", erro: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public Task ConfirmarVinculoAsync()
    {
        if (UnidadeSelecionada is null)
        {
            Status("Selecione uma unidade para vincular.", true);
            return Task.CompletedTask;
        }

        Preferences.Set(PrefUnidadeIdKey, UnidadeSelecionada.IdUnidade);
        Preferences.Set(PrefUnidadeNomeKey, UnidadeSelecionada.Nome);
        Status($"Dispositivo vinculado à unidade \"{UnidadeSelecionada.Nome}\".", false);
        return Task.CompletedTask;
    }

    [RelayCommand]
    public void EditarUnidade(Unidade unidade)
    {
        if (unidade is null)
            return;

        _idUnidadeEditando = unidade.IdUnidade;
        Nome = unidade.Nome;

        // deixa a unidade selecionada igual à que está editando
        UnidadeSelecionada = unidade;

        Status($"Editando unidade \"{unidade.Nome}\".");
    }

    [RelayCommand]
    public async Task ExcluirUnidadeAsync(Unidade unidade)
    {
        if (unidade is null)
            return;

        var confirmar = await Application.Current.MainPage.DisplayAlert(
            "Excluir unidade",
            $"Deseja realmente excluir \"{unidade.Nome}\"?",
            "Sim", "Não");

        if (!confirmar)
            return;

        try
        {
            IsBusy = true;
            await _supa.InitializeAsync();

            var ok = await _supa.DeleteUnidadeAsync(unidade.IdUnidade);
            if (!ok)
            {
                Status("Não foi possível excluir a unidade.", erro: true);
                return;
            }

            Unidades.Remove(unidade);

            // se a unidade excluída era a vinculada, apaga o vínculo
            var atualId = Preferences.Get(PrefUnidadeIdKey, 0);
            if (atualId == unidade.IdUnidade)
            {
                Preferences.Remove(PrefUnidadeIdKey);
                Preferences.Remove(PrefUnidadeNomeKey);
                UnidadeSelecionada = Unidades.FirstOrDefault();
            }

            Status($"Unidade \"{unidade.Nome}\" excluída.");
        }
        catch (Exception ex)
        {
            Status($"Erro ao excluir: {ex.Message}", erro: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Status(string msg, bool erro = false)
    {
        StatusMessage = msg;
        ShowStatus = true;
        // se quiser, depois criamos uma propriedade de cor conforme erro
    }
}
