using System;
using Avalia_.ViewModels;
using Microsoft.Maui.Controls;

namespace Avalia_.Views
{
    public partial class CadastroUnidadesPage : ContentPage
    {
        public CadastroUnidadesPage(CadastroUnidadesViewModel vm) 
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is CadastroUnidadesViewModel vm)
                await vm.CarregarUnidadesAsync();
        }

        private async void OnVoltarClicked(object sender, EventArgs e)
        {
            // Volta para a página anterior na pilha da Shell
            await Shell.Current.GoToAsync("..");
            // Se quiser forçar para uma rota específica:
            // await Shell.Current.GoToAsync(nameof(MenuPage));
        }
    }
}
