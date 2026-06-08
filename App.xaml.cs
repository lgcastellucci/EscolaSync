namespace EscolaSync;

public partial class App : Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        try
        {
            var navPage = new NavigationPage(_mainPage)
            {
                BarBackgroundColor = Color.FromArgb("#1565C0"),
                BarTextColor = Colors.White
            };
            return new Window(navPage);
        }
        catch (Exception ex)
        {
            // Fallback: abre a página diretamente sem NavigationPage
            Android.Util.Log.Error("EscolaSync", $"CreateWindow falhou: {ex}");
            return new Window(_mainPage);
        }
    }
}
