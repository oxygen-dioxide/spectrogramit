using SpectrogramIt.ViewModels;

namespace SpectrogramIt.Views;

public partial class WelcomePage : ContentPage
{
    private readonly WelcomeViewModel _viewModel;

    public WelcomePage(RecentFilesStore store)
    {
        InitializeComponent();
        _viewModel = new WelcomeViewModel(store);
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.RefreshRecent();
    }
}