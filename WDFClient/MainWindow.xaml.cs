/*
* FILE          : MainWindow.xaml.cs
* PROJECT       : A02 - TCPIP 
* PROGRAMMER    : Najaf Ali, Che-Ping Chien, Precious Orewen, Yi-Chen Tsai
* FIRST VERSION : 2026-02-05
* DESCRIPTION   :
*      This file contains the code-behind logic for the main menu window of the Word Jumble Game client.
*      It implements the entry point interface that handles player registration and game initialization.
*      
*      User Interface Management:
*      - Panel switching between main menu (Play/Quit) and login interface
*      - Smooth transitions between application states
*      - Focus management for improved user experience
*      
*      Player Validation:
*      - Comprehensive input validation for player names:
*        * Cannot be empty or whitespace
*        * Maximum length of 8 characters
*        * Alphanumeric characters only (letters and numbers)
*      - User-friendly error messages with specific validation feedback
*      
*      Game Flow:
*      - Creates new GameWindow instance upon successful validation
*      - Passes player name to game window via Title property
*      - Handles navigation between main menu and login screens
*      - Manages application exit with confirmation dialog
*      
*      The class serves as the gateway to the game, ensuring that only valid
*      player names are passed to the game client before establishing server
*      connections and starting gameplay.
*/

using System.Windows;
using System.Text.RegularExpressions;

namespace WDFClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //
        // CONSTRUCTOR   : MainWindow
        // DESCRIPTION   : Initializes the main window components and sets up the UI
        //
        public MainWindow()
        {
            InitializeComponent();
        }

        //
        // EVENT HANDLER : Play_Click
        // DESCRIPTION   : Handles the Play button click event - transitions from main menu
        //                 to login panel where players can enter their name
        // PARAMETERS    : object sender - The source of the event (Play button)
        //                 RoutedEventArgs e - Event data
        //
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            // Hide main menu buttons
            MainMenuPanel.Visibility = Visibility.Hidden;

            // Show login panel
            LoginPanel.Visibility = Visibility.Visible;
            LoginBox.Focus();
        }

        //
        // EVENT HANDLER : Quit_Click
        // DESCRIPTION   : Handles the Quit button click event - displays confirmation dialog
        //                 and exits the application if user confirms
        // PARAMETERS    : object sender - The source of the event (Quit button)
        //                 RoutedEventArgs e - Event data
        //
        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to quit?",
                                                     "Confirm Exit",
                                                     MessageBoxButton.YesNo,
                                                     MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Close();
            }
        }

        //
        // EVENT HANDLER : Login_Click
        // DESCRIPTION   : Handles the Login/Start button click event - validates player name
        //                 input and launches the game window if validation passes
        // PARAMETERS    : object sender - The source of the event (Start button)
        //                 RoutedEventArgs e - Event data
        //
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string PlayerName = LoginBox.Text.Trim();

            // Validate that name is not empty
            if (string.IsNullOrEmpty(PlayerName))
            {
                MessageBox.Show("Player name cannot be empty, please try again",
                              "Invalid Name",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            // Validate name length (max 8 characters)
            if (PlayerName.Length > 8)
            {
                MessageBox.Show("Name must be 8 characters or less, please try again",
                              "Invalid Name",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            // Validate name contains only alphanumeric characters
            if (!Regex.IsMatch(PlayerName, "^[a-zA-Z0-9]*$"))
            {
                MessageBox.Show("Name can only contain letters and numbers",
                              "Invalid Name",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            // Start the game with validated player name
            GameWindow GW = new GameWindow();
            GW.Title = $"Word Jumble - Player: {PlayerName}";
            GW.Show();
            Close();
        }

        //
        // EVENT HANDLER : Back_Click
        // DESCRIPTION   : Handles the Back button click event - returns from login panel
        //                 to main menu and clears any entered text
        // PARAMETERS    : object sender - The source of the event (Back button)
        //                 RoutedEventArgs e - Event data
        //
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // Show main menu buttons
            MainMenuPanel.Visibility = Visibility.Visible;

            // Hide login panel
            LoginPanel.Visibility = Visibility.Hidden;
            LoginBox.Text = "";
        }
    }
}