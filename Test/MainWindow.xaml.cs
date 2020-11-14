using System.Windows;
using System.Windows.Controls;

namespace Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var view = new View(new ViewOptions()
            {
                Html = txtInput.Text
            });
            txtInput.Text = view.Render();
        }

        private void txtInput_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
