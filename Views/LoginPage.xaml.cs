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
            await Shell.Current.GoToAsync(nameof(AdminPage));
        }
        else
        {
            lblErro.Text = "Usuário ou senha inválidos.";
            lblErro.IsVisible = true;
        }
    }
}