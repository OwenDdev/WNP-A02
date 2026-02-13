using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WDFClient
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

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            PlayBtn.Visibility = Visibility.Hidden;
            QuitBtn.Visibility = Visibility.Hidden;
            GameLabel.Visibility = Visibility.Hidden;


            LoginBox.Visibility = Visibility.Visible;
            LoginBtn.Visibility = Visibility.Visible;
            Loginlabel.Visibility = Visibility.Visible;
            BackBtn.Visibility = Visibility.Visible;

        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            GameWindow GW = new GameWindow();
            GW.Show();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            PlayBtn.Visibility = Visibility.Visible;
            QuitBtn.Visibility = Visibility.Visible;
            GameLabel.Visibility = Visibility.Visible;


            LoginBox.Visibility = Visibility.Hidden;
            LoginBtn.Visibility = Visibility.Hidden;
            Loginlabel.Visibility = Visibility.Hidden;
            BackBtn.Visibility = Visibility.Hidden;
        }
    }
}