namespace Avalia_.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var user = txtUser.Text?.Trim();
        var pass = txtPassword.Text?.Trim();

        if (user == "admin" && pass == "1234")
        {
            await Shell.Current.GoToAsync(nameof(MenuPage));
        }
        else
        {
            lblErro.Text = "Usuário ou senha inválidos.";
            lblErro.IsVisible = true;
        }
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        // Volta para a página anterior na pilha da Shell
        await Shell.Current.GoToAsync("..");
        // Se quiser forçar para uma rota específica:
        // await Shell.Current.GoToAsync(nameof(MenuPage));
    }
}