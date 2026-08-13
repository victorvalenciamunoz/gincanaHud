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
		// Evita restaurar fragmentos con IDs de layout corruptos/colisionados
		// (síntoma: No view found for id …/jumpToEnd = navigationlayout_content).
		base.OnCreate(null);
	}
}
