using System.Windows.Controls;
using System.Windows.Media;

namespace VsEnvironmentManager
{
    public partial class EnvStatusBarControl : UserControl
    {
        public EnvStatusBarControl()
        {
            InitializeComponent();
        }

        public void SetStatus(int variableCount)
        {
            var addIcon = (DrawingImage)this.FindResource("AddVariableDrawingImage");
            var detectedIcon = (DrawingImage)this.FindResource("DetectedVariableDrawingImage");

            if (variableCount > 0)
            {
                StatusIcon.Source = detectedIcon;
                VarCountText.Text = variableCount.ToString();
                VarCountText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7EF37A"));
            }
            else
            {
                StatusIcon.Source = addIcon;
                VarCountText.Text = "";
                VarCountText.Foreground = Brushes.Gray;
            }
        }
    }
}
