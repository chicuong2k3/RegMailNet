namespace RegMailNet.Ui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage())
            {
                Title = "RegMailNet",
                Width = 1100,
                Height = 700,
            };

#if WINDOWS
            window.Created += (s, e) =>
            {
                var platformWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (platformWindow?.Content is Microsoft.UI.Xaml.FrameworkElement root)
                {
                    // Lock the window to a fixed size by disabling resize
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                        Microsoft.UI.Win32Interop.GetWindowIdFromWindow(
                            WinRT.Interop.WindowNative.GetWindowHandle(platformWindow)));
                    if (appWindow?.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                    {
                        presenter.IsResizable = false;
                        presenter.IsMaximizable = false;
                    }
                }
            };
#endif

            return window;
        }
    }
}