// Views/CadastroFuncionarioPage.xaml.cs
using Avalia_.ViewModels;

namespace Avalia_.Views;

public partial class CadastroFuncionarioPage : ContentPage
{
    private readonly CadastroFuncionarioViewModel _vm;

    public CadastroFuncionarioPage(CadastroFuncionarioViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        // Volta para a página anterior na pilha da Shell
        await Shell.Current.GoToAsync("..");
        // Se quiser forçar para uma rota específica:
        // await Shell.Current.GoToAsync(nameof(MenuPage));
    }
}
