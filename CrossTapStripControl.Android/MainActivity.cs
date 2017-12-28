using Android.App;
using Android.Content.PM;
using Android.OS;
using FFImageLoading.Forms.Droid;

namespace CrossTapStripControl.Droid
{
    [Activity(Label = "CrossTapStripControl", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            Xamarin.Forms.Forms.SetFlags("FastRenderers_Experimental");

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);

            CachedImageRenderer.Init(enableFastRenderer: true);

            LoadApplication(new App());
        }
    }
}