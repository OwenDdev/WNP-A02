using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;

namespace WDFClient
{
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;

        private Random rnd = new Random();
        private Label[] guessLabels;

        private int TotalFound = 0;

        public GameWindow()
        {
            InitializeComponent();
            // temp
            Loaded += GameWindow_Loaded;

            guessLabels = new Label[] { Guess1, Guess2, Guess3, Guess4, Guess5, Guess6, Guess7, Guess8, Guess9, Guess10 };
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse("10.144.110.236"), 5000);
            stream = client.GetStream();
            string message = "Hello from client!";
            await Send_message(message);

        }

        public async Task Send_message(string message)
        {
            //string message = "Hello from client!";
            byte[] data = Encoding.UTF8.GetBytes(message);

            await stream.WriteAsync(data, 0, data.Length);
            Console.WriteLine("Message sent");

            // We need an input buffer.
            // Then we can read the data.
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            // The input data is in bytes so we must convert it to
            //    a string to use it.
            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            string[] info;
            info = response.Split('|');
            if (info[1] == "Found")
            {
                FoundBox.Text = info[0];

                displayFound(TotalFound, info[0]);
            }
            else if (info[2] == "Jumble")
            {
                int.TryParse(info[1], out TotalFound);
                if (TotalFound >= 10)
                {
                    GuessSpace10.Visibility = Visibility.Visible;
                    GuessSpace10.IsEnabled = true;

                }
                Worddisplay.Content = info[0];
            }

            // Console.WriteLine($"Server replied: {response}");

        }

        private void displayFound(int totalFound, string wordFound)
        {
            foreach (Label gl in guessLabels)
            {
                if (gl.Content.ToString() == wordFound)
                {
                    return;
                }
            }


            Label selectedLabel;
            bool filled = false;

            while (!filled)
            {
                //Random rnd = new Random();
                int labelnumber = rnd.Next(0, totalFound);

                selectedLabel = guessLabels[labelnumber];
                if (selectedLabel.Content.ToString() == "???")
                {
                    selectedLabel.Content = wordFound;
                    filled = true;
                }
            }
        }

        private async void Guess_Click(object sender, RoutedEventArgs e)
        {
            string guess = GuessBox.Text;
            await Send_message(guess);
        }

    }
}
