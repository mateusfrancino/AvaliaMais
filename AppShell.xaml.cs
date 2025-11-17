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

            // Rotas (não aparecem no topo/Flyout por padrão)
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(MenuPage), typeof(MenuPage));
            Routing.RegisterRoute(nameof(AdminPage), typeof(AdminPage)); // apenas rota
            Routing.RegisterRoute(nameof(CadastroUnidadesPage), typeof(CadastroUnidadesPage));
            Routing.RegisterRoute(nameof(CadastroFuncionarioPage), typeof(CadastroFuncionarioPage));
            Routing.RegisterRoute(nameof(ObrigadoPage), typeof(ObrigadoPage));
        }
    }
}
