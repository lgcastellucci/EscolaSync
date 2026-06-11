namespace EscolaSync;

public partial class App : Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        Android.Util.Log.Debug("ES_BOOT", "20 App() construtor iniciado");

        try
        {
            InitializeComponent();
            Android.Util.Log.Debug("ES_BOOT", "21 InitializeComponent() OK");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"21 ERRO InitializeComponent: {ex.GetType().Name}: {ex.Message}");
            Android.Util.Log.Error("ES_BOOT", $"STACK: {ex.StackTrace}");
            throw;
        }

        _mainPage = mainPage;
        Android.Util.Log.Debug("ES_BOOT", "22 MainPage atribuída");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Android.Util.Log.Debug("ES_BOOT", "23 CreateWindow() chamado");

        try
        {
            var navPage = new NavigationPage(_mainPage)
            {
                BarBackgroundColor = Color.FromArgb("#1565C0"),
                BarTextColor = Colors.White
            };
            Android.Util.Log.Debug("ES_BOOT", "24 NavigationPage criada OK");

            var window = new Window(navPage);
            Android.Util.Log.Debug("ES_BOOT", "25 Window criada OK — retornando");
            return window;
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"24 ERRO CreateWindow: {ex.GetType().Name}: {ex.Message}");
            Android.Util.Log.Error("ES_BOOT", $"STACK: {ex.StackTrace}");
            // fallback sem NavigationPage
            try
            {
                Android.Util.Log.Debug("ES_BOOT", "25 Tentando fallback sem NavigationPage...");
                return new Window(_mainPage);
            }
            catch (Exception ex2)
            {
                Android.Util.Log.Error("ES_BOOT", $"25 ERRO fallback: {ex2.Message}");
                throw;
            }
        }
    }
}
