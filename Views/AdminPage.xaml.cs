using Avalia_.ViewModels;

namespace Avalia_.Views;

public partial class AdminPage : ContentPage
{
    private bool _loadedOnce;

    public AdminPage(AdminViewModel vm)   // receba via DI
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_loadedOnce) return;  // impede rodar novamente ao reentrar
        _loadedOnce = true;

        if (BindingContext is AdminViewModel vm)
            await vm.LoadAsync();
    }

    private async void OnVoltarTapped(object sender, TappedEventArgs e)
    {
        // Volta para a página anterior na pilha da Shell
        await Shell.Current.GoToAsync("..");

        // Se quiser forçar voltar para o MenuPage, seria:
        // await Shell.Current.GoToAsync(nameof(MenuPage));
    }
}
