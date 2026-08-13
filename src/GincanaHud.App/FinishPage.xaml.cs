using GincanaHud.App.ViewModels;

namespace GincanaHud.App;

public partial class FinishPage : ContentPage
{
	private readonly FinishViewModel _vm;

	public FinishPage(FinishViewModel vm)
	{
		InitializeComponent();
		_vm = vm;
		BindingContext = vm;
		vm.CloseModalAsync = async () =>
		{
			if (Navigation.ModalStack.Count > 0)
				await Navigation.PopModalAsync();
		};
	}

	public Task LoadAsync(Guid activityId, Guid userId, string activityTitle)
		=> _vm.LoadAsync(activityId, userId, activityTitle);
}
