using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Widget;

namespace SkillShaper.AutoDj;

[Activity(Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize
                           | ConfigChanges.Orientation
                           | ConfigChanges.UiMode
                           | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize
                           | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        try
        {
            base.OnCreate(savedInstanceState);
        }
        catch (Exception ex)
        {
            Log.Error("SkillShaper.AutoDj", $"Startup crash in MainActivity.OnCreate: {ex}");

            var text = new TextView(this)
            {
                Text = $"Startup crash: {ex.GetType().Name}\n{ex.Message}\n\nCheck logcat tag: SkillShaper.AutoDj"
            };

            text.SetTextColor(Android.Graphics.Color.White);
            text.SetBackgroundColor(Android.Graphics.Color.ParseColor("#111318"));
            text.SetPadding(28, 40, 28, 28);
            SetContentView(text);
        }
    }
}
