using System.Windows;

namespace VsEnvironmentManager
{
    public partial class AddVariableDialog : Window
    {
        public string VariableName { get; private set; }
        public string VariableValue { get; private set; }

        public AddVariableDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            VariableName = NameTextBox.Text;
            VariableValue = ValueTextBox.Text;
            DialogResult = true;
            Close();
        }
    }
}
