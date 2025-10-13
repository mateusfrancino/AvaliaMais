using Avalia_.Views;

namespace Avalia_
{
    public partial class AppShell : Shell
    {
        public AppShell(FeedbackPage feedbackPage)
        {
            InitializeComponent();

            Items.Add(new ShellContent
            {
                Title = "Home",
                Content = feedbackPage,
                Route = "Feedback"
            });
        }
    }
}
