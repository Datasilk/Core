using System.Linq;
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
            view.Show("is-block");
            view["insert-adj"] = view.Elements.Where(a => a.Name == "insert-adj").First().Vars["text"];
            var header = view.Child("header");
            var i = header.Fields.Where(a => a.Key == "page-list").First().Value[0];
            header["page-list"] = "This is a page list filtered by path: " + view.Elements[i].Vars["path"];
            txtInput.Text = view.Render();
            txtInput.Text += "\n\n" + view.GetBlock(view.Elements.Where(a => a.Name == "is-block").First());
            btnRender.Visibility = Visibility.Hidden;
        }

        private void txtInput_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
