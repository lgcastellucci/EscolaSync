using EscolaSync.Services;

namespace EscolaSync;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private bool _initialized = false;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Solicita permissões apenas uma vez
        if (!_initialized)
        {
            _initialized = true;
            await RequestPermissionsAsync();

            // Só carrega álbuns DEPOIS de pedir permissão
            _viewModel.InitializeAfterPermissions();
        }
    }

    private async Task RequestPermissionsAsync()
    {
        try
        {
            var readStatus = await Permissions.RequestAsync<Permissions.Media>();

            if (readStatus != PermissionStatus.Granted)
            {
                await DisplayAlertAsync(
                    "Permissão necessária",
                    "O app precisa de acesso às fotos para funcionar. Conceda a permissão nas configurações do celular.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PERM] Erro ao solicitar permissão: {ex.Message}");
        }
    }

    // Botão Enviar — passa a Activity para deletar fotos no Android 11+
    private async void OnSendButtonClicked(object sender, EventArgs e)
    {
        var activity = Platform.CurrentActivity;
        if (activity == null) return;
        await _viewModel.SyncAsync(activity);
    }

    // Recebe resultado do intent de deleção (Android 11+)
    public static void OnDeleteResult(int resultCode)
    {
        MediaStoreService.DeleteResultCallback?.Invoke(resultCode);
        MediaStoreService.DeleteResultCallback = null;
    }
}
