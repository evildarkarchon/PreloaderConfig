using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PreloaderConfigurator.ViewModels;

namespace PreloaderConfigurator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}