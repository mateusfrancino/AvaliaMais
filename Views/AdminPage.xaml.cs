using Avalia_.ViewModels;

namespace Avalia_.Views;

public partial class AdminPage : ContentPage
{
    public AdminPage()
    {
        InitializeComponent();
        BindingContext = new AdminViewModel(); 
    }
}
