using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using EscolaSync.Models;
using EscolaSync.Services;

namespace EscolaSync;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly GoogleAuthService _authService;
    private readonly DriveUploadService _driveService;
    private readonly MediaStoreService _mediaService;

    private bool _isAuthenticated;
    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set { _isAuthenticated = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotAuthenticated)); }
    }
    public bool IsNotAuthenticated => !IsAuthenticated;

    private string _accountLabel = "Nenhuma conta conectada";
    public string AccountLabel
    {
        get => _accountLabel;
        set { _accountLabel = value; OnPropertyChanged(); }
    }

    private string _selectedAlbum = "Escola";
    public string SelectedAlbum
    {
        get => _selectedAlbum;
        set { _selectedAlbum = value; OnPropertyChanged(); RefreshPhotoCount(); }
    }

    private int _photoCount;
    public int PhotoCount
    {
        get => _photoCount;
        set { _photoCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(PhotoCountLabel)); }
    }
    public string PhotoCountLabel => _photoCount == 0
        ? "Nenhuma foto encontrada"
        : $"{_photoCount} foto(s) no álbum";

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotBusy)); }
    }
    public bool IsNotBusy => !IsBusy;

    private int _progress;
    public int Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private string _statusColor = "#1565C0";
    public string StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; OnPropertyChanged(); }
    }

    private bool _hasStatus;
    public bool HasStatus
    {
        get => _hasStatus;
        set { _hasStatus = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> Albums { get; } = new();

    public ICommand LoginCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand SyncCommand { get; }
    public ICommand RefreshAlbumsCommand { get; }

    public MainViewModel(
        GoogleAuthService authService,
        DriveUploadService driveService,
        MediaStoreService mediaService)
    {
        _authService  = authService;
        _driveService = driveService;
        _mediaService = mediaService;

        LoginCommand         = new Command(async () => await LoginAsync(),  () => IsNotBusy);
        LogoutCommand        = new Command(async () => await LogoutAsync(), () => IsNotBusy);
        SyncCommand          = new Command(async () => await SyncAsync(),   () => IsAuthenticated && IsNotBusy);
        RefreshAlbumsCommand = new Command(() => LoadAlbums());

        // NÃO chama LoadAlbums() aqui — sem permissão ainda causa crash no startup
        // LoadAlbums() é chamado pelo OnAppearing após permissões concedidas
        _ = TryRestoreSessionAsync();
    }

    /// <summary>
    /// Chamado pela MainPage.OnAppearing após permissões serem solicitadas.
    /// </summary>
    public void InitializeAfterPermissions()
    {
        LoadAlbums();
    }

    private async Task TryRestoreSessionAsync()
    {
        try
        {
            bool restored = await _authService.TryRestoreSessionAsync();
            if (restored)
            {
                IsAuthenticated = true;
                AccountLabel = "Conta Drive conectada ✓";
                ShowStatus("Sessão restaurada com sucesso", "#388E3C");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VM] TryRestore erro: {ex.Message}");
        }
    }

    private async Task LoginAsync()
    {
        IsBusy = true;
        ShowStatus("Abrindo browser para login...", "#1565C0");

        try
        {
            bool ok = await _authService.AuthenticateAsync();
            if (ok)
            {
                IsAuthenticated = true;
                AccountLabel = "Conta Drive conectada ✓";
                ShowStatus("Login realizado com sucesso!", "#388E3C");
            }
            else
            {
                ShowStatus("Falha no login. Tente novamente.", "#D32F2F");
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Erro no login: {ex.Message}", "#D32F2F");
        }

        IsBusy = false;
    }

    private async Task LogoutAsync()
    {
        IsBusy = true;
        try
        {
            await _authService.LogoutAsync();
            _driveService.ClearCache();
            IsAuthenticated = false;
            AccountLabel = "Nenhuma conta conectada";
            ShowStatus("Logout realizado.", "#757575");
        }
        catch (Exception ex)
        {
            ShowStatus($"Erro ao sair: {ex.Message}", "#D32F2F");
        }
        IsBusy = false;
    }

    public void LoadAlbums()
    {
        try
        {
            Albums.Clear();
            var albums = _mediaService.GetAlbums();
            foreach (var a in albums)
                Albums.Add(a);

            if (albums.Contains("Escola"))
                SelectedAlbum = "Escola";
            else if (albums.Count > 0)
                SelectedAlbum = albums[0];

            RefreshPhotoCount();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VM] LoadAlbums erro: {ex.Message}");
            ShowStatus($"Erro ao listar álbuns: {ex.Message}", "#D32F2F");
        }
    }

    private void RefreshPhotoCount()
    {
        try
        {
            var photos = _mediaService.GetPhotosFromAlbum(SelectedAlbum);
            PhotoCount = photos.Count;
        }
        catch
        {
            PhotoCount = 0;
        }
    }

    public async Task SyncAsync(Android.App.Activity? activity = null)
    {
        if (!IsAuthenticated) return;

        IsBusy = true;
        Progress = 0;

        var result = new SyncResult();

        try
        {
            ShowStatus("Lendo fotos do álbum...", "#1565C0");

            var photos = _mediaService.GetPhotosFromAlbum(SelectedAlbum);
            result.Total = photos.Count;

            if (photos.Count == 0)
            {
                ShowStatus("Nenhuma foto encontrada no álbum.", "#FF8F00");
                IsBusy = false;
                return;
            }

            ShowStatus($"Enviando {photos.Count} foto(s)...", "#1565C0");

            for (int i = 0; i < photos.Count; i++)
            {
                var photo = photos[i];
                int overallPct = (int)((i / (double)photos.Count) * 100);
                Progress = overallPct;

                ShowStatus($"Enviando {i + 1}/{photos.Count}: {photo.DisplayName}", "#1565C0");

                var photoProgress = new Progress<int>(pct =>
                {
                    int overall = overallPct + (int)(pct / (double)photos.Count);
                    Progress = Math.Min(overall, 99);
                });

                bool uploaded = await _driveService.UploadPhotoAsync(photo, photoProgress);

                if (uploaded)
                {
                    result.Uploaded++;
                    bool deleted = activity != null
                        ? await _mediaService.DeletePhotoAsync(photo, activity)
                        : false;
                    if (deleted) result.Deleted++;
                }
                else
                {
                    result.Failed++;
                    result.Errors.Add(photo.DisplayName);
                }
            }

            Progress = 100;

            if (result.Failed == 0)
                ShowStatus($"✓ Concluído! {result.Summary}", "#388E3C");
            else
                ShowStatus($"⚠ Parcial: {result.Summary}", "#FF8F00");

            RefreshPhotoCount();
        }
        catch (Exception ex)
        {
            ShowStatus($"Erro inesperado: {ex.Message}", "#D32F2F");
            System.Diagnostics.Debug.WriteLine($"[SYNC] Exceção: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowStatus(string msg, string color)
    {
        StatusMessage = msg;
        StatusColor   = color;
        HasStatus     = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
