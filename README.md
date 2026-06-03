# 📚 EscolaSync

App Android (.NET MAUI) que move fotos do álbum **Escola** para uma pasta **Escola** no Google Drive e, após upload confirmado, remove as fotos do celular.

---

## 📱 Preview visual

| Arquivo | Descrição |
|---|---|
| [docs/preview.html](docs/preview.html) | Fluxo completo do app: tela inicial, conta conectada, envio em progresso e resultado final |
| [docs/auth-flow.html](docs/auth-flow.html) | Fluxo de autenticação OAuth2: do botão "Entrar com Google" até o retorno ao app autenticado |

> Abra os arquivos `.html` diretamente no browser para visualizar o mockup interativo.

---

## 🔧 Pré-requisitos

- Visual Studio 2022 com workload **Mobile development with .NET (MAUI)**
- .NET 10 SDK
- Android SDK API 36 (Android 16) — target; mínimo suportado: API 26 (Android 8)
- Conta no [Google Cloud Console](https://console.cloud.google.com)

---

## 1. Configurar o Google Cloud Console

### 1.1 Criar projeto
1. Acesse [console.cloud.google.com](https://console.cloud.google.com)
2. Clique em **New Project** → dê o nome `EscolaSync`

### 1.2 Ativar a API do Drive
1. No menu lateral: **APIs & Services → Library**
2. Busque **Google Drive API** → clique em **Enable**

### 1.3 Criar credenciais OAuth2
1. **APIs & Services → Credentials → Create Credentials → OAuth 2.0 Client ID**
2. Application type: **Android**
3. Package name: `com.escolasync.app`
4. SHA-1: gere com o comando abaixo:

```bash
# Debug keystore (para desenvolvimento)
keytool -keystore ~/.android/debug.keystore \
        -list -v -alias androiddebugkey \
        -storepass android -keypass android
```

5. Clique em **Create** — anote o **Client ID** gerado

> ⚠️ Para tipo Android, o Client Secret não é usado diretamente.  
> Mas você também precisará criar um OAuth Client ID do tipo **Web application** para usar com `GoogleWebAuthorizationBroker` no fluxo de desktop/installed app.

### 1.4 Configurar a tela de consentimento
1. **APIs & Services → OAuth consent screen**
2. User type: **External**
3. Preencha: App name, email de suporte
4. Scopes: adicione `../auth/drive` e `../auth/drive.file`
5. Test users: adicione o e-mail que usará para testar

---

## 2. Configurar o projeto

### 2.1 Inserir o Client ID no código

Abra `Services/GoogleAuthService.cs` e substitua:

```csharp
private const string ClientId     = "SEU_CLIENT_ID.apps.googleusercontent.com";
private const string ClientSecret = "SEU_CLIENT_SECRET";
```

> Use o Client ID do tipo **Web application** (não o Android) para o broker funcionar corretamente no MAUI.

### 2.2 Atualizar o AndroidManifest

Em `Platforms/Android/AndroidManifest.xml`, substitua:

```xml
<data android:scheme="com.googleusercontent.apps.SEU_CLIENT_ID" />
```

pelo seu Client ID **sem** o sufixo `.apps.googleusercontent.com`:

```xml
<data android:scheme="com.googleusercontent.apps.123456789-abcdef" />
```

---

## 3. Build e execução

```bash
# Restaurar pacotes
dotnet restore

# Build para Android (debug)
dotnet build -f net10.0-android -c Debug

# Rodar direto no device físico conectado via USB
dotnet run -f net10.0-android -c Debug

# Se houver mais de um device conectado, especifique o serial (veja com: adb devices)
dotnet run -f net10.0-android -c Debug -p:AndroidAttachedDeviceSerial=SEU_SERIAL

# Publicar APK de release (sideload — sem Play Store)
dotnet publish -f net10.0-android -c Release \
  -p:AndroidSigningKeyStore=sua_chave.keystore \
  -p:AndroidSigningKeyAlias=alias \
  -p:AndroidSigningKeyPass=senha \
  -p:AndroidSigningStorePass=senha
# APK gerado em: bin/Release/net10.0-android/publish/
```

**Pré-requisito para o device físico:** ative **Depuração USB** em Configurações → Opções do desenvolvedor, conecte o cabo e aceite o popup no celular. Verifique com `adb devices` se o device aparece como `device` (não `unauthorized`).

Ou use o Visual Studio: selecione seu celular no seletor de device na barra superior → **F5** para debug.

---

## 4. Fluxo de uso

1. Abra o app → toque em **Entrar com Google**
2. O browser se abre — faça login com **qualquer conta Google** (não precisa estar cadastrada no celular)
3. Autorize o acesso ao Drive → volte ao app
4. Selecione o álbum de origem (padrão: **Escola**)
5. Toque em **📤 Enviar Agora**
6. O app envia todas as fotos para `Google Drive → Escola/`
7. Cada foto removida do celular **somente após** confirmação de upload

---

## 5. Estrutura do projeto

```
EscolaSync/
├── EscolaSync.csproj
├── MauiProgram.cs              ← DI e configuração do app
├── App.xaml / .cs              ← Application root (usa CreateWindow — API atual .NET 10)
├── MainPage.xaml / .cs         ← Tela principal
├── MainViewModel.cs            ← Lógica de negócio + bindings
├── Converters/
│   └── ValueConverters.cs      ← IntToDouble, StringToColor
├── Models/
│   ├── PhotoItem.cs            ← Representa uma foto
│   └── SyncResult.cs           ← Resultado da sincronização
├── Services/
│   ├── GoogleAuthService.cs    ← OAuth2 via browser (sem usar contas do SO)
│   ├── DriveUploadService.cs   ← Upload + criação de pasta no Drive
│   └── MediaStoreService.cs    ← Listagem e deleção via MediaStore
├── Platforms/Android/
│   ├── MainActivity.cs         ← Entry point + OnActivityResult
│   ├── AndroidManifest.xml     ← Permissões
│   └── Resources/values/
│       ├── styles.xml
│       └── colors.xml
└── docs/
    ├── preview.html            ← Mockup visual do app (4 estados)
    └── auth-flow.html          ← Mockup do fluxo OAuth2 (5 passos)
```

---

## 6. Permissões utilizadas

| Permissão | Motivo |
|---|---|
| `READ_MEDIA_IMAGES` | Ler fotos (Android 13+) |
| `READ_EXTERNAL_STORAGE` | Ler fotos (Android ≤ 12) |
| `WRITE_EXTERNAL_STORAGE` | Deletar fotos (Android ≤ 9) |
| `MANAGE_MEDIA` | Deletar fotos de outros apps (Android 11+) |
| `INTERNET` | Upload para o Drive |

---

## 7. Observações importantes

- **Duplicatas**: se a foto já existir na pasta Drive com o mesmo nome, o upload é pulado e a foto local é removida normalmente.
- **Android 11+**: a deleção exibe um diálogo de confirmação do sistema operacional — isso é obrigatório pelo Android. O diálogo aparece **uma única vez para o lote inteiro** (não foto por foto).
- **MANAGE_MEDIA**: necessária porque as fotos foram criadas pela câmera (outro app). Sem ela, o Android 11+ bloqueia a deleção com `RecoverableSecurityException`. Esta permissão é aceita pela Play Store, diferente de `MANAGE_EXTERNAL_STORAGE` que é rejeitada.
- **Token**: após o primeiro login, o token é salvo em `AppDataDirectory/EscolaSync_Token`. Próximas aberturas do app não exigem novo login.
- **Conta Drive**: completamente independente das contas Google configuradas no celular.
- **App.xaml.cs**: usa `CreateWindow()` em vez de `MainPage` (obsoleto no MAUI .NET 10).
- **Pacotes NuGet**: `Microsoft.Maui.Controls 10.0.0`, `Google.Apis.Drive.v3 1.68.0.3568`, `Google.Apis.Auth 1.68.0`.
