/*
* FILE          : GameWindow.xaml.cs
* PROJECT       : A02 - TCPIP 
* PROGRAMMER    : Najaf Ali, Che-Ping Chien, Precious Orewen, Yi-Chen Tsai
* FIRST VERSION : 2026-02-05
* DESCRIPTION   :
*      This file contains the code-behind logic for the main game window of the Word Jumble Game client.
*      It implements the complete client-side game functionality including:
*      
*      Network Communication:
*      - TCP/IP connection to the game server using TcpClient and NetworkStream
*      - Asynchronous message sending/receiving with Task-based operations
*      - Protocol handling for game commands (GUESS, HINT, NEW_GAME, DISCONNECT, TIME_UP)
*      - Connection resilience with automatic reconnection on failure
*      
*      Game Management:
*      - Timer system with visual feedback (2-minute game duration)
*      - Score tracking (words found, wrong guesses, hints used)
*      - Win/loss condition checking (all words found, max wrong guesses exceeded, time expired)
*      - Dynamic UI updates for game state changes
*      - Enhanced game over handling with server-reported unfound words
*      
*      User Interface:
*      - Found word display with random positioning in grid
*      - Visual feedback for game events with emoji-enhanced MessageBox dialogs
*      - Input validation (letters only, non-empty guesses)
*      - Hint system with maximum usage tracking (3 hints max)
*      - Color-coded statistics based on remaining resources
*      
*      Game Flow:
*      - Game initialization from server data (jumbled string, word list)
*      - Play again functionality with connection verification and auto-reconnect
*      - Graceful disconnection and return to main menu
*      - Error handling for network failures and server disconnections
*      - Connection state validation before each server communication
*      
*      The client provides a complete, user-friendly gaming experience with
*      robust error handling and intuitive visual feedback throughout the game.
*/

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace WDFClient
{
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        // TCP/IP communication objects
        private TcpClient client;
        private NetworkStream stream;

        // Game timing components
        private DispatcherTimer gameTimer;
        private TimeSpan timeLeft;

        // Game state flags
        private bool gameActive = false;
        private bool isDisconnecting = false;
        private bool isGameOver = false;

        // Game data
        private string currentJumble;
        private List<string> allWords = new List<string>();
        private Random rnd = new Random();

        // UI element references
        private Label[] guessLabels;
        private Label[] guessLabels2;

        // Game statistics
        private int TotalFound = 0;
        private int currentFound = 0;
        private int wrongGuesses = 0;
        private int hintsUsed = 0;
        private List<string> FoundWordsList = new List<string>();

        // Game constants
        private const int MAX_WRONG_GUESSES = 3;
        private const int MAX_HINTS = 3;
        private const int GAME_DURATION_SECONDS = 120; // 2 minutes

        //
        // CONSTRUCTOR   : GameWindow
        // DESCRIPTION   : Initializes the game window, sets up UI element references,
        //                 registers event handlers, and initializes the game timer
        //
        public GameWindow()
        {
            InitializeComponent();
            Loaded += GameWindow_Loaded;
            this.Closed += GameWindow_Closed;

            // Initialize arrays for found word display labels
            guessLabels = new Label[] { Guess1, Guess2, Guess3, Guess4, Guess5, Guess6, Guess7, Guess8, Guess9 };
            guessLabels2 = new Label[] { Guess11, Guess12, Guess13, Guess14, Guess15, Guess16, Guess17, Guess18, Guess19, Guess20,
                                        Guess21, Guess22, Guess23, Guess24, Guess25, Guess26, Guess27, Guess28, Guess29, Guess30 };

            InitializeTimer();
        }

        //
        // METHOD        : InitializeTimer
        // DESCRIPTION   : Creates and configures the game timer with 1-second interval
        //
        private void InitializeTimer()
        {
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromSeconds(1);
            gameTimer.Tick += GameTimer_Tick;
        }

        //
        // EVENT HANDLER : GameTimer_Tick
        // DESCRIPTION   : Handles timer tick events, decrements time remaining,
        //                 updates display, and sends TIME_UP message to server when time expires
        //
        private async void GameTimer_Tick(object sender, EventArgs e)
        {
            if (gameActive && !isGameOver)
            {
                timeLeft = timeLeft.Subtract(TimeSpan.FromSeconds(1));
                UpdateTimerDisplay();

                if (timeLeft.TotalSeconds <= 0)
                {
                    // Stop timer first
                    gameTimer.Stop();
                    gameActive = false;

                    // Send time up message to server to get unfound words
                    await Send_message("TIME_UP");
                }
            }
        }

        //
        // METHOD        : UpdateTimerDisplay
        // DESCRIPTION   : Updates the timer display with current time remaining
        //                 Changes color based on urgency (orange at 30s, red at 10s)
        //
        private void UpdateTimerDisplay()
        {
            TimeDisplay.Content = timeLeft.ToString(@"mm\:ss");

            // Change color when less than 30 seconds remain
            if (timeLeft.TotalSeconds <= 30)
            {
                TimeDisplay.Foreground = System.Windows.Media.Brushes.Orange;
            }
            if (timeLeft.TotalSeconds <= 10)
            {
                TimeDisplay.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        //
        // METHOD        : UpdateStatsDisplay
        // DESCRIPTION   : Updates the statistics display (wrong guesses, hints left)
        //                 Changes colors based on remaining resources for visual feedback
        //
        private void UpdateStatsDisplay()
        {
            WrongGuessDisplay.Content = $"{wrongGuesses}/{MAX_WRONG_GUESSES}";
            HintsLeftDisplay.Content = $"{MAX_HINTS - hintsUsed}";

            // Change color based on wrong guesses left
            if (wrongGuesses >= 2)
            {
                WrongGuessDisplay.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (wrongGuesses >= 1)
            {
                WrongGuessDisplay.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                WrongGuessDisplay.Foreground = System.Windows.Media.Brushes.Green;
            }

            // Change color based on hints left
            if (hintsUsed >= MAX_HINTS)
            {
                HintsLeftDisplay.Foreground = System.Windows.Media.Brushes.Gray;
            }
            else if (hintsUsed >= 2)
            {
                HintsLeftDisplay.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                HintsLeftDisplay.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        //
        // METHOD        : StartGameTimer
        // DESCRIPTION   : Initializes and starts the game timer for a new game
        //
        private void StartGameTimer()
        {
            timeLeft = TimeSpan.FromSeconds(GAME_DURATION_SECONDS);
            UpdateTimerDisplay();
            gameActive = true;
            isGameOver = false;
            gameTimer.Start();
        }

        //
        // METHOD        : GameOver
        // DESCRIPTION   : Handles game over scenarios (time up, too many wrong guesses, win)
        //                 Displays appropriate message with emojis and offers play again option
        // PARAMETERS    : string message - The game over reason/message
        //                 string unfoundWords - List of words the player missed
        //
        private void GameOver(string message, string unfoundWords)
        {
            if (isGameOver || isDisconnecting) return; // Prevent multiple game over calls

            isGameOver = true;
            gameActive = false;
            gameTimer.Stop();

            string gameOverMessage;
            string title;

            if (message == "TIME'S UP!")
            {
                gameOverMessage = $"TIME'S UP!\n\n" +
                                 $"Words found: {currentFound} out of {TotalFound}\n" +
                                 $"Wrong guesses: {wrongGuesses}/{MAX_WRONG_GUESSES}\n" +
                                 $"Hints used: {hintsUsed}/{MAX_HINTS}\n\n";

                if (!string.IsNullOrEmpty(unfoundWords))
                {
                    gameOverMessage += $"Words you missed:\n{unfoundWords}\n\n";
                }

                gameOverMessage += $"Better luck next time!";
                title = "Game Over";
            }
            else if (message == "TOO MANY WRONG GUESSES!")
            {
                gameOverMessage = $"GAME OVER!\n\n" +
                                 $"You've made {MAX_WRONG_GUESSES} wrong guesses.\n" +
                                 $"Words found: {currentFound} out of {TotalFound}\n" +
                                 $"Hints used: {hintsUsed}/{MAX_HINTS}\n\n";

                if (!string.IsNullOrEmpty(unfoundWords))
                {
                    gameOverMessage += $"Words you missed:\n{unfoundWords}\n\n";
                }

                gameOverMessage += $"Try again!";
                title = "Game Over";
            }
            else
            {
                gameOverMessage = message;
                title = "Game Over";
            }

            // Show game over dialog
            MessageBoxResult result = MessageBox.Show($"{gameOverMessage}\n\nWould you like to play again?",
                                                      title,
                                                      MessageBoxButton.YesNo,
                                                      MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                PlayAgain();
            }
            else
            {
                DisconnectAndReturnToMenu();
            }
        }

        //
        // METHOD        : CheckForWin
        // DESCRIPTION   : Checks if all words have been found and triggers win condition
        //                 Displays celebratory message with trophy emoji
        //
        private void CheckForWin()
        {
            if (currentFound >= TotalFound && TotalFound > 0 && !isGameOver && !isDisconnecting)
            {
                isGameOver = true;
                gameActive = false;
                gameTimer.Stop();

                MessageBoxResult result = MessageBox.Show($"CONGRATULATIONS!\n\n" +
                                                          $"You found all {TotalFound} words!\n" +
                                                          $"Wrong guesses: {wrongGuesses}/{MAX_WRONG_GUESSES}\n" +
                                                          $"Hints used: {hintsUsed}/{MAX_HINTS}\n\n" +
                                                          $"Would you like to play again?",
                                                          "You Win!",
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    PlayAgain();
                }
                else
                {
                    DisconnectAndReturnToMenu();
                }
            }
        }

        //
        // METHOD        : PlayAgain
        // DESCRIPTION   : Resets game state, verifies/reconnects to server, and requests a new game
        //                 Includes connection resilience with automatic reconnection attempts
        //
        private async void PlayAgain()
        {
            // Reset UI
            ClearGuessLabels();
            FoundWordsList.Clear();
            currentFound = 0;
            wrongGuesses = 0;
            hintsUsed = 0;
            isGameOver = false;
            Scoredisplay.Content = $"0/{TotalFound}";
            UpdateStatsDisplay();

            try
            {
                // Check if connection is still alive
                if (client == null || !client.Connected)
                {
                    // Reconnect if needed
                    client = new TcpClient();
                    await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 5000);
                    stream = client.GetStream();
                }

                // Request new game from server
                await Send_message("NEW_GAME");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PlayAgain: {ex.Message}");
                // Try to reconnect
                try
                {
                    CloseConnections();
                    client = new TcpClient();
                    await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 5000);
                    stream = client.GetStream();
                    await Send_message("NEW_GAME");
                }
                catch
                {
                    MessageBox.Show("Failed to reconnect to server. Returning to main menu.",
                                  "Connection Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                    ReturnToMainMenu();
                }
            }
        }

        //
        // METHOD        : ClearGuessLabels
        // DESCRIPTION   : Resets all found word labels to "???" for new game
        //
        private void ClearGuessLabels()
        {
            foreach (Label label in guessLabels)
            {
                label.Content = "???";
            }
            foreach (Label label in guessLabels2)
            {
                label.Content = "???";
            }
        }

        //
        // METHOD        : DisconnectAndReturnToMenu
        // DESCRIPTION   : Gracefully disconnects from server and returns to main menu
        //
        private async void DisconnectAndReturnToMenu()
        {
            isDisconnecting = true;
            gameActive = false;
            gameTimer?.Stop();

            try
            {
                // Send disconnect message to server
                if (stream != null && client != null && client.Connected)
                {
                    byte[] disconnectMsg = Encoding.UTF8.GetBytes("DISCONNECT");
                    await stream.WriteAsync(disconnectMsg, 0, disconnectMsg.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending disconnect: {ex.Message}");
            }
            finally
            {
                CloseConnections();
                ReturnToMainMenu();
            }
        }

        //
        // METHOD        : CloseConnections
        // DESCRIPTION   : Closes network stream and client connection with null safety
        //
        private void CloseConnections()
        {
            try
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
                if (client != null)
                {
                    client.Close();
                    client = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing connections: {ex.Message}");
            }
        }

        //
        // METHOD        : ReturnToMainMenu
        // DESCRIPTION   : Returns to the main menu window
        //
        private void ReturnToMainMenu()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        //
        // EVENT HANDLER : GameWindow_Loaded
        // DESCRIPTION   : Handles window loaded event - establishes server connection
        //                 and initializes game
        //
        private async void GameWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 5000);
                stream = client.GetStream();
                string message = "Hello from client!";
                await Send_message(message);
                StartGameTimer();
                UpdateStatsDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to server:\n{ex.Message}",
                              "Connection Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                ReturnToMainMenu();
            }
        }

        //
        // EVENT HANDLER : GameWindow_Closed
        // DESCRIPTION   : Handles window closed event - stops timer and closes connections
        //
        private void GameWindow_Closed(object sender, EventArgs e)
        {
            gameTimer?.Stop();
            CloseConnections();
        }

        //
        // METHOD        : Send_message
        // DESCRIPTION   : Sends a message to the server and processes the response
        //                 Core communication method implementing the game protocol
        //                 Enhanced to handle TIME_UP messages and emoji-enhanced responses
        // PARAMETERS    : string message - The message to send to the server
        // RETURNS       : Task - Asynchronous operation
        //
        public async Task Send_message(string message)
        {
            try
            {
                // Check if connection is valid
                if (client == null || !client.Connected || stream == null)
                {
                    throw new Exception("Connection to server lost");
                }

                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                Console.WriteLine($"Message sent: {message}");

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    // Server disconnected
                    if (!isDisconnecting && !isGameOver)
                    {
                        MessageBox.Show("Server disconnected.", "Connection Lost",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        ReturnToMainMenu();
                    }
                    return;
                }

                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Response received: {response}");
                string[] info = response.Split('|');

                // Check for GameOver message first (handles both wrong guesses and time up)
                if (info[0] == "GameOver")
                {
                    if (info.Length > 1)
                    {
                        if (info[1] == "MaxWrongGuessesExceeded")
                        {
                            string unfoundWords = info.Length > 2 ? info[2] : "";
                            GameOver("TOO MANY WRONG GUESSES!", unfoundWords);
                        }
                        else if (info[1] == "TimeIsUp")
                        {
                            string unfoundWords = info.Length > 2 ? info[2] : "";
                            GameOver("TIME'S UP!", unfoundWords);
                        }
                        else
                        {
                            GameOver("Game Over", null);
                        }
                    }
                    else
                    {
                        GameOver("Game Over", null);
                    }
                    return;
                }

                // Don't process further if game is already over
                if (isGameOver)
                {
                    return;
                }

                // Process correct guess
                if (info.Length >= 2 && info[1] == "Found")
                {
                    string keyword = info[0];
                    if (!FoundWordsList.Contains(keyword))
                    {
                        FoundWordsList.Add(keyword);
                        currentFound++;
                        Scoredisplay.Content = $"{currentFound}/{TotalFound}";
                        displayFound(TotalFound, keyword);
                        GuessBox.Text = "";

                        // Show success message with checkmark emoji
                        MessageBox.Show($"Correct! You found '{keyword}'!",
                                      "Good Guess!",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);

                        CheckForWin(); // Check if player has won
                    }
                }
                // Process new game initialization
                else if (info.Length >= 3 && info[2] == "Jumble")
                {
                    int.TryParse(info[1], out TotalFound);
                    currentJumble = info[0];

                    // Store all words from server response
                    allWords.Clear();
                    for (int i = 3; i < info.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(info[i]))
                        {
                            allWords.Add(info[i]);
                        }
                    }

                    FoundWordsList.Clear();
                    currentFound = 0;
                    wrongGuesses = 0;
                    hintsUsed = 0;
                    isGameOver = false;
                    Scoredisplay.Content = $"{currentFound}/{TotalFound}";
                    UpdateStatsDisplay();

                    // Display appropriate grid based on word count
                    if (TotalFound <= 9)
                    {
                        GuessSpace10.Visibility = Visibility.Visible;
                        GuessSpace10.IsEnabled = true;
                        GuessSpace20.Visibility = Visibility.Hidden;
                        GuessSpace20.IsEnabled = false;
                    }
                    else
                    {
                        GuessSpace20.Visibility = Visibility.Visible;
                        GuessSpace20.IsEnabled = true;
                        GuessSpace10.Visibility = Visibility.Hidden;
                        GuessSpace10.IsEnabled = false;
                    }

                    Worddisplay.Content = info[0];
                    StartGameTimer(); // Restart timer for new game
                }
                // Process hint response
                else if (info[0] == "HINT")
                {
                    if (info.Length > 1 && !string.IsNullOrEmpty(info[1]))
                    {
                        if (info[1] == "No hints left!")
                        {
                            MessageBox.Show($"No hints left! You've used all {MAX_HINTS} hints.",
                                          "No Hints",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Warning);
                        }
                        else
                        {
                            MessageBox.Show($"Hint: {info[1]}",
                                          "Word Hint",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("No hints available right now!", "Hint",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                // Process duplicate word guess
                else if (info[0] == "Duplicate")
                {
                    // Duplicate word - already found
                    MessageBox.Show($"You already found '{GuessBox.Text.ToUpper().Trim()}'!",
                                  "Already Found",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    GuessBox.Text = "";
                }
                // Process wrong guess
                else if (info[0] == "Wrong")
                {
                    // Wrong guess - increment counter
                    wrongGuesses++;
                    UpdateStatsDisplay();
                    GuessBox.Text = "";

                    // Show wrong guess message with remaining attempts
                    int remaining = MAX_WRONG_GUESSES - wrongGuesses;
                    string guessWord = remaining == 1 ? "guess" : "guesses";
                    MessageBox.Show($"Wrong guess! {remaining} wrong {guessWord} left.",
                                  "Incorrect",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
                else
                {
                    GuessBox.Text = "";
                }
            }
            catch (Exception ex)
            {
                if (!isDisconnecting && !isGameOver)
                {
                    MessageBox.Show($"Communication error:\n{ex.Message}\n\nReturning to main menu.",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    gameActive = false;
                    gameTimer.Stop();
                    ReturnToMainMenu();
                }
            }
        }

        //
        // EVENT HANDLER : Hint_Click
        // DESCRIPTION   : Handles hint button click - requests hint from server
        //                 Tracks hint usage and enforces maximum hint limit
        //
        private async void Hint_Click(object sender, RoutedEventArgs e)
        {
            if (!gameActive || isGameOver)
            {
                MessageBox.Show("Game is not active!", "Hint",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if hints are available
            if (hintsUsed >= MAX_HINTS)
            {
                MessageBox.Show($"No hints left! You've used all {MAX_HINTS} hints.",
                              "No Hints", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Increment hints used
            hintsUsed++;
            UpdateStatsDisplay();

            // Request hint from server
            await Send_message("REQUEST_HINT");
        }

        //
        // METHOD        : displayFound
        // DESCRIPTION   : Places a found word in a random empty label in the grid
        //                 Falls back to sequential placement if random placement fails
        // PARAMETERS    : int totalFound - Total number of words to find
        //                 string wordFound - The word that was found
        //
        private void displayFound(int totalFound, string wordFound)
        {
            // Check if word is already displayed
            foreach (Label gl in guessLabels)
            {
                if (gl.Content.ToString() == wordFound)
                {
                    return;
                }
            }
            foreach (Label gl in guessLabels2)
            {
                if (gl.Content.ToString() == wordFound)
                {
                    return;
                }
            }

            Label selectedLabel;
            bool filled = false;
            int attempts = 0;
            int maxAttempts = totalFound * 2;

            while (!filled && attempts < maxAttempts)
            {
                int labelnumber = rnd.Next(0, totalFound);

                if (TotalFound <= 9)
                {
                    selectedLabel = guessLabels[labelnumber];
                    if (selectedLabel.Content.ToString() == "???")
                    {
                        selectedLabel.Content = wordFound;
                        filled = true;
                    }
                }
                else
                {
                    selectedLabel = guessLabels2[labelnumber];
                    if (selectedLabel.Content.ToString() == "???")
                    {
                        selectedLabel.Content = wordFound;
                        filled = true;
                    }
                }
                attempts++;
            }

            // If we couldn't place randomly, find first empty slot
            if (!filled)
            {
                if (TotalFound <= 9)
                {
                    foreach (Label label in guessLabels)
                    {
                        if (label.Content.ToString() == "???")
                        {
                            label.Content = wordFound;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (Label label in guessLabels2)
                    {
                        if (label.Content.ToString() == "???")
                        {
                            label.Content = wordFound;
                            break;
                        }
                    }
                }
            }
        }

        //
        // EVENT HANDLER : Guess_Click
        // DESCRIPTION   : Handles guess button click - validates input and sends guess to server
        //                 Provides emoji-enhanced error messages for invalid input
        //
        private async void Guess_Click(object sender, RoutedEventArgs e)
        {
            if (!gameActive || isGameOver)
            {
                MessageBox.Show("Game is not active! Please start a new game.",
                              "Not Active", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string guess = GuessBox.Text.ToUpper().Trim();

            if (string.IsNullOrEmpty(guess))
            {
                MessageBox.Show("Please enter a guess!",
                              "Empty Guess", MessageBoxButton.OK, MessageBoxImage.Warning);
                GuessBox.Focus();
                return;
            }

            if (!Regex.IsMatch(guess, "^[a-zA-Z]*$"))
            {
                MessageBox.Show("Please enter only letters (A-Z)!",
                              "Invalid Characters", MessageBoxButton.OK, MessageBoxImage.Warning);
                GuessBox.SelectAll();
                GuessBox.Focus();
                return;
            }

            await Send_message(guess);
        }

        //
        // EVENT HANDLER : GuessBox_TextChanged
        // DESCRIPTION   : Optional placeholder for text change handling
        //
        private void GuessBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Optional: Add any text changed logic here
        }

        //
        // EVENT HANDLER : Quit_Click
        // DESCRIPTION   : Handles quit button click - confirms and exits current game
        //
        private async void Quit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to quit the current game?",
                                                     "Confirm Quit",
                                                     MessageBoxButton.YesNo,
                                                     MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await Send_message("DISCONNECT");
                DisconnectAndReturnToMenu();
            }
        }
    }
}