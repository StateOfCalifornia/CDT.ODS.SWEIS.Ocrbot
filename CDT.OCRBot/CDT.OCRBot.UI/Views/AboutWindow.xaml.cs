using System.Reflection;
using System.Windows;

namespace CDT.OCRBot.UI.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadSystemInformation();
        }

        private void LoadSystemInformation()
        {
            // Get version from assembly
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            txtVersion.Text = $"Version: {version?.ToString() ?? "1.0.0"}";
            txtCopyright.Text = $"Copyright \u00A9 {DateTime.Now.Year} (AGPL).";
        
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}




