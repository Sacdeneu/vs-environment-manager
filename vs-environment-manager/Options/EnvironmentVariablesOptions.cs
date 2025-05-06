using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace vs_environment_manager.Options
{
    public class EnvironmentVariablesOptions : DialogPage
    {
        private string _storagePath = "default_path";

        [Category("General")]
        [DisplayName("Storage Path")]
        [Description("Path where environment variables are stored.")]
        public string StoragePath
        {
            get { return _storagePath; }
            set { _storagePath = value; }
        }
    }
}
