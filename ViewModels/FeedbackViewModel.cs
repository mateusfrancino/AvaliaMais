using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using Avalia_.Models;



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

            // Mock inicial — depois você pode trocar pela chamada ao Supabase

            Attendants.Add(new Attendant { Name = "Letícia", PhotoUrl = "https://i.pravatar.cc/150?img=36" });

            Attendants.Add(new Attendant { Name = "Paulo", PhotoUrl = "https://i.pravatar.cc/150?img=8" });

            Attendants.Add(new Attendant { Name = "Marina", PhotoUrl = "https://i.pravatar.cc/150?img=5" });



            SelectedAttendant = Attendants.FirstOrDefault();

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


                await Shell.Current.DisplayAlert("Obrigado!", "Sua avaliação foi registrada com sucesso.", "Fechar");



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

