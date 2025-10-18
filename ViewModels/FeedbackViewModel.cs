using Avalia_.Models;
using Avalia_.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Avalia_.ViewModels
{
    public partial class FeedbackViewModel : ObservableObject
    {
        public ObservableCollection<Attendant> Attendants { get; } = new();


        [ObservableProperty] private Attendant? selectedAttendant;
        [ObservableProperty] private double attendantScore = 0;   
        [ObservableProperty] private double institutionScore = 0;
        [ObservableProperty] private string? comment;
        [ObservableProperty] private bool isSubmitting;

        public FeedbackViewModel()
        {
            // Mock inicial — depois troque pelo Supabase
            Attendants.Add(new Attendant { Name = "Letícia", PhotoUrl = "https://i.pravatar.cc/150?img=36" });
            Attendants.Add(new Attendant { Name = "Paulo", PhotoUrl = "https://i.pravatar.cc/150?img=8" });
            Attendants.Add(new Attendant { Name = "Marina", PhotoUrl = "https://i.pravatar.cc/150?img=5" });

            SelectedAttendant = Attendants.FirstOrDefault();

            // opcional: valor inicial “sem seleção”
            AttendantScore = 0;

            WeakReferenceMessenger.Default.Register<ClearFeedbackMessage>(this, (_, __) => Reset());
        }

        // NOVO: comando para quando tocar/clicar num emoji (CollectionView -> CommandParameter=Value)
        [RelayCommand]
        private void SelectAttendantEmoji(int value)
        {
            AttendantScore = value; // vai de 1..5
        }

        void Reset()
        {
            AttendantScore = 0;
            InstitutionScore = 0;
            Comment = string.Empty;
            SelectedAttendant = null;
        }

        [RelayCommand]
        private async Task ConfirmAsync()
        {
            if (IsSubmitting) return;
            IsSubmitting = true;
            try
            {
                // TODO: salvar / enviar
                await Task.Delay(1500);

                await Shell.Current.GoToAsync(nameof(ObrigadoPage)); // ou Navigation.PushAsync(new ObrigadoPage())


                // Reseta campos
                AttendantScore = 0;
                InstitutionScore = 0;
                Comment = string.Empty;
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}
