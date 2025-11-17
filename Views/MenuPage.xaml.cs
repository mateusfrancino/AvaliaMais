using System;
using Microsoft.Maui.Controls;

namespace Avalia_.Views
{
    public partial class MenuPage : ContentPage
    {
        public MenuPage()
        {
            InitializeComponent();
        }

        private async void OnVoltarClicked(object sender, EventArgs e)
        {
            // Volta para a página anterior (LoginPage, no seu fluxo atual)
            await Shell.Current.GoToAsync("..");

            // Se quiser garantir que sempre vá para o LoginPage, pode usar:
            // await Shell.Current.GoToAsync(nameof(LoginPage));
        }


        private async void OnAdminTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(AdminPage));
        }

        private async void OnUnidadesTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CadastroUnidadesPage));
        }

        private async void OnFuncionarioTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CadastroFuncionarioPage));
        }
    }
}
