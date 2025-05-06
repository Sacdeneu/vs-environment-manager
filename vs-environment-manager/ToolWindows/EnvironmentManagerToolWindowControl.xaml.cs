using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using vs_environment_manager.Models;

namespace vs_environment_manager.ToolWindows
{
    /// <summary>
    /// Logique d'interaction pour EnvironmentManagerToolWindowControl.xaml
    /// </summary>

    public partial class EnvironmentManagerToolWindowControl : UserControl
    {
        private ObservableCollection<EnvironmentVariable> variables = new();

        public EnvironmentManagerToolWindowControl()
        {
            InitializeComponent();
            VariablesListView.ItemsSource = variables;

            // Pour tester, ajoute quelques variables fictives :
            variables.Add(new EnvironmentVariable { Name = "var 1", Value = "value 1" });
            variables.Add(new EnvironmentVariable { Name = "var 2", Value = "value 2" });
            variables.Add(new EnvironmentVariable { Name = "var 3", Value = "value 3" });
        }

        private void AddVariable_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewVarName.Text))
            {
                variables.Add(new EnvironmentVariable { Name = NewVarName.Text, Value = NewVarValue.Text });
                NewVarName.Clear();
                NewVarValue.Clear();
            }
        }
    }

}
