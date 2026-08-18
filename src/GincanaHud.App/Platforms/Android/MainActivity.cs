using Android.App;
using Android.Content.PM;
using Android.OS;

namespace GincanaHud.App;

[Activity(
	Theme = "@style/Maui.SplashTheme",
	MainLauncher = true,
	LaunchMode = LaunchMode.SingleTop,
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
		// No restaurar fragmentos: tras un deploy incremental los R.id del APK
		// y los que Android tenía en savedInstanceState no coinciden
		// (el log enseña un nombre aleatorio: jumpToEnd, italic, labeled…).
		base.OnCreate(null);
	}
}
