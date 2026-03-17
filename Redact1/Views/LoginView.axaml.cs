using Avalonia.Controls;
using Avalonia.Interactivity;
using Redact1.ViewModels;

namespace Redact1.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();

            PasswordBox.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == "Text" && DataContext is LoginViewModel vm)
                {
                    vm.Password = PasswordBox.Text ?? string.Empty;
                }
            };

            LoginButton.Click += (s, e) =>
            {
                if (DataContext is LoginViewModel vm)
                {
                    vm.LoginCommand.Execute(null);
                }
            };
        }
    }
}
