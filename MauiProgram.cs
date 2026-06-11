using EscolaSync.Services;
using Microsoft.Extensions.Logging;

namespace EscolaSync;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Android.Util.Log.Debug("ES_BOOT", "01 MauiProgram.CreateMauiApp() chamado");

        var builder = MauiApp.CreateBuilder();
        Android.Util.Log.Debug("ES_BOOT", "02 CreateBuilder() OK");

        try
        {
            builder
                .UseMaui()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            Android.Util.Log.Debug("ES_BOOT", "03 UseMaui() + fontes OK");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"03 ERRO UseMaui: {ex}");
        }

        try
        {
            builder.Services.AddSingleton<GoogleAuthService>();
            Android.Util.Log.Debug("ES_BOOT", "04 GoogleAuthService registrado");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"04 ERRO GoogleAuthService: {ex}");
        }

        try
        {
            builder.Services.AddSingleton<DriveUploadService>();
            Android.Util.Log.Debug("ES_BOOT", "05 DriveUploadService registrado");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"05 ERRO DriveUploadService: {ex}");
        }

        try
        {
            builder.Services.AddSingleton<MediaStoreService>();
            Android.Util.Log.Debug("ES_BOOT", "06 MediaStoreService registrado");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"06 ERRO MediaStoreService: {ex}");
        }

        try
        {
            builder.Services.AddSingleton<MainViewModel>();
            Android.Util.Log.Debug("ES_BOOT", "07 MainViewModel registrado");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"07 ERRO MainViewModel: {ex}");
        }

        try
        {
            builder.Services.AddSingleton<MainPage>();
            Android.Util.Log.Debug("ES_BOOT", "08 MainPage registrado");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"08 ERRO MainPage: {ex}");
        }

        try
        {
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            Android.Util.Log.Debug("ES_BOOT", "09 Logging configurado");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"09 ERRO Logging: {ex}");
        }

        // Captura exceções não tratadas
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Android.Util.Log.Error("ES_BOOT", $"UNHANDLED: {ex?.GetType().Name}: {ex?.Message}");
            Android.Util.Log.Error("ES_BOOT", $"STACK: {ex?.StackTrace}");
        };
        Android.Util.Log.Debug("ES_BOOT", "10 UnhandledException handler registrado");

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Android.Util.Log.Error("ES_BOOT", $"UNOBSERVED_TASK: {e.Exception?.Message}");
            Android.Util.Log.Error("ES_BOOT", $"STACK: {e.Exception?.StackTrace}");
            e.SetObserved();
        };
        Android.Util.Log.Debug("ES_BOOT", "11 TaskScheduler handler registrado");

        MauiApp app;
        try
        {
            Android.Util.Log.Debug("ES_BOOT", "12 Chamando builder.Build()...");
            app = builder.Build();
            Android.Util.Log.Debug("ES_BOOT", "13 builder.Build() OK");
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("ES_BOOT", $"13 ERRO Build: {ex.GetType().Name}: {ex.Message}");
            Android.Util.Log.Error("ES_BOOT", $"STACK: {ex.StackTrace}");
            throw;
        }

        Android.Util.Log.Debug("ES_BOOT", "14 CreateMauiApp() retornando app");
        return app;
    }
}
