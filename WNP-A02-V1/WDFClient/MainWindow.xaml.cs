using System.Net;
using System.Net.Sockets;
using System.Text;
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
        private TcpClient client;
        private NetworkStream stream;

        public MainWindow()
        {
            InitializeComponent();
            // temp
            Loaded += MainWindow_Loaded;
        }
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse("192.168.43.24"), 5000);
            stream = client.GetStream();
            await Send_message();

        }

        public async Task Send_message()
        {
            string message = "Hello from client!";
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
            Worddisplay.Content = response;
            // Console.WriteLine($"Server replied: {response}");

        }
    }
}