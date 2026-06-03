using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace EscolaSync.Services;

/// <summary>
/// Autentica no Google Drive via OAuth2 com browser externo.
/// NÃO usa as contas Google cadastradas no celular.
/// </summary>
public class GoogleAuthService
{
    // ─────────────────────────────────────────────────────────────────────────
    // CONFIGURAÇÃO — preencha com seus dados do Google Cloud Console
    // ─────────────────────────────────────────────────────────────────────────
    private const string ClientId = "SEU_CLIENT_ID.apps.googleusercontent.com";
    private const string ClientSecret = "SEU_CLIENT_SECRET";
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly string[] Scopes = new[]
    {
        DriveService.Scope.Drive,
        DriveService.Scope.DriveFile
    };

    private const string TokenFolder = "EscolaSync_Token";

    private UserCredential? _credential;

    public bool IsAuthenticated => _credential != null;

    /// <summary>
    /// Inicia o fluxo OAuth2. Abre o browser para o usuário logar com
    /// qualquer conta Google (mesmo que não esteja no celular).
    /// </summary>
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            var secrets = new ClientSecrets
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret
            };

            // FileDataStore salva o token no armazenamento local do app
            var tokenStore = new FileDataStore(
                Path.Combine(FileSystem.AppDataDirectory, TokenFolder),
                fullPath: true);

            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                Scopes,
                user: "user",       // chave para armazenar o token
                CancellationToken.None,
                tokenStore);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AUTH] Erro: {ex.Message}");
            _credential = null;
            return false;
        }
    }

    /// <summary>
    /// Revoga o token salvo e limpa o estado de autenticação.
    /// </summary>
    public async Task LogoutAsync()
    {
        if (_credential != null)
        {
            await _credential.RevokeTokenAsync(CancellationToken.None);
            _credential = null;
        }

        // Apaga tokens salvos em disco
        var tokenPath = Path.Combine(FileSystem.AppDataDirectory, TokenFolder);
        if (Directory.Exists(tokenPath))
            Directory.Delete(tokenPath, recursive: true);
    }

    /// <summary>
    /// Retorna um DriveService autenticado pronto para uso.
    /// </summary>
    public DriveService GetDriveService()
    {
        if (_credential == null)
            throw new InvalidOperationException("Usuário não autenticado. Chame AuthenticateAsync primeiro.");

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = _credential,
            ApplicationName = "EscolaSync"
        });
    }

    /// <summary>
    /// Tenta restaurar uma sessão previamente salva sem abrir o browser.
    /// </summary>
    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var secrets = new ClientSecrets
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret
            };

            var tokenStore = new FileDataStore(
                Path.Combine(FileSystem.AppDataDirectory, TokenFolder),
                fullPath: true);

            // Tenta carregar token salvo; se não existir, retorna false
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                Scopes,
                user: "user",
                CancellationToken.None,
                tokenStore);

            // Se chegou aqui sem abrir browser, o token foi restaurado
            return _credential != null;
        }
        catch
        {
            _credential = null;
            return false;
        }
    }
}
