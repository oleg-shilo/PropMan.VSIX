using System.Windows;
using System.Windows.Controls;

namespace OlegShilo.PropMan
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : UserControl
    {
        public ConfigWindow()
        {
            InitializeComponent();
            TemplateData.Text = CSharpRefactor.FullPropTemplate;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            TemplateData.Text = CSharpRefactor.defaultFullPropTemplate;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            CSharpRefactor.FullPropTemplate = TemplateData.Text;
            // Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Close();
        }

        private void TemplateData_TextChanged(object sender, TextChangedEventArgs e)
        {
            Save.IsEnabled = (CSharpRefactor.FullPropTemplate != TemplateData.Text);
            Reset.IsEnabled = (CSharpRefactor.FullPropTemplate != TemplateData.Text);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}