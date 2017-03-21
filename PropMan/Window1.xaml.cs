using System.Windows;
using System.Windows.Input;

namespace Microsoft.AutoPropertyConverterVSX
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public enum Operation
        {
            Undefined,
            AutoFull,
            Encapsulate
        }

        public Operation SelectedOperation;

        public Window1()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            ChoiceBox.Focus();
            AutoVsFullItem.Focus();
            ChoiceBox.SelectedIndex = 0;
        }

        private void ChoiceBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            else if (e.Key == Key.Return)
            {
                SelectedOperation = (Operation)(ChoiceBox.SelectedIndex + 1);
                this.Close();
            }
        }

        private void ChoiceBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectedOperation = (Operation)(ChoiceBox.SelectedIndex + 1);
            this.Close();
        }
    }
}