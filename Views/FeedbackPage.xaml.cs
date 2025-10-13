using Avalia_.ViewModels;

namespace Avalia_.Views;

public partial class FeedbackPage : ContentPage
{
    public FeedbackPage(FeedbackViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm; 
    }
}