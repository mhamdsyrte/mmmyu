using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;

namespace FileManagerPro;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestStoragePermissions();
    }

    void RequestStoragePermissions()
    {
        // Android 11+ (API 30+) needs the special "All files access" permission
        // for a real file manager to browse the whole storage, same as apps
        // like Total Commander / MiXplorer.
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            if (!Android.OS.Environment.IsExternalStorageManager)
            {
                try
                {
                    var intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission);
                    intent.SetData(Android.Net.Uri.Parse($"package:{PackageName}"));
                    StartActivity(intent);
                }
                catch
                {
                    var intent = new Intent(Settings.ActionManageAllFilesAccessPermission);
                    StartActivity(intent);
                }
            }
        }
        else
        {
            ActivityCompat.RequestPermissions(this, new[]
            {
                Android.Manifest.Permission.ReadExternalStorage,
                Android.Manifest.Permission.WriteExternalStorage
            }, 0);
        }
    }
}
