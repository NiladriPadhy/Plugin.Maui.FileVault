namespace Plugin.Maui.FileVault.Sample;

public partial class App : Application
{
    readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(_mainPage);
}
