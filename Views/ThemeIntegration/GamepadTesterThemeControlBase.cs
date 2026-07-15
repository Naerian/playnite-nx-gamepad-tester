using GamepadTester.Converters;
using GamepadTester.Services;
using GamepadTester.ViewModels;
using Playnite.SDK.Controls;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace GamepadTester.Views.ThemeIntegration
{
    public abstract class GamepadTesterThemeControlBase : PluginUserControl
    {
        public static readonly DependencyProperty IsInputCaptureActiveProperty =
            DependencyProperty.Register(
                "IsInputCaptureActive",
                typeof(bool),
                typeof(GamepadTesterThemeControlBase),
                new PropertyMetadata(false));

        private readonly GamepadTesterThemeRuntimeHandle runtimeHandle;

        protected GamepadTesterThemeControlBase(GamepadTesterSettings settings, Func<string, string> localizer)
        {
            runtimeHandle = GamepadTesterThemeRuntime.Acquire(settings, localizer);
            DataContext = runtimeHandle.ViewModel;
            SetBinding(IsInputCaptureActiveProperty, Bind("IsFullscreenInputCaptureActive"));
            Unloaded += OnUnloaded;
        }

        public bool IsInputCaptureActive
        {
            get { return (bool)GetValue(IsInputCaptureActiveProperty); }
        }

        protected GamepadTesterViewModel ViewModel
        {
            get { return runtimeHandle.ViewModel; }
        }

        protected string L(string key, string fallback)
        {
            return runtimeHandle.Localize(key, fallback);
        }

        protected static Binding Bind(string path)
        {
            return new Binding(path);
        }

        protected static Binding Bind(string path, IValueConverter converter)
        {
            return new Binding(path) { Converter = converter };
        }

        protected static TextBlock Text(string text, double size, FontWeight weight)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = size,
                FontWeight = weight,
                Foreground = DynamicBrush("TextBrush", Brushes.White),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        protected static Border Panel()
        {
            return new Border
            {
                Padding = new Thickness(18),
                CornerRadius = new CornerRadius(8),
                Background = DynamicBrush("ControlBackgroundBrush", new SolidColorBrush(Color.FromRgb(20, 24, 31))),
                BorderBrush = DynamicBrush("ControlBorderBrush", new SolidColorBrush(Color.FromRgb(63, 72, 88))),
                BorderThickness = new Thickness(1)
            };
        }

        protected static Brush DynamicBrush(string key, Brush fallback)
        {
            var brush = Application.Current == null ? null : Application.Current.TryFindResource(key) as Brush;
            return brush ?? fallback;
        }

        protected static IValueConverter BoolToVisibility()
        {
            return new BoolToVisibilityConverter();
        }

        protected static IValueConverter InverseBoolToVisibility()
        {
            return new InverseBoolToVisibilityConverter();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;
            runtimeHandle.Dispose();
        }
    }

    internal sealed class GamepadTesterThemeRuntimeHandle : IDisposable
    {
        private readonly Action release;
        private bool disposed;

        public GamepadTesterThemeRuntimeHandle(GamepadTesterViewModel viewModel, Func<string, string> localizer, Action release)
        {
            ViewModel = viewModel;
            Localizer = localizer;
            this.release = release;
        }

        public GamepadTesterViewModel ViewModel { get; private set; }
        public Func<string, string> Localizer { get; private set; }

        public string Localize(string key, string fallback)
        {
            if (Localizer == null)
            {
                return fallback;
            }

            var value = Localizer(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            release();
        }
    }

    internal static class GamepadTesterThemeRuntime
    {
        private static readonly object sync = new object();
        private static GamepadTesterViewModel viewModel;
        private static Func<string, string> localizer;
        private static int referenceCount;

        public static GamepadTesterThemeRuntimeHandle Acquire(GamepadTesterSettings settings, Func<string, string> localize)
        {
            lock (sync)
            {
                if (viewModel == null)
                {
                    localizer = localize;
                    viewModel = new GamepadTesterViewModel(
                        new GamepadPollingService(new SdlGamepadProvider()),
                        settings,
                        localize);
                    viewModel.IsFullscreenSimplifiedMode = true;
                    viewModel.IsInputLogEnabled = false;
                    viewModel.Start();
                }

                referenceCount++;
                return new GamepadTesterThemeRuntimeHandle(viewModel, localizer, Release);
            }
        }

        private static void Release()
        {
            lock (sync)
            {
                if (referenceCount > 0)
                {
                    referenceCount--;
                }

                if (referenceCount != 0 || viewModel == null)
                {
                    return;
                }

                viewModel.Dispose();
                viewModel = null;
                localizer = null;
            }
        }
    }

    internal sealed class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
