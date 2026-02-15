/*
* FILE          : Program.cs
* PROJECT       : A02 - TCPIP 
* PROGRAMMER    : Najaf Ali, Che-Ping Chien, Precious Orewen, Yi-Chen Tsai
* FIRST VERSION : 2026-02-05
* DESCRIPTION   :
*      This file contains the server-side implementation for the Word Jumble Game.
*      It implements a multi-client TCP/IP server that manages game sessions, word validation,
*      and game state for multiple concurrent players using asynchronous Task-based operations.
*      
*      Server Architecture:
*      - TCP listener on port 5000 accepting multiple client connections
*      - Fire-and-forget client handling pattern for concurrent game sessions
*      - Asynchronous I/O operations for efficient resource utilization
*      - Persistent connections that remain alive after game over for replay options
*      
*      Game Management:
*      - Random game file selection from 4 data files (data01.txt through data04.txt)
*      - Each file contains: 30-character jumbled string, word count, and word list
*      - Per-client game state tracking (found words, wrong guesses, hints used)
*      - Protocol implementation for client-server communication
*      
*      Enhanced Game Logic:
*      - Word validation against loaded game data
*      - Duplicate guess detection and prevention
*      - Wrong guess counting with maximum limit (3 strikes)
*      - Hint system providing first/last letter clues for unfound words
*      - Win/loss condition checking and appropriate responses
*      - TIME_UP message handling for timer expiration
*      - Game reset functionality for "play again" requests
*      - Game state validation (checks if game is active before processing)
*      - Connection persistence after game over for replay without reconnecting
*      
*      The server maintains complete game state for each connected client
*      independently, allowing multiple players to play simultaneously with
*      potentially different game files and progress. Connections remain alive
*      after game completion to facilitate immediate replay without reconnection.
*/

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WNPA02V1
{
    class WNPA02V1
    {
        //
        // METHOD        : Main
        // DESCRIPTION   : Entry point for the server application. Initializes TCP listener
        //                 on port 5000 and continuously accepts client connections.
        //                 Each client is handled asynchronously using fire-and-forget pattern.
        // RETURNS       : Task - Asynchronous operation
        //
        public static async Task Main()
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            listener.Start();
            Console.WriteLine("Server started on port 5000");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client); // Fire and forget - allows concurrent clients
            }
        }

        //
        // METHOD        : HandleClientAsync
        // DESCRIPTION   : Manages an individual client connection throughout their game session.
        //                 Handles message processing, game state tracking, and response generation.
        //                 Enhanced to support TIME_UP messages and maintain connection after game over.
        // PARAMETERS    : TcpClient client - The connected client instance
        // RETURNS       : Task - Asynchronous operation
        //
        private static async Task HandleClientAsync(TcpClient client)
        {
            Console.WriteLine("Client connected");
            string[] GameData = ReadFileAsync();

            using NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            Dictionary<string, bool> foundWords = new Dictionary<string, bool>();
            int wrongGuessCount = 0;
            const int MAX_WRONG_GUESSES = 3;
            int hintsRequested = 0;
            const int MAX_HINTS = 3;
            bool clientDisconnected = false;
            bool gameActive = true;

            while (!clientDisconnected)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        // Client disconnected gracefully
                        Console.WriteLine("Client disconnected gracefully");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received: {message}");

                    string response;
                    byte[] responseData;

                    // Handle client disconnect request
                    if (message == "DISCONNECT")
                    {
                        Console.WriteLine("Client requested disconnect");
                        clientDisconnected = true;
                        break;
                    }
                    // Handle new game request (play again)
                    else if (message == "NEW_GAME")
                    {
                        // Load new random game data
                        GameData = ReadFileAsync();
                        foundWords.Clear();
                        wrongGuessCount = 0;
                        hintsRequested = 0;
                        gameActive = true;
                        response = $"{GameData[0]}|{GameData[1]}|Jumble";
                        // Add all words to response for client to know the list
                        for (int i = 2; i < GameData.Length; i++)
                        {
                            response += $"|{GameData[i]}";
                        }
                        Console.WriteLine("New game started");
                    }
                    // Handle hint requests
                    else if (message == "REQUEST_HINT")
                    {
                        // Validate game is active before providing hint
                        if (!gameActive)
                        {
                            response = "HINT|Game is not active";
                        }
                        else
                        {
                            hintsRequested++;

                            // Check if max hints exceeded
                            if (hintsRequested > MAX_HINTS)
                            {
                                response = "HINT|No hints left!";
                            }
                            else
                            {
                                // Find an unfound word to give a hint
                                string hint = GetHint(GameData, foundWords);
                                response = $"HINT|{hint}";
                                Console.WriteLine($"Hint #{hintsRequested}/{MAX_HINTS} given");
                            }
                        }
                    }
                    // Handle time up notification from client
                    else if (message == "TIME_UP")
                    {
                        Console.WriteLine("Time's up reported by client");

                        // Get list of unfound words to send back to client
                        List<string> unfoundWords = new List<string>();
                        for (int i = 2; i < GameData.Length; i++)
                        {
                            if (!foundWords.ContainsKey(GameData[i]))
                            {
                                unfoundWords.Add(GameData[i]);
                            }
                        }

                        string unfoundList = string.Join(", ", unfoundWords);
                        response = $"GameOver|TimeIsUp|{unfoundList}";
                        gameActive = false;
                        // Don't break - keep connection alive for replay
                    }
                    else
                    {
                        // Handle guess messages (including initial hello)
                        if (!message.Contains("Hello from client"))
                        {
                            // Validate game is active before processing guesses
                            if (!gameActive)
                            {
                                response = "GameNotActive";
                            }
                            else
                            {
                                int code = determine_response(message, GameData, foundWords);

                                if (code == 0)
                                {
                                    response = $"{GameData[0]}|{GameData[1]}|Jumble";
                                    // Add all words to response
                                    for (int i = 2; i < GameData.Length; i++)
                                    {
                                        response += $"|{GameData[i]}";
                                    }
                                }
                                else if (code == -1)
                                {
                                    // Check if it's a duplicate (already found)
                                    bool isDuplicate = false;
                                    for (int i = 2; i < GameData.Length; i++)
                                    {
                                        if (message.Equals(GameData[i], StringComparison.OrdinalIgnoreCase) && foundWords.ContainsKey(GameData[i]))
                                        {
                                            isDuplicate = true;
                                            break;
                                        }
                                    }

                                    if (isDuplicate)
                                    {
                                        // Already submitted
                                        response = "Duplicate|AlreadyFound";
                                        Console.WriteLine("Duplicate word - already found");
                                    }
                                    else
                                    {
                                        // Wrong guess - increment counter
                                        wrongGuessCount++;
                                        Console.WriteLine($"Wrong guess #{wrongGuessCount}/{MAX_WRONG_GUESSES}");

                                        // Check if max wrong guesses exceeded
                                        if (wrongGuessCount >= MAX_WRONG_GUESSES)
                                        {
                                            Console.WriteLine("Max wrong guesses exceeded - game over");

                                            // Get list of unfound words for game over message
                                            List<string> unfoundWords = new List<string>();
                                            for (int i = 2; i < GameData.Length; i++)
                                            {
                                                if (!foundWords.ContainsKey(GameData[i]))
                                                {
                                                    unfoundWords.Add(GameData[i]);
                                                }
                                            }

                                            string unfoundList = string.Join(", ", unfoundWords);
                                            response = $"GameOver|MaxWrongGuessesExceeded|{unfoundList}";
                                            gameActive = false;
                                            // Don't break - keep connection alive for replay
                                        }
                                        else
                                        {
                                            response = "Wrong|TryAgain";
                                        }
                                    }
                                }
                                else
                                {
                                    // Correct guess
                                    response = $"{GameData[code]}|Found";
                                }
                            }
                        }
                        else
                        {
                            // Hello message from client - send initial game data
                            response = $"{GameData[0]}|{GameData[1]}|Jumble";
                            for (int i = 2; i < GameData.Length; i++)
                            {
                                response += $"|{GameData[i]}";
                            }
                        }
                    }

                    responseData = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseData, 0, responseData.Length);
                    Console.WriteLine($"Response sent: {response}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Client connection closed: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client Error: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine($"Client disconnected - Wrong guesses: {wrongGuessCount}, Hints used: {hintsRequested}");
            client.Close();
        }

        //
        // METHOD        : GetHint
        // DESCRIPTION   : Generates a hint for an unfound word. Provides first and last letter
        //                 as a clue to help the player.
        // PARAMETERS    : string[] GameData - The current game data array
        //                 Dictionary<string, bool> foundWords - Dictionary of found words
        // RETURNS       : string - Hint message for the player
        //
        private static string GetHint(string[] GameData, Dictionary<string, bool> foundWords)
        {
            // Find words that haven't been found yet
            List<string> unfoundWords = new List<string>();
            for (int i = 2; i < GameData.Length; i++)
            {
                string word = GameData[i];
                if (!foundWords.ContainsKey(word))
                {
                    unfoundWords.Add(word);
                }
            }

            if (unfoundWords.Count > 0)
            {
                Random rnd = new Random();
                string hintWord = unfoundWords[rnd.Next(unfoundWords.Count)];

                // For 3-letter words, give first and last letter hint
                return $"Starts with '{hintWord[0]}' and ends with '{hintWord[hintWord.Length - 1]}'";
            }

            return "All words found! Keep guessing!";
        }

        //
        // METHOD        : determine_response
        // DESCRIPTION   : Evaluates a client's guess against the word list and updates game state.
        //                 Determines if guess is correct, duplicate, or wrong.
        // PARAMETERS    : string message - The client's guess message
        //                 string[] Gamedata - The current game data array
        //                 Dictionary<string, bool> foundWords - Dictionary tracking found words
        // RETURNS       : int - Index of found word (2+), 0 for system messages, -1 for wrong/duplicate
        //
        private static int determine_response(string message, string[] Gamedata, Dictionary<string, bool> foundWords)
        {
            string clientMessage = message.Trim().ToLower();

            if (clientMessage.Contains("hello from client") || clientMessage == "new_game")
            {
                return 0;
            }

            for (int i = 2; i < Gamedata.Length; i++)
            {
                if (message.Equals(Gamedata[i], StringComparison.OrdinalIgnoreCase))
                {
                    if (!foundWords.ContainsKey(Gamedata[i]))
                    {
                        foundWords[Gamedata[i]] = true;
                        return i;
                    }
                    return -1; // Word already found (duplicate)
                }
            }
            return -1; // Word not found in list
        }

        //
        // METHOD        : ReadFileAsync
        // DESCRIPTION   : Loads a random game data file from the GameData directory.
        //                 Parses the file format: line1 = jumbled string, line2 = word count,
        //                 subsequent lines = words to find.
        // RETURNS       : string[] - Array containing game data [jumble, count, words...]
        //
        private static string[] ReadFileAsync()
        {
            string line;
            string gamefile = RandomGameData();
            string filepath = $@"C:\Users\najaf\Downloads\WNP-A02-main\WNP-A02-main\WNP-A02-V1\WNP-A02-V1\GameData\{gamefile}";

            using StreamReader sr = new StreamReader(filepath);

            string Jumble = sr.ReadLine();
            string Arraysize = sr.ReadLine();

            int size = int.Parse(Arraysize) + 2;
            string[] Localdata = new string[size];

            Localdata[0] = Jumble;
            Localdata[1] = Arraysize;

            int i = 2;
            while ((line = sr.ReadLine()) != null && i < Localdata.Length)
            {
                Localdata[i] = line;
                i++;
            }

            sr.Close();
            Console.WriteLine($"Loaded game file: {gamefile} with {Arraysize} words");
            return Localdata;
        }

        //
        // METHOD        : RandomGameData
        // DESCRIPTION   : Randomly selects one of the four game data files (data01.txt - data04.txt)
        //                 to provide variety between game sessions.
        // RETURNS       : string - Filename of selected game data file
        //
        private static string RandomGameData()
        {
            Random random = new Random();
            int GameFileNumber = random.Next(1, 5);
            string GameFileName = $"data{GameFileNumber.ToString("D2")}.txt";
            return GameFileName;
        }
    }
}