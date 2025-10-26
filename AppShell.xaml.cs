using Avalia_.Views;

namespace Avalia_
{
    public partial class AppShell : Shell
    {
        public AppShell(FeedbackPage feedbackPage)
        {
            InitializeComponent();

            // Item principal (Home)
            Items.Add(new ShellContent
            {
                Title = "Home",
                Content = feedbackPage,
                Route = "Feedback"
            });

            // Rotas "simples" (não-topo)
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(AdminPage), typeof(AdminPage));   // <-- só registrar
            Routing.RegisterRoute(nameof(ObrigadoPage), typeof(ObrigadoPage));
        }
    }
}
