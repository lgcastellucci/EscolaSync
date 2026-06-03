using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using EscolaSync.Models;

// Alias explícito para evitar ambiguidade com System.IO.File
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace EscolaSync.Services;

/// <summary>
/// Gerencia upload de fotos para o Google Drive.
/// Cria a pasta "Escola" automaticamente se não existir.
/// </summary>
public class DriveUploadService
{
    private const string DriveFolderName = "Escola";

    private readonly GoogleAuthService _authService;
    private string? _cachedFolderId;

    public DriveUploadService(GoogleAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Retorna o ID da pasta "Escola" no Drive, criando-a se necessário.
    /// </summary>
    public async Task<string> GetOrCreateEscolaFolderAsync()
    {
        if (_cachedFolderId != null)
            return _cachedFolderId;

        var service = _authService.GetDriveService();

        var listRequest = service.Files.List();
        listRequest.Q = $"mimeType='application/vnd.google-apps.folder' " +
                        $"and name='{DriveFolderName}' " +
                        $"and trashed=false";
        listRequest.Fields = "files(id, name)";
        listRequest.Spaces = "drive";

        var result = await listRequest.ExecuteAsync();

        if (result.Files.Count > 0)
        {
            _cachedFolderId = result.Files[0].Id;
            return _cachedFolderId;
        }

        // Cria a pasta
        var folderMeta = new DriveFile
        {
            Name = DriveFolderName,
            MimeType = "application/vnd.google-apps.folder"
        };

        var createRequest = service.Files.Create(folderMeta);
        createRequest.Fields = "id";
        var folder = await createRequest.ExecuteAsync();

        _cachedFolderId = folder.Id;
        return _cachedFolderId;
    }

    /// <summary>
    /// Faz upload de uma foto para a pasta Escola no Drive.
    /// Retorna true se sucesso.
    /// </summary>
    public async Task<bool> UploadPhotoAsync(
        PhotoItem photo,
        IProgress<int>? progress = null)
    {
        try
        {
            var service = _authService.GetDriveService();
            var folderId = await GetOrCreateEscolaFolderAsync();

            // Verifica duplicata pelo nome
            var existsRequest = service.Files.List();
            existsRequest.Q = $"name='{EscapeQuery(photo.DisplayName)}' " +
                              $"and '{folderId}' in parents " +
                              $"and trashed=false";
            existsRequest.Fields = "files(id)";
            var existsResult = await existsRequest.ExecuteAsync();

            if (existsResult.Files.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[DRIVE] Já existe: {photo.DisplayName} — pulando upload");
                return true;
            }

            var fileMeta = new DriveFile
            {
                Name = photo.DisplayName,
                Parents = new List<string> { folderId }
            };

            using var stream = System.IO.File.OpenRead(photo.FilePath);

            var uploadRequest = service.Files.Create(fileMeta, stream, photo.MimeType);
            uploadRequest.Fields = "id, name, size";

            uploadRequest.ProgressChanged += (p) =>
            {
                if (p.Status == UploadStatus.Uploading && photo.SizeBytes > 0)
                {
                    int pct = (int)(p.BytesSent * 100 / photo.SizeBytes);
                    progress?.Report(pct);
                }
            };

            var uploadResult = await uploadRequest.UploadAsync();

            if (uploadResult.Status == UploadStatus.Completed)
            {
                System.Diagnostics.Debug.WriteLine($"[DRIVE] Upload OK: {photo.DisplayName}");
                return true;
            }

            System.Diagnostics.Debug.WriteLine($"[DRIVE] Falha: {uploadResult.Exception?.Message}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DRIVE] Exceção ao enviar {photo.DisplayName}: {ex.Message}");
            return false;
        }
    }

    public void ClearCache() => _cachedFolderId = null;

    private static string EscapeQuery(string name) => name.Replace("'", "\\'");
}
