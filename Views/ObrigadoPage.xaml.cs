using Avalia_.Models;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading; // <-- para CancellationTokenSource
#if ANDROID
using Android.Views;
using Microsoft.Maui.Platform;
#endif

namespace Avalia_.Views;

public partial class ObrigadoPage : ContentPage
{
#if ANDROID
    StatusBarVisibility? _oldUi;
#endif
    CancellationTokenSource? _pulseCts;

    public ObrigadoPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

#if ANDROID
        var decor = Platform.CurrentActivity?.Window?.DecorView;
        if (decor is not null)
        {
            _oldUi = decor.SystemUiVisibility;
            decor.SystemUiVisibility = (StatusBarVisibility)(
                SystemUiFlags.ImmersiveSticky |
                SystemUiFlags.Fullscreen |
                SystemUiFlags.HideNavigation);
        }
#endif

        await Task.WhenAll(
            ThankStack.FadeTo(1, 280, Easing.CubicOut),
            ThankIcon.ScaleTo(1.08, 320, Easing.CubicOut)
        );
        await ThankIcon.ScaleTo(1.0, 180, Easing.CubicIn);

        _pulseCts = new CancellationTokenSource();
        _ = PulseAsync(_pulseCts.Token); // inicia o pulso em loop

        await Task.Delay(5000); // tempo de exibição

        _pulseCts.Cancel(); // para o pulso antes de navegar

        WeakReferenceMessenger.Default.Send(new ClearFeedbackMessage());

        if (Shell.Current is not null)
            await Shell.Current.GoToAsync("///Feedback");
        else
            await Navigation.PopToRootAsync(true);
    }

    protected override void OnDisappearing()
    {
        _pulseCts?.Cancel();
        _pulseCts = null;

        base.OnDisappearing();
#if ANDROID
        var decor = Platform.CurrentActivity?.Window?.DecorView;
        if (decor is not null && _oldUi is not null)
            decor.SystemUiVisibility = _oldUi.Value;
#endif
    }

    async Task PulseAsync(CancellationToken ct)
    {
        const double baseScale = 1.0;
        const double peakScale = 1.06;  // intensidade do pulso
        const uint duration = 800;      // velocidade (ms)

        try
        {
            ThankIcon.Scale = baseScale;

            while (!ct.IsCancellationRequested)
            {
                await ThankIcon.ScaleTo(peakScale, duration, Easing.SinInOut);
                await ThankIcon.ScaleTo(baseScale, duration, Easing.SinInOut);
            }
        }
        catch (TaskCanceledException)
        {
            // navegação cancelou — sem problemas
        }
        finally
        {
            // garante que volta ao estado base
            try { await ThankIcon.ScaleTo(baseScale, 120); } catch { /* noop */ }
        }
    }
}
