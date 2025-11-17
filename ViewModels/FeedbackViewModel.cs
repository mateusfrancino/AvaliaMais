using Avalia_.Services;
using Avalia_.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Avalia_.ViewModels
{
    public class Attendant
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? PhotoUrl { get; set; }
    }

    public partial class FeedbackViewModel : ObservableObject
    {
        private readonly SupabaseService _supa;

        // dispara reset visual na View (emoji/Editor etc.)
        public event Action? ResetUiRequested;

        private int UnidadeAtualId => Preferences.Get(CadastroUnidadesViewModel.PrefUnidadeIdKey, 0);

        public ObservableCollection<Attendant> Attendants { get; } = new();

        [ObservableProperty] private Attendant? selectedAttendant;
        [ObservableProperty] private double attendantScore = 0;     // 1..5 (emoji)
        [ObservableProperty] private double institutionScore = 0;   // 1..5
        [ObservableProperty] private string? comment;
        [ObservableProperty] private bool isSubmitting;

        public FeedbackViewModel(SupabaseService supa)
        {
            _supa = supa;
        }

        // Carregar funcionários da unidade fixa
        [RelayCommand]
        public async Task LoadAsync()
        {
            try
            {
                IsSubmitting = true; // usa o spinner do botão como “busy”
                await _supa.InitializeAsync();

                Attendants.Clear();

                var funcionarios = await _supa.GetFuncionariosAsync(UnidadeAtualId);
                foreach (var f in funcionarios)
                {
                    Attendants.Add(new Attendant
                    {
                        Id = f.Id,
                        Name = f.Name,
                        PhotoUrl = f.Photo
                    });
                }

                // NÃO auto-seleciona mais ninguém:
                SelectedAttendant = null;
                AttendantScore = 0;
                InstitutionScore = 0;
                Comment = string.Empty;

                // pede pra view resetar os visuais (emojis, editor etc.)
                ResetUiRequested?.Invoke();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao carregar funcionários: {ex.Message}", "OK");
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        [RelayCommand]
        private void SelectAttendantEmoji(int value) => AttendantScore = value;

        [RelayCommand]
        private async Task ConfirmAsync()
        {
            if (IsSubmitting) return;

            // validações simples
            if (AttendantScore < 1 || AttendantScore > 5)
            {
                await Shell.Current.DisplayAlert("Atenção", "Escolha um emoji (1 a 5).", "OK");
                return;
            }
            if (InstitutionScore < 1 || InstitutionScore > 5)
            {
                await Shell.Current.DisplayAlert("Atenção", "Dê uma nota para a instituição (1 a 5).", "OK");
                return;
            }
            // Se quiser obrigar atendente, descomente:
            // if (SelectedAttendant is null) { await Shell.Current.DisplayAlert("Atenção","Selecione quem lhe atendeu.","OK"); return; }

            try
            {
                IsSubmitting = true;

                var criado = await _supa.AddAvaliacaoAsync(
                    idUnidade: UnidadeAtualId,
                    idFuncionario: SelectedAttendant?.Id,
                    emojiScore: (short)AttendantScore,
                    nota: (short)InstitutionScore,
                    comentario: string.IsNullOrWhiteSpace(Comment) ? null : Comment,
                    criadoEmUtc: DateTime.UtcNow
                );

                // limpa campos para próximo uso (inclusive atendente)
                SelectedAttendant = null;
                AttendantScore = 0;
                InstitutionScore = 0;
                Comment = string.Empty;

                // reseta visuais (emojis/Editor na View)
                ResetUiRequested?.Invoke();

                // navega para “Obrigado”
                await Shell.Current.GoToAsync(nameof(ObrigadoPage));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao enviar avaliação: {ex.Message}", "OK");
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}
