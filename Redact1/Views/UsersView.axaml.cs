using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Redact1.ViewModels;

namespace Redact1.Views
{
    public partial class UsersView : UserControl
    {
        private UsersViewModel? _viewModel;

        public UsersView()
        {
            InitializeComponent();

            EditPasswordBox.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == "Text" && _viewModel != null)
                {
                    _viewModel.EditPassword = EditPasswordBox.Text ?? string.Empty;
                }
            };
        }

        public void Initialize()
        {
            _viewModel = App.Services.GetRequiredService<UsersViewModel>();
            DataContext = _viewModel;
            _ = _viewModel.LoadUsersAsync();
        }
    }
}
