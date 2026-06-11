namespace EscolaSync;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;

    public MainPage(MainViewModel vm)
    {
        Android.Util.Log.Debug("ES_BOOT", "30 MainPage() construtor iniciado");

        try
        {
            InitializeComponent();
            Android.Util.Log.Debug("ES_BOOT", "31 InitializeComponent() OK");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"31 ERRO InitializeComponent: {ex.GetType().Name}: {ex.Message}");
            Android.Util.Log.Error("ES_BOOT", $"STACK: {ex.StackTrace}");
            throw;
        }

        try
        {
            _vm = vm;
            BindingContext = vm;
            Android.Util.Log.Debug("ES_BOOT", "32 BindingContext atribuído OK");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"32 ERRO BindingContext: {ex.GetType().Name}: {ex.Message}");
            throw;
        }

        try
        {
            vm.LogAdded += OnLogAdded;
            Android.Util.Log.Debug("ES_BOOT", "33 LogAdded event registrado");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"33 ERRO LogAdded event: {ex.Message}");
        }

        Android.Util.Log.Debug("ES_BOOT", "34 MainPage() construtor concluído");
    }

    protected override void OnAppearing()
    {
        Android.Util.Log.Debug("ES_BOOT", "35 MainPage.OnAppearing() chamado");
        try
        {
            base.OnAppearing();
            Android.Util.Log.Debug("ES_BOOT", "36 base.OnAppearing() OK");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"36 ERRO base.OnAppearing: {ex.Message}");
        }

        try
        {
            _vm.InitializeAfterPermissions();
            Android.Util.Log.Debug("ES_BOOT", "37 InitializeAfterPermissions() chamado");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"37 ERRO InitializeAfterPermissions: {ex.Message}");
        }
    }

    private void OnLogAdded()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await Task.Delay(80);
                await LogScroll.ScrollToAsync(0, double.MaxValue, false);
            }
            catch { /* scroll não crítico */ }
        });
    }
}
