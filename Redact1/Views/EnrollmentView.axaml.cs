using Avalonia.Controls;
using Avalonia.Input;
using Redact1.ViewModels;

namespace Redact1.Views
{
    public partial class EnrollmentView : UserControl
    {
        public EnrollmentView()
        {
            InitializeComponent();

            DepartmentCodeBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && DataContext is EnrollmentViewModel vm)
                {
                    vm.ConnectCommand.Execute(null);
                }
            };
        }
    }
}
