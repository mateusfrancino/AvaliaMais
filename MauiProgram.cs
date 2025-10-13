using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Maui.Toolkit.Carousel;
using Syncfusion.Maui.Toolkit.Hosting;

namespace Avalia_
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
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
                // remove fundo/underline nativo do Spinner no Android
                handler.PlatformView.Background = null;
                handler.PlatformView.SetPadding(0, 0, 0, 0);
            });
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // Registros de Views e ViewModels (boa prática MVVM)
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<Views.FeedbackPage>();
            builder.Services.AddSingleton<ViewModels.FeedbackViewModel>();

            return builder.Build();
        }
    }
}
