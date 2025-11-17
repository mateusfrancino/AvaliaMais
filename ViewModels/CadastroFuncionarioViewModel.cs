using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalia_.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Avalia_.Data.Models;

namespace Avalia_.ViewModels;

public partial class CadastroFuncionarioViewModel : ObservableObject
{
    private readonly SupabaseService _supa;

    [ObservableProperty] private string nome = string.Empty;
    [ObservableProperty] private Unidade? unidadeSelecionada;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? statusMessage;
    [ObservableProperty] private bool showStatus;
    [ObservableProperty] private ImageSource? fotoPreview;

    // lista de unidades
    public ObservableCollection<Unidade> Unidades { get; } = new();

    // lista de funcionários da unidade selecionada
    public ObservableCollection<Funcionario> Funcionarios { get; } = new();

    private byte[]? _fotoBytes;

    // controle de edição
    private int? _idFuncionarioEditando;
    private bool _suppressUnidadeChanged; // evitar recarga duplicada

    public CadastroFuncionarioViewModel(SupabaseService supa)
    {
        _supa = supa;
    }

    [RelayCommand]
    public async Task CarregarAsync()
    {
        try
        {
            IsBusy = true;
            await _supa.InitializeAsync();

            Unidades.Clear();
            var lista = await _supa.GetUnidadesAsync();
            foreach (var u in lista)
                Unidades.Add(u);

            _suppressUnidadeChanged = true;
            UnidadeSelecionada ??= Unidades.FirstOrDefault();
            _suppressUnidadeChanged = false;

            await RecarregarFuncionariosAsync();
        }
        catch (Exception ex)
        {
            Status($"Erro ao carregar unidades: {ex.Message}", erro: true);
        }
        finally { IsBusy = false; }
    }

    private async Task RecarregarFuncionariosAsync()
    {
        if (UnidadeSelecionada is null)
            return;

        try
        {
            IsBusy = true;
            await _supa.InitializeAsync();

            Funcionarios.Clear();
            var lista = await _supa.GetFuncionariosAsync(UnidadeSelecionada.IdUnidade);
            foreach (var f in lista)
                Funcionarios.Add(f);
        }
        catch (Exception ex)
        {
            Status($"Erro ao carregar funcionários: {ex.Message}", erro: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnUnidadeSelecionadaChanged(Unidade? value)
    {
        if (_suppressUnidadeChanged)
            return;

        // troca de unidade -> recarrega funcionários
        _ = RecarregarFuncionariosAsync();
    }

    [RelayCommand]
    public async Task PickFotoAsync()
    {
        try
        {
            var file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Selecione uma foto"
            });
            if (file == null) return;

            await using var src = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await src.CopyToAsync(ms);
            _fotoBytes = ms.ToArray();

            FotoPreview = ImageSource.FromStream(() => new MemoryStream(_fotoBytes));
        }
        catch (Exception ex)
        {
            Status($"Não foi possível selecionar a foto: {ex.Message}", erro: true);
        }
    }

    [RelayCommand]
    public async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            Status("Informe o nome do funcionário.", erro: true);
            return;
        }
        if (UnidadeSelecionada is null)
        {
            Status("Selecione a unidade.", erro: true);
            return;
        }

        try
        {
            IsBusy = true;
            await _supa.InitializeAsync();

            // NOVO: se tiver id em edição, atualiza; se não, cria
            if (_idFuncionarioEditando is null)
            {
                // 1) cria funcionário sem foto
                var criado = await _supa.AddFuncionarioAsync(Nome, UnidadeSelecionada.IdUnidade, null);
                if (criado is null)
                {
                    Status("Não foi possível salvar o funcionário.", erro: true);
                    return;
                }

                // 2) se houver foto, upload + update
                if (_fotoBytes is not null && _fotoBytes.Length > 0)
                {
                    var url = await _supa.UploadFotoFuncionarioAsync(criado.IdUnidade, criado.Id, _fotoBytes!, "image/jpeg");
                    try
                    {
                        await _supa.UpdateFuncionarioPhotoAsync(criado.Id, url);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Falha ao atualizar foto: {ex}");
                    }
                }

                Status($"Funcionário \"{Nome}\" salvo na unidade \"{UnidadeSelecionada.Nome}\".");
            }
            else
            {
                var id = _idFuncionarioEditando.Value;

                // pega foto atual (se já tiver)
                var atual = Funcionarios.FirstOrDefault(f => f.Id == id);
                var photoUrl = atual?.Photo;

                // se escolher nova foto, faz upload
                if (_fotoBytes is not null && _fotoBytes.Length > 0)
                {
                    photoUrl = await _supa.UploadFotoFuncionarioAsync(UnidadeSelecionada.IdUnidade, id, _fotoBytes!, "image/jpeg");
                }

                var atualizado = await _supa.UpdateFuncionarioAsync(id, Nome, UnidadeSelecionada.IdUnidade, photoUrl);
                if (atualizado is null)
                {
                    Status("Não foi possível atualizar o funcionário.", erro: true);
                    return;
                }

                Status($"Funcionário \"{Nome}\" atualizado.");
            }

            // recarrega lista
            await RecarregarFuncionariosAsync();

            // limpa para próximo cadastro / edição
            Nome = string.Empty;
            FotoPreview = null;
            _fotoBytes = null;
            _idFuncionarioEditando = null;
        }
        catch (Exception ex)
        {
            Status($"Erro ao salvar: {ex.Message}", erro: true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void EditarFuncionario(Funcionario func)
    {
        if (func is null)
            return;

        _idFuncionarioEditando = func.Id;
        Nome = func.Name;

        // garante que a unidade selecionada é a mesma do funcionário
        var unidade = Unidades.FirstOrDefault(u => u.IdUnidade == func.IdUnidade);
        if (unidade is not null)
        {
            _suppressUnidadeChanged = true;
            UnidadeSelecionada = unidade;
            _suppressUnidadeChanged = false;
        }

        FotoPreview = string.IsNullOrWhiteSpace(func.Photo)
            ? null
            : ImageSource.FromUri(new Uri(func.Photo));

        _fotoBytes = null; // só troca se escolher nova foto
        Status($"Editando funcionário \"{func.Name}\".");
    }

    [RelayCommand]
    public async Task ExcluirFuncionarioAsync(Funcionario func)
    {
        if (func is null)
            return;

        var confirm = await Application.Current.MainPage.DisplayAlert(
            "Excluir funcionário",
            $"Deseja realmente excluir \"{func.Name}\"?",
            "Sim", "Não");

        if (!confirm)
            return;

        try
        {
            IsBusy = true;
            await _supa.InitializeAsync();

            var ok = await _supa.DeleteFuncionarioAsync(func.Id);
            if (!ok)
            {
                Status("Não foi possível excluir o funcionário.", erro: true);
                return;
            }

            Funcionarios.Remove(func);
            Status($"Funcionário \"{func.Name}\" excluído.");
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
        // se quiser, depois criamos uma cor de erro/sucesso
    }
}
