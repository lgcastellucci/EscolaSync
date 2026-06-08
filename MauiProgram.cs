using Microsoft.Extensions.Logging;
using EscolaSync.Services;

namespace EscolaSync;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Registrar serviços
        builder.Services.AddSingleton<GoogleAuthService>();
        builder.Services.AddSingleton<DriveUploadService>();
        builder.Services.AddSingleton<MediaStoreService>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MainViewModel>();

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif


        // Captura exceções não tratadas
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine($"[FATAL] UnhandledException: {ex}");
            Android.Util.Log.Error("EscolaSync", $"UnhandledException: {ex}");
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"[FATAL] UnobservedTask: {args.Exception}");
            Android.Util.Log.Error("EscolaSync", $"UnobservedTask: {args.Exception}");
            args.SetObserved();
        };

        return builder.Build();
    }


}
