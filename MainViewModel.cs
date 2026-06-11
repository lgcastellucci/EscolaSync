using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using EscolaSync.Models;
using EscolaSync.Services;
using Microsoft.Maui.Graphics;

namespace EscolaSync;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly GoogleAuthService _auth;
    private readonly DriveUploadService _drive;
    private readonly MediaStoreService _media;

    public event Action? LogAdded;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    // ── Auth status ──────────────────────────────────────────
    private string _authStatusText = "Não autenticado";
    public string AuthStatusText
    {
        get => _authStatusText;
        set { _authStatusText = value; OnPropertyChanged(); }
    }

    private Color _authStatusColor = Color.FromArgb("#7F1D1D");
    public Color AuthStatusColor
    {
        get => _authStatusColor;
        set { _authStatusColor = value; OnPropertyChanged(); }
    }

    private Color _authDotColor = Color.FromArgb("#EF4444");
    public Color AuthDotColor
    {
        get => _authDotColor;
        set { _authDotColor = value; OnPropertyChanged(); }
    }

    private double _progress = 0;
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    private string _progressText = "Aguardando...";
    public string ProgressText
    {
        get => _progressText;
        set { _progressText = value; OnPropertyChanged(); }
    }

    private bool _canSync = false;
    public bool CanSync
    {
        get => _canSync;
        set { _canSync = value; OnPropertyChanged(); }
    }

    public ICommand AuthCommand { get; }
    public ICommand SyncCommand { get; }

    public MainViewModel(GoogleAuthService auth, DriveUploadService drive, MediaStoreService media)
    {
        Android.Util.Log.Debug("ES_BOOT", "40 MainViewModel() construtor iniciado");

        try
        {
            _auth = auth;
            Android.Util.Log.Debug("ES_BOOT", "41 GoogleAuthService injetado");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"41 ERRO auth inject: {ex.Message}");
            throw;
        }

        try
        {
            _drive = drive;
            Android.Util.Log.Debug("ES_BOOT", "42 DriveUploadService injetado");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"42 ERRO drive inject: {ex.Message}");
            throw;
        }

        try
        {
            _media = media;
            Android.Util.Log.Debug("ES_BOOT", "43 MediaStoreService injetado");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"43 ERRO media inject: {ex.Message}");
            throw;
        }

        try
        {
            AuthCommand = new Command(async () => await AuthenticateAsync());
            SyncCommand = new Command(async () => await SyncAsync());
            Android.Util.Log.Debug("ES_BOOT", "44 Commands criados OK");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"44 ERRO Commands: {ex.Message}");
            throw;
        }

        try
        {
            Log(LogEntry.Info("App iniciado"));
            Log(LogEntry.Info($"Versão: {AppInfo.VersionString} build {AppInfo.BuildString}"));
            Log(LogEntry.Info($"SO: Android {Android.OS.Build.VERSION.Release} (SDK {Android.OS.Build.VERSION.SdkInt})"));
            Log(LogEntry.Info($"Dispositivo: {Android.OS.Build.Manufacturer} {Android.OS.Build.Model}"));
            Android.Util.Log.Debug("ES_BOOT", "45 Log inicial OK");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"45 ERRO log inicial: {ex.Message}");
        }

        Android.Util.Log.Debug("ES_BOOT", "46 MainViewModel() construtor concluído");
    }

    private void Log(LogEntry entry)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogEntries.Add(entry);
            LogAdded?.Invoke();
        });
    }

    public void InitializeAfterPermissions()
    {
        Android.Util.Log.Debug("ES_BOOT", "50 InitializeAfterPermissions() chamado");
        Log(LogEntry.Step("Inicializando..."));

        Task.Run(async () =>
        {
            try
            {
                Android.Util.Log.Debug("ES_BOOT", "51 Verificando permissão READ_MEDIA_IMAGES...");
                Log(LogEntry.Info("[P1] Verificando permissão READ_MEDIA_IMAGES..."));
                var permStatus = await Permissions.CheckStatusAsync<Permissions.Photos>();
                Log(permStatus == PermissionStatus.Granted
                    ? LogEntry.Ok($"[P1] Permissão fotos: {permStatus}")
                    : LogEntry.Error($"[P1] Permissão fotos: {permStatus} — necessário conceder"));
                Android.Util.Log.Debug("ES_BOOT", $"51 Permissão fotos: {permStatus}");

                if (permStatus != PermissionStatus.Granted)
                {
                    Log(LogEntry.Step("[P1] Solicitando permissão..."));
                    permStatus = await Permissions.RequestAsync<Permissions.Photos>();
                    Log(permStatus == PermissionStatus.Granted
                        ? LogEntry.Ok($"[P1] Permissão concedida")
                        : LogEntry.Error($"[P1] Permissão negada — app não funcionará"));
                    Android.Util.Log.Debug("ES_BOOT", $"52 Após solicitar: {permStatus}");
                }

                Android.Util.Log.Debug("ES_BOOT", "53 Verificando token salvo...");
                Log(LogEntry.Info("[T1] Verificando token salvo..."));

                bool hasToken = false;
                try
                {
                    hasToken = _auth.HasSavedToken();
                    Android.Util.Log.Debug("ES_BOOT", $"54 HasSavedToken: {hasToken}");
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Error("ES_BOOT", $"54 ERRO HasSavedToken: {ex.Message}");
                    Log(LogEntry.Error($"[T1] Erro ao verificar token: {ex.Message}"));
                }

                if (hasToken)
                {
                    Log(LogEntry.Ok("[T1] Token encontrado — autenticado"));
                    SetAuthenticated();
                }
                else
                {
                    Log(LogEntry.Info("[T1] Sem token — clique em Autenticar Drive"));
                }

                Log(LogEntry.Ok("Inicialização concluída"));
                Android.Util.Log.Debug("ES_BOOT", "55 InitializeAfterPermissions concluído");
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("ES_BOOT", $"5X ERRO init: {ex.GetType().Name}: {ex.Message}");
                Android.Util.Log.Error("ES_BOOT", $"STACK: {ex.StackTrace}");
                Log(LogEntry.Error($"ERRO init: {ex.GetType().Name}: {ex.Message}"));
            }
        });
    }

    private async Task AuthenticateAsync()
    {
        Android.Util.Log.Debug("ES_BOOT", "60 AuthenticateAsync() iniciado");
        Log(LogEntry.Step("═══ Autenticação iniciada ═══"));
        SetProgress(0.05, "Iniciando auth...");

        try
        {
            Log(LogEntry.Info("[A1] Preparando OAuth2..."));
            Android.Util.Log.Debug("ES_BOOT", "61 Chamando _auth.AuthenticateAsync()");
            Log(LogEntry.Info("[A2] Abrindo browser Google..."));

            var ok = await _auth.AuthenticateAsync();
            Android.Util.Log.Debug("ES_BOOT", $"62 AuthenticateAsync retornou: {ok}");

            if (ok)
            {
                Log(LogEntry.Ok("[A3] OAuth2 concluído — token recebido"));
                Log(LogEntry.Info("[A4] Salvando token..."));
                SetAuthenticated();
                SetProgress(1.0, "Autenticado ✓");
                Log(LogEntry.Ok("═══ Autenticado com sucesso ═══"));
            }
            else
            {
                Log(LogEntry.Error("[A3] Auth cancelada ou falhou"));
                SetProgress(0, "Cancelado");
            }
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"6X ERRO auth: {ex.GetType().Name}: {ex.Message}");
            Android.Util.Log.Error("ES_BOOT", $"STACK: {ex.StackTrace}");
            Log(LogEntry.Error($"ERRO: {ex.GetType().Name}"));
            Log(LogEntry.Error(ex.Message));
            if (ex.InnerException != null)
                Log(LogEntry.Error($"Inner: {ex.InnerException.Message}"));
            SetProgress(0, "Erro na autenticação");
        }
    }

    private async Task SyncAsync()
    {
        Android.Util.Log.Debug("ES_BOOT", "70 SyncAsync() iniciado");
        Log(LogEntry.Step("═══════════════════════════════"));
        Log(LogEntry.Step("Sincronização iniciada"));
        CanSync = false;

        try
        {
            // PASSO 1
            Log(LogEntry.Step("[1/5] Listando fotos do álbum 'Escola'..."));
            Android.Util.Log.Debug("ES_BOOT", "71 Chamando GetPhotosFromAlbumAsync");
            SetProgress(0.1, "Buscando fotos...");

            var photos = await _media.GetPhotosFromAlbumAsync("Escola");
            Android.Util.Log.Debug("ES_BOOT", $"72 GetPhotosFromAlbumAsync: {photos.Count} fotos");

            if (photos.Count == 0)
            {
                Log(LogEntry.Error("[1/5] Nenhuma foto no álbum 'Escola'"));
                Log(LogEntry.Info("Verifique se o álbum existe com esse nome exato"));
                CanSync = true;
                SetProgress(0, "Sem fotos");
                return;
            }
            Log(LogEntry.Ok($"[1/5] {photos.Count} foto(s) encontrada(s)"));
            foreach (var p in photos.Take(5))
                Log(LogEntry.Info($"  • {p.FileName} ({p.Size / 1024}KB)"));
            if (photos.Count > 5)
                Log(LogEntry.Info($"  • ... e mais {photos.Count - 5}"));

            // PASSO 2
            Log(LogEntry.Step("[2/5] Verificando pasta 'Escola' no Drive..."));
            Android.Util.Log.Debug("ES_BOOT", "73 Chamando GetOrCreateFolderAsync");
            SetProgress(0.2, "Verificando Drive...");

            var folderId = await _drive.GetOrCreateFolderAsync("Escola");
            Android.Util.Log.Debug("ES_BOOT", $"74 FolderId: {folderId}");
            Log(LogEntry.Ok($"[2/5] Pasta OK — {folderId[..Math.Min(16, folderId.Length)]}..."));

            // PASSO 3
            Log(LogEntry.Step($"[3/5] Upload de {photos.Count} foto(s)..."));
            int done = 0, failed = 0;
            var toDelete = new List<(long id, string uri)>();

            foreach (var photo in photos)
            {
                SetProgress(0.2 + (0.6 * done / photos.Count), $"Upload {done + 1}/{photos.Count}");
                Log(LogEntry.Info($"  ▶ {photo.FileName}"));
                Android.Util.Log.Debug("ES_BOOT", $"75 Upload: {photo.FileName}");

                try
                {
                    var driveId = await _drive.UploadPhotoAsync(photo, folderId);
                    Android.Util.Log.Debug("ES_BOOT", $"76 Upload OK: {driveId[..8]}");
                    Log(LogEntry.Ok($"  ✓ {photo.FileName} → {driveId[..8]}..."));
                    toDelete.Add((photo.Id, photo.ContentUri));
                    done++;
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Error("ES_BOOT", $"76 ERRO upload {photo.FileName}: {ex.Message}");
                    Log(LogEntry.Error($"  ✗ {photo.FileName}: {ex.Message}"));
                    failed++;
                }
            }
            Log(LogEntry.Step($"[3/5] {done} OK, {failed} falhas"));

            // PASSO 4
            if (toDelete.Count > 0)
            {
                Log(LogEntry.Step($"[4/5] Deletando {toDelete.Count} foto(s)..."));
                Android.Util.Log.Debug("ES_BOOT", "77 Deletando fotos locais");
                SetProgress(0.85, "Limpando...");

                int deleted = 0;
                foreach (var (id, uri) in toDelete)
                {
                    try
                    {
                        await _media.DeletePhotoAsync(id, uri);
                        deleted++;
                        Android.Util.Log.Debug("ES_BOOT", $"78 Deletado ID {id}");
                    }
                    catch (Exception ex)
                    {
                        Android.Util.Log.Error("ES_BOOT", $"78 ERRO delete ID {id}: {ex.Message}");
                        Log(LogEntry.Error($"  Não deletou ID {id}: {ex.Message}"));
                    }
                }
                Log(LogEntry.Ok($"[4/5] {deleted}/{toDelete.Count} deletadas"));
            }

            // PASSO 5
            Android.Util.Log.Debug("ES_BOOT", "79 Sync concluído");
            Log(failed == 0
                ? LogEntry.Ok("[5/5] ═══ Tudo OK! ═══")
                : LogEntry.Error($"[5/5] Concluído com {failed} erro(s)"));
            SetProgress(1.0, $"{done} enviadas, {failed} erros");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"7X ERRO sync: {ex.GetType().Name}: {ex.Message}");
            Android.Util.Log.Error("ES_BOOT", $"STACK: {ex.StackTrace}");
            Log(LogEntry.Error($"ERRO: {ex.GetType().Name}"));
            Log(LogEntry.Error(ex.Message));
            if (ex.InnerException != null)
                Log(LogEntry.Error($"Inner: {ex.InnerException.Message}"));
            var stack = ex.StackTrace ?? "";
            Log(LogEntry.Error(stack[..Math.Min(300, stack.Length)]));
            SetProgress(0, "Erro");
        }
        finally
        {
            CanSync = true;
        }
    }

    private void SetAuthenticated()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AuthStatusText = "✓ Autenticado no Google Drive";
            AuthStatusColor = Color.FromArgb("#064E3B");
            AuthDotColor = Color.FromArgb("#34D399");
            CanSync = true;
        });
    }

    private void SetProgress(double val, string text)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Progress = val;
            ProgressText = text;
        });
    }

    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
