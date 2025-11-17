using Avalia_.Data;
using Avalia_.Services;
using Avalia_.ViewModels;
using Avalia_.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Licensing;
using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;

namespace Avalia_
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH5ed3RSQmleVER2XUJWYEg=");
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionCore()
                .ConfigureSyncfusionToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if ANDROID
            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("RemoveNativeBg", (handler, view) =>
            {
                handler.PlatformView.Background = null;
                handler.PlatformView.SetPadding(0, 0, 0, 0);
            });
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // Shell
            builder.Services.AddSingleton<AppShell>();

            // Feedback 
            builder.Services.AddSingleton<FeedbackViewModel>();
            builder.Services.AddSingleton<FeedbackPage>();

            // Cadastro de Unidades
            builder.Services.AddTransient<CadastroUnidadesViewModel>();
            builder.Services.AddTransient<CadastroUnidadesPage>();

            // Cadastro de Funcionarios
            builder.Services.AddTransient<CadastroFuncionarioViewModel>();
            builder.Services.AddTransient<CadastroFuncionarioPage>();

            builder.Services.AddTransient<AdminViewModel>();
            builder.Services.AddTransient<AdminPage>();

            //---------- Supabase ----------
            builder.Services.AddSingleton<global::Supabase.Client>(_ => SupabaseFactory.Create());
            builder.Services.AddSingleton<SupabaseService>();

            return builder.Build();
        }
    }
}
