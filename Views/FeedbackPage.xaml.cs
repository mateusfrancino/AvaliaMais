using System;
using System.Collections.Generic;
using System.ComponentModel; // <-- precisa disso
using Avalia_.ViewModels;
using Microsoft.Maui.Controls;

#if ANDROID
using Android.Views;
using Microsoft.Maui.Platform;
#endif

namespace Avalia_.Views
{
    public partial class FeedbackPage : ContentPage
    {
        bool _ready;

#if ANDROID
        StatusBarVisibility? _oldUi;
#endif

        // antes era (Grid, Border, Label)
        private readonly List<(Grid grid, Border overlay, Image image)> _emojis;

        // Easter-egg para Admin
        private int _tapCount = 0;
        private DateTime _firstTapTime = DateTime.MinValue;
        private static readonly TimeSpan TapWindow = TimeSpan.FromSeconds(1.5);
        private const int TapsToUnlock = 3;

        // delegate correto para INotifyPropertyChanged
        private PropertyChangedEventHandler? _vmWatcher;

        public FeedbackPage(FeedbackViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;

            _emojis = new()
            {
                (gridEmoji0, overlayEmoji0, imageEmoji0),
                (gridEmoji1, overlayEmoji1, imageEmoji1),
                (gridEmoji2, overlayEmoji2, imageEmoji2),
                (gridEmoji3, overlayEmoji3, imageEmoji3),
                (gridEmoji4, overlayEmoji4, imageEmoji4),
            };

            // estado inicial: todos “desselecionados”
            ResetAll();

            // quando a VM pedir, reseta visuais
            vm.ResetUiRequested += () => MainThread.BeginInvokeOnMainThread(ResetForm);
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

                await Shell.Current.GoToAsync(nameof(LoginPage));
            }
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

            _ready = true;

            if (BindingContext is FeedbackViewModel vm)
            {
                // evita múltiplas inscrições ao navegar/voltar
                if (_vmWatcher is not null)
                    vm.PropertyChanged -= _vmWatcher;

                _vmWatcher = (sender, e) =>
                {
                    if (e.PropertyName == nameof(FeedbackViewModel.IsSubmitting) && _ready)
                    {
                        var sub = vm.IsSubmitting;
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            // só alterna texto vs spinner — sem morph
                            BtnText.IsVisible = !sub;
                            BtnSpinner.IsVisible = sub;
                            BtnSpinner.IsRunning = sub;

                            // bloqueia o botão enquanto envia
                            ConfirmButton.IsEnabled = !sub;
                        });
                    }
                };

                vm.PropertyChanged += _vmWatcher;

                await vm.LoadAsync();
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

            if (BindingContext is FeedbackViewModel vm && _vmWatcher is not null)
            {
                vm.PropertyChanged -= _vmWatcher;
                _vmWatcher = null;
            }
        }

        private void ResetForm()
        {
            ResetAll();
            CommentEditor.Text = string.Empty; // reforço visual
        }

        private void ResetAll()
        {
            foreach (var (grid, overlay, image) in _emojis)
            {
                // tamanho “normal”
                grid.WidthRequest = 100;
                grid.HeightRequest = 100;

                // volta o emoji pro tamanho base
                image.Scale = 1; // ou image.WidthRequest / HeightRequest se preferir

                // véu cinza por cima (estado “não selecionado”)
                overlay.IsVisible = true;
                overlay.InputTransparent = false; // overlay captura o toque
            }
        }

        private void SelectIndex(int index)
        {
            if (index < 0 || index >= _emojis.Count)
                return;

            ResetAll();

            var (grid, overlay, image) = _emojis[index];

            // dá uma “crescidinha” no selecionado
            grid.WidthRequest = 130;
            grid.HeightRequest = 130;
            image.Scale = 1.30; // ajuste fino se quiser mais/menos destaque

            // tira o véu do selecionado
            overlay.IsVisible = false;
            // se quiser garantir que o toque passe pro grid/image:
            overlay.InputTransparent = true;
        }


        private void OnEmojiTapped(object sender, TappedEventArgs e)
        {
            if (BindingContext is FeedbackViewModel vm && vm.IsSubmitting) return;

            int idx = -1;
            if (e.Parameter is int i) idx = i;
            else if (e.Parameter is string s && int.TryParse(s, out var parsed)) idx = parsed;

            if (idx >= 0)
            {
                SelectIndex(idx);
                if (BindingContext is FeedbackViewModel vm2)
                    vm2.AttendantScore = idx + 1; // 0..4 -> 1..5
            }
        }
    }
}
