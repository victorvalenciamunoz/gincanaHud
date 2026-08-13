namespace GincanaHud.App.Services;

public interface IAppNavigator
{
	void GoToHud();
	void GoToJoin();
}

/// <summary>Navegación entre pestañas del TabbedPage raíz.</summary>
public sealed class AppNavigator : IAppNavigator
{
	private TabbedPage? _tabs;

	public void Attach(TabbedPage tabs) => _tabs = tabs;

	public void GoToHud() => Select(1);

	public void GoToJoin() => Select(0);

	private void Select(int index)
	{
		var tabs = _tabs;
		if (tabs is null || index < 0 || index >= tabs.Children.Count)
			return;

		MainThread.BeginInvokeOnMainThread(() =>
		{
			if (tabs.Children.Count > index)
				tabs.CurrentPage = tabs.Children[index];
		});
	}
}
