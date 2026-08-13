using GincanaHud.App.ViewModels;

namespace GincanaHud.App;

public partial class MainPage : ContentPage
{
	public MainPage(SensorDebugViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
