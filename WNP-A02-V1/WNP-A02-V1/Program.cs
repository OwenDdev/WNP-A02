using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WNPA02V1
{
    class WNPA02V1
    {
        
        //cna delete this line if don't need it-che ping
        //private static string[] data;
        public static async Task Main()
        {
            //also could delete this line if don't need it-che ping 
            /*string[] response;
            response = ReadFileAsync();*/

            // Create the TcpListener and start it
            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            listener.Start();
            Console.WriteLine("Server started on port 5000");

            
            // Main loop to handle client requests.
            // The server accepts a client request, then passes control
            //    to the code that does the work.

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
 
                Task task = HandleClientAsync(client);
            }
        }

        // The client handler handles the request
        private static async Task HandleClientAsync(TcpClient client)
        {
            Console.WriteLine("Client connected");
            
            //every client will get thier own random data without restart server 
            string[] GameData = ReadFileAsync();

            // The using statement should be used to make communications
            //   easier.
            using NetworkStream stream = client.GetStream();

            // Need a buffer for the input
            byte[] buffer = new byte[1024];


            while (true)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    // Remember, the data over TCP/IP is in bytes, so the stream
                    //    must be converted to a string so you can use it.
                    //string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Console.WriteLine($"Received: {message}");

                    int code = determine_response(message, GameData);

                    // The data must be converted to a byte array to be used
                    //    by TCP/IP

                    string response;
                    if (code == 0)
                    {
                        response = $"{GameData[code]}|{GameData[1]}|Jumble";
                    }
                    else if(code == -1)
                    {
                        response = "Wrong|TryAgain";
                    }
                    else
                    {
                        response = $"{GameData[code]}|Found";
                    }
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    Console.WriteLine("Response sent");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Clinet Error: {ex.Message}");
                    break;
                }
            }
            //later on we cn specify which client disconnected here 
            Console.WriteLine("Client disconnected");
            client.Close();
        }

        

        private static int determine_response(string message, string[] Gamedata)
        {
            string clientMessage = message.Trim().ToLower();
            if (clientMessage.Contains("hello from client"))
            {
                return 0;
            }
                for (int i = 2; i < Gamedata.Length; i++)
                {
                    if (message == Gamedata[i] ) {
                        return i;
                    }
                }
             return -1;
        }


        private static string[] ReadFileAsync()
        {
            string line;
            string gamefile = RandomGameData();
            string filepath = $@"C:\SRC\WNP-A02\WNP-A02-V1\WNP-A02-V1\GameData\{gamefile}";
            // string[] data = new string[16];

            using StreamReader sr = new StreamReader(filepath);

            string Jumble = sr.ReadLine();
            string Arraysize = sr.ReadLine();

            int size = int.Parse(Arraysize) + 2;
            //create local array passed by gamedata 
            string[] Localdata = new string[size];

            Localdata = new string[size];

            // Store first two lines
            Localdata[0] = Jumble;
            Localdata[1] = Arraysize;


            int i = 2;
            //change hardcode 16 to data.Length for accpet other gamefile content
            while ((line = sr.ReadLine()) != null && i < Localdata.Length)
            {
                Localdata[i] = line;
                i++;  // move to next index for next line
            }

            //close the file
            sr.Close();
            //Console.ReadLine();
            Console.WriteLine(Localdata[0]);
            return Localdata;
        }

        //Function:RandomGameData
        //Dscription: chose one gamedata randomly
        //Return: string, Game filename
        private static string RandomGameData()
        {
            Random random = new Random();
            int GameFileNumber = random.Next(1, 5);
            string GameFileName = $"data{GameFileNumber.ToString("D2")}.txt";
            return GameFileName;
        }
    }
}
