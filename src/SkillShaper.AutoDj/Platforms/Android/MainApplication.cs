using Android.App;
using Android.Runtime;
using Android.Util;

namespace SkillShaper.AutoDj;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
        {
            Log.Error("SkillShaper.AutoDj", $"Unhandled Android exception: {args.Exception}");
        };
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
