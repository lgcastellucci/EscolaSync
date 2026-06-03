using Android.App;
using Android.Content.PM;
using Android.OS;

namespace EscolaSync.Platforms.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
    }

    /// <summary>
    /// Recebe resultado do CreateDeleteRequest do MediaStore (Android 11+).
    /// RequestCode 42 é usado pela MediaStoreService.
    /// </summary>
    protected override void OnActivityResult(int requestCode, Result resultCode, global::Android.Content.Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode == 42)
        {
            MainPage.OnDeleteResult((int)resultCode);
        }
    }
}
