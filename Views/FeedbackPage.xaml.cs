using Avalia_.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace Avalia_.Views;

public partial class FeedbackPage : ContentPage
{
    double _startWidth;
    double _targetDiameter;
    bool _ready;

    public FeedbackPage(FeedbackViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // aguarda layout para ter medidas corretas do host
        Dispatcher.Dispatch(() =>
        {
            // largura útil do host (= largura total - padding horizontal)
            var hostWidth = Math.Max(0,
                ButtonHost.Width - (ButtonHost.Padding.Left + ButtonHost.Padding.Right));

            // altura do botão já renderizada
            _targetDiameter = ConfirmButton.Height > 0 ? ConfirmButton.Height : 54;

            // define a largura inicial (full-ish) com um mínimo confortável
            _startWidth = Math.Max(hostWidth, 280);

            // fixa a largura inicial no Border e no conteúdo
            ConfirmButton.WidthRequest = _startWidth;
            BtnContent.WidthRequest = _startWidth;

            // agora mudamos para Center para permitir encolher
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

    async Task AnimateToLoadingAsync()
    {
        this.AbortAnimation("MorphBtn"); // evita colisão

        // some o texto suavemente (escala + fade)
        await Task.WhenAll(
            BtnText.ScaleTo(0.96, 90, Easing.CubicOut),
            BtnText.FadeTo(0, 120, Easing.CubicOut)
        );

        // mostra o spinner
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

        // esconde spinner e volta texto suavemente
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

        // sombra levemente mais forte ao encolher
        anim.Add(0, 1, new Animation(v =>
        {
            float shadowOpacity = (float)(0.25 + (0.40 - 0.25) * v); 
            ConfirmButton.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black), // sem opacidade no Brush
                Opacity = shadowOpacity,                    // opacidade é da Shadow
                Offset = new Point(0, 8),
                Radius = 12
            };
        }));


        anim.Commit(this, "MorphBtn", 16, duration, easing,
            finished: (v, c) => tcs.SetResult());

        return tcs.Task;
    }

}