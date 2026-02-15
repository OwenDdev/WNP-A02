using System.Windows;
using System.Text.RegularExpressions;
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
            string PlayerName = LoginBox.Text.Trim();
            if (string.IsNullOrEmpty(PlayerName))
            {
                MessageBox.Show("Player name cannot be empty, please try again");
                return;
            }
            if(PlayerName.Length > 8)
            {
                MessageBox.Show("Only enter 8 characters for playername, please try again");
                return;
            }
            if (!Regex.IsMatch(PlayerName, "^[a-zA-Z0-9]*$"))
            {
                MessageBox.Show("Invalid name, only letter and number");
                return;
            }
            GameWindow GW = new GameWindow();
            GW.Show();
            Close();
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