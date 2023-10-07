using Autofac;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Frontend.Desktop.Core;

namespace Frontend.Desktop
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            IoC.Build();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var vm = IoC.Container.Resolve<MainViewModel>();
            vm.Init();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow()
                {
                    DataContext = vm
                };
                desktop.ShutdownRequested += Desktop_ShutdownRequested;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private async void Desktop_ShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            await ((ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow.DataContext as MainViewModel).ClientClosing();
        }
    }
}
