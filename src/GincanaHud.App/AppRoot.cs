using GincanaHud.App.Services;

namespace GincanaHud.App;

/// <summary>
/// Raíz sin Shell. Un solo NavigationPage envuelve el TabbedPage
/// (evita NavigationPage anidados que complican fragmentos en Android).
/// </summary>
public sealed class AppRoot : TabbedPage
{
	public AppRoot(
		JoinPage joinPage,
		HudPage hudPage,
		IAppNavigator navigator,
		IJoinSessionStore session,
		IGameplayLauncher launcher)
	{
		BarBackgroundColor = Color.FromArgb("#0B1218");
		BarTextColor = Color.FromArgb("#7CFFB2");
		SelectedTabColor = Color.FromArgb("#7CFFB2");
		UnselectedTabColor = Color.FromArgb("#9AABBC");
		BackgroundColor = Color.FromArgb("#0B1218");

		joinPage.Title = "Unirse";
		hudPage.Title = "HUD";

		NavigationPage.SetHasNavigationBar(hudPage, false);

		Children.Add(joinPage);
		Children.Add(hudPage);

		if (navigator is AppNavigator concrete)
			concrete.Attach(this);

		// Sesión previa: HUD + arranque automático (un toque menos en evento).
		if (session.Current is not null)
		{
			launcher.RequestAutoStart();
			CurrentPage = hudPage;
		}
	}
}
