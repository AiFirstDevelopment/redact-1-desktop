using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Redact1.ViewModels;

namespace Redact1.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        public void Initialize()
        {
            var viewModel = App.Services.GetRequiredService<SettingsViewModel>();
            DataContext = viewModel;
        }
    }
}
