using EscolaSync.Services;

namespace EscolaSync;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Solicita permissões na primeira vez que a tela aparece
        await RequestPermissionsAsync();
    }

    private async Task RequestPermissionsAsync()
    {
        // READ_MEDIA_IMAGES (Android 13+) ou READ_EXTERNAL_STORAGE (Android 12-)
        var readStatus = await Permissions.RequestAsync<Permissions.Media>();

        if (readStatus != PermissionStatus.Granted)
        {
            await DisplayAlertAsync(
                "Permissão necessária",
                "O app precisa de acesso às fotos para funcionar.",
                "OK");
        }
    }

    // Chamado pelo botão "Enviar Agora" via Command, mas o SyncAsync
    // precisa da Activity para deleção no Android 11+
    // Sobrescrevemos o comando para injetar a Activity
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is MainViewModel vm)
        {
            // Substitui o SyncCommand para passar a Activity
            // O binding do XAML usa vm.SyncCommand que já está configurado
        }
    }

    // Botão Enviar — chamado diretamente para passar a Activity
    private async void OnSendButtonClicked(object sender, EventArgs e)
    {
        var activity = Platform.CurrentActivity;
        if (activity == null) return;

        await _viewModel.SyncAsync(activity);
    }

    // Recebe resultado do intent de deleção (Android 11+)
    // Registrado na MainActivity
    public static void OnDeleteResult(int resultCode)
    {
        MediaStoreService.DeleteResultCallback?.Invoke(resultCode);
        MediaStoreService.DeleteResultCallback = null;
    }
}
