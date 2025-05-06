using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;

namespace vs_environment_manager
{
    [VisualStudioContribution]
    public class EnvironmentManagerToolWindow : ToolWindow
    {
        public override ToolWindowConfiguration Configuration => new()
        {
            Title = "Environment Manager"
        };

        public override VisualElementContent GetContent()
            => new WpfToolWindowContent(new ToolWindows.EnvironmentManagerToolWindowControl());
    }

}
