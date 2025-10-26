using Avalia_.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using System.Security.Principal;

#if ANDROID
using Android.Views;
using Microsoft.Maui.Platform;
#endif

namespace Avalia_.Views;

public partial class FeedbackPage : ContentPage
{
    double _startWidth;
    double _targetDiameter;
    bool _ready;

#if ANDROID
    StatusBarVisibility? _oldUi;
#endif

    private List<(Grid grid, Border overlay, Label label)> _emojis = default!;

    private int _tapCount = 0;
    private DateTime _firstTapTime = DateTime.MinValue;
    private static readonly TimeSpan TapWindow = TimeSpan.FromSeconds(1.5); 
    private const int TapsToUnlock = 3;

    public FeedbackPage(FeedbackViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        _emojis = new()
    {
        (gridEmoji0, overlayEmoji0, labelEmoji0),
        (gridEmoji1, overlayEmoji1, labelEmoji1),
        (gridEmoji2, overlayEmoji2, labelEmoji2),
        (gridEmoji3, overlayEmoji3, labelEmoji3),
        (gridEmoji4, overlayEmoji4, labelEmoji4),
    };

        // estado inicial: todos pequenos e "desabilitados"
        ResetAll();
    }

    private async void AdminTitle_Tapped(object sender, TappedEventArgs e)
    {
        var now = DateTime.UtcNow;

        if (now - _firstTapTime > TapWindow)
        {
            _tapCount = 0;
            _firstTapTime = now;
        }

        _tapCount++;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        if (_tapCount >= TapsToUnlock)
        {
            _tapCount = 0;

            bool ok = await DisplayAlert("Acesso restrito", "Abrir área administrativa?", "Sim", "Não");
            if (!ok) return;

            // Abre LoginPage (normal). Se quiser modal, veja a nota logo abaixo.
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }
    }

    protected override void OnAppearing()
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

        // aguarda layout para ter medidas corretas do host
        Dispatcher.Dispatch(() =>
        {
            var hostWidth = Math.Max(0,
                ButtonHost.Width - (ButtonHost.Padding.Left + ButtonHost.Padding.Right));

            _targetDiameter = ConfirmButton.Height > 0 ? ConfirmButton.Height : 54;
            _startWidth = Math.Max(hostWidth, 280);

            ConfirmButton.WidthRequest = _startWidth;
            BtnContent.WidthRequest = _startWidth;

            ConfirmButton.HorizontalOptions = LayoutOptions.Center;
            _ready = true;
        });

        if (BindingContext is FeedbackViewModel vm)
        {
            vm.PropertyChanged += async (_, e) =>
            {
                if (e.PropertyName == nameof(FeedbackViewModel.IsSubmitting) && _ready)
                {
                    if (vm.IsSubmitting) await AnimateToLoadingAsync();
                    else await AnimateToNormalAsync();
                }
            };
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
#if ANDROID
        var decor = Platform.CurrentActivity?.Window?.DecorView;
        if (decor is not null && _oldUi is not null)
            decor.SystemUiVisibility = _oldUi.Value;
#endif
    }

    async Task AnimateToLoadingAsync()
    {
        this.AbortAnimation("MorphBtn");

        await Task.WhenAll(
            BtnText.ScaleTo(0.96, 90, Easing.CubicOut),
            BtnText.FadeTo(0, 120, Easing.CubicOut)
        );

        BtnSpinner.IsVisible = true;
        await BtnSpinner.FadeTo(1, 140, Easing.CubicOut);

        await AnimateWidthAndRadiusAsync(
            fromW: _startWidth,
            toW: _targetDiameter,
            fromR: 24,
            toR: _targetDiameter / 2,
            duration: 420,
            easing: Easing.SinInOut
        );
    }

    async Task AnimateToNormalAsync()
    {
        this.AbortAnimation("MorphBtn");

        await AnimateWidthAndRadiusAsync(
            fromW: _targetDiameter,
            toW: _startWidth,
            fromR: _targetDiameter / 2,
            toR: 24,
            duration: 480,
            easing: Easing.SinInOut
        );

        await BtnSpinner.FadeTo(0, 120, Easing.CubicIn);
        BtnSpinner.IsVisible = false;

        await Task.WhenAll(
            BtnText.FadeTo(1, 140, Easing.CubicOut),
            BtnText.ScaleTo(1.0, 90, Easing.CubicOut)
        );
    }

    Task AnimateWidthAndRadiusAsync(
        double fromW, double toW,
        double fromR, double toR,
        uint duration, Easing easing)
    {
        var tcs = new TaskCompletionSource();

        var anim = new Animation();

        // largura
        anim.Add(0, 1, new Animation(v =>
        {
            var w = fromW + (toW - fromW) * v;
            ConfirmButton.WidthRequest = w;
            BtnContent.WidthRequest = w;
        }));

        // raio dos cantos
        anim.Add(0, 1, new Animation(v =>
        {
            var r = fromR + (toR - fromR) * v;
            ConfirmButton.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(r)
            };
        }));

        // sombra
        anim.Add(0, 1, new Animation(v =>
        {
            float shadowOpacity = (float)(0.25 + (0.40 - 0.25) * v);
            ConfirmButton.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Opacity = shadowOpacity,
                Offset = new Point(0, 8),
                Radius = 12
            };
        }));

        anim.Commit(this, "MorphBtn", 16, duration, easing,
            finished: (v, c) => tcs.SetResult());

        return tcs.Task;
    }

    private void ResetAll()
    {
        foreach (var (grid, overlay, label) in _emojis)
        {
            grid.WidthRequest = grid.HeightRequest = 100;
            label.FontSize = 52;
            overlay.IsVisible = true;          // mostra véu cinza
            overlay.InputTransparent = false;  // overlay captura o toque
        }
    }

    private void SelectIndex(int index)
    {
        if (index < 0 || index >= _emojis.Count) return;
        ResetAll();

        var (grid, overlay, label) = _emojis[index];
        grid.WidthRequest = grid.HeightRequest = 140;
        label.FontSize = 82;
        overlay.IsVisible = false;            // remove véu do selecionado
    }

    private void OnEmojiTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is int i) { SelectIndex(i); return; }
        if (e.Parameter is string s && int.TryParse(s, out var idx))
            SelectIndex(idx);
    }
}
