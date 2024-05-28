using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Threading;
using WMPLib;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

//System.Windows.MessageBox.Show("No instance running");
namespace XMRadio
{
    public partial class Window1
    {
        private XM xm = new XM(); //Utility Object
        
        private bool loginStatus = false; //See if the user is logged in
        private bool playStatus = false; //See if the media player is playing

        private bool firstUpdate = false;
        private bool relogin = false; //Used to see if it has been to long since the user changed channel. if it has, they will need to relogin into xm.

        private WindowsMediaPlayer player = new WindowsMediaPlayer(); //Create a media player object

        private XMChan channel = new XMChan(); //XMChan of the current chan being played

        private BitmapImage radioBitmap; //The station logo
        private BitmapImage albumBitmap = new BitmapImage(); //The album logo

        private const string lastFMKey = "REPLACE"; //The last fm key to pull album art

        //The object that all settings information
        private Settings settings = new Settings();

        // Delegates to be used in placing jobs onto the Dispatcher so the threads can be merged back into the gui
        private delegate void NoArgDelegate();
        private delegate void OneArgDelegateArrayList(ArrayList arg);
        private delegate void OneArgDelegateBool(bool arg);
        private delegate void TwoArgDelegateString(string arg, XMChan arg2);
        private delegate void OneArgDelegateBitmapImage(BitmapImage arg);
        private delegate void OneArgDelegateXMChan(XMChan arg);


        private DispatcherTimer chanTimer; //Used to update the channel list with the latest artist and songs
        private DispatcherTimer loginTimer; //Used to see if it has been 30 minutes since the user last change channels, thus forcing them to have to relogin to xm.

        //File system objects used to save the settings information
        private FileStream input;
        private BinaryFormatter reader = new BinaryFormatter();
        private FileStream output;
        private BinaryFormatter formatter = new BinaryFormatter();

        private const string PasswordMask = "********************************";

        public Window1()
        {

            this.InitializeComponent();
            chanGrid.AddHandler(MouseDoubleClickEvent, new RoutedEventHandler(StationDoubleClick));
            colorCombo.SelectedIndex = 0;
            albumRect.Visibility = Visibility.Hidden;
            radioRect.Visibility = Visibility.Hidden;

            LoadColorOptions();

            GetSettings();
            
        }
        private void LoadColorOptions()
        {
            //This is really cool thing that is known as reflection. It allows us to break out all of the methods and properties within an object or class.
            //We will use it again when we go and try to find out what the user selected for their colors.
            Type color = (typeof(Colors));
            System.Reflection.PropertyInfo[] colorProperties = color.GetProperties();
            int colorCount = colorProperties.Length;
            for (int i = 0; i < colorCount; i++)
            {
                colorCombo.Items.Add(colorProperties[i].Name);
                colorTextCombo.Items.Add(colorProperties[i].Name);
            }
        }
        
        private void GetSettings()
        {
            //Tries to automatically load a settings file located in the root directory of the executable called EmailSettings.db
            if (File.Exists("settings.db"))
            {
                // Creates FileStream to obtain read access to file
                input = new FileStream("settings.db", FileMode.Open, FileAccess.Read);

                try
                {

                    settings = (Settings)reader.Deserialize(input);
                    // Closing the FileStream
                    input.Close();
                }
                catch (SerializationException)
                {
                    MessageBox.Show("Could not automatically load settings file.\nPlease reinput your login information and save again.");
                }
            }
            if (settings != null)
            {
                userNameTextBox.Text = settings.User;
                passwordTextBox.Password = PasswordMask;
                colorCombo.SelectedIndex = colorCombo.Items.IndexOf(settings.Color);
                colorTextCombo.SelectedIndex = colorTextCombo.Items.IndexOf(settings.TextColor);
                SetSelectedColors();
            }
        }
        //Finds the station the user double clicked and then calls the change station method
        private void StationDoubleClick(object sender, EventArgs e)
        {
            pauseButton.Visibility = Visibility.Visible;
            if ((XMChan)chanGrid.SelectedItem != null)
            {
                XMChan tempChan = (XMChan)chanGrid.SelectedItem;
                channel = new XMChan(tempChan.ChanNum);
                tempChan = null;

                ChangeStation((XMChan)chanGrid.SelectedItem);
            }
        }
        //*************
        // Changing Channel System
        //************/
        //Begins seperate thread to change the station
        private void ChangeStation(XMChan chan)
        {
            //NoArgDelegate fetcher = new NoArgDelegate(this.ThreadChangeStation);
            OneArgDelegateXMChan fetcher = new OneArgDelegateXMChan(this.ThreadChangeStation);
            fetcher.BeginInvoke(chan, null, null);
        }
        //Relogins if necessary and then gets the url of the radio station
        private void ThreadChangeStation(XMChan chan)
        {
            if (relogin)
            {
                Login();
            }

            string channelURL = xm.GetStreamURL(chan.ChanNum);


            // Schedule the update function in the UI thread
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new TwoArgDelegateString(UpdateGuiChangeStation), channelURL, chan);
        }
        //Updates GUI with the status of the playing station
        private void UpdateGuiChangeStation(string channelURL, XMChan chan)
        {
            player.URL = channelURL;
            playStatus = true;
            
            DownloadChannelInformation();
            ChannelLogo(chan);
        }
        //Retrieves the channel logo for the station that the user is listening to
        private void ChannelLogo(XMChan chan)
        {
            radioBitmap = null;
            radioBitmap = new BitmapImage(new Uri("http://www.xmradio.com/images/channels/logos/main/" + chan.ChanNum + ".jpg"));
            radioImage.Source = radioBitmap;
        }
        //*************
        // END Changing Channel System
        //************/

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (loginStatus)
            {
                Disconnect();
            }
            else
            {
                Login();
            }
        }

        //*************
        // Main Channel List and Whats Playing System
        //************/
        //Begins seperate thread to download the latest channel information
        private void DownloadChannelInformation()
        {
            NoArgDelegate fetcher = new NoArgDelegate(this.ThreadDownloadChannelInformation);
            fetcher.BeginInvoke(null, null);
        }
        //Downloads from XM Radio the latest channel information for all of the stations.
        private void ThreadDownloadChannelInformation()
        {
            ArrayList xmAllChan = new ArrayList(); //XMChan Array

            xmAllChan = xm.GetAllChanInfo(ref xmAllChan);

            if(xmAllChan.Count == -1 || xmAllChan.Count == 0)
            {
                xmAllChan.Clear();
            }
            else
            {
                // Schedule the update function in the UI thread
                Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new OneArgDelegateArrayList(UpdateGuiChannelInformation),
                    xmAllChan);
            }
        }
        //Updates gui with the latest channel information. 
        //If the current song title is different then it also updates that with the latest information
        private void UpdateGuiChannelInformation(ArrayList xmAllChan)
        {
            XMChan tempXMChan = new XMChan();
            XMChan updatedXMChan = new XMChan();

            if (xmAllChan.Count != 0)
            {
                tempXMChan = (XMChan)xmAllChan[0];

            }
            if (tempXMChan != null)
            {
                if ((String)tempXMChan.SongTitle != "")
                {
                    chanGrid.Items.Clear();
                    chanNumCol.DisplayMemberBinding = new Binding("ChanNum");
                    chanNameCol.DisplayMemberBinding = new Binding("ChanName");
                    artistCol.DisplayMemberBinding = new Binding("ArtistName");
                    songCol.DisplayMemberBinding = new Binding("SongTitle");

                    for (int i = 0; i < xmAllChan.Count; i++)
                    {
                        if (xmAllChan[i] != null)
                        {
                            tempXMChan = (XMChan)xmAllChan[i];
                            if (channel.ChanNum == tempXMChan.ChanNum)
                            {
                                updatedXMChan = tempXMChan;
                            }
                            chanGrid.Items.Add(tempXMChan);
                            tempXMChan = null;
                        }
                    }
                    if (playStatus == true)
                    {
                        if (((String)channel.SongTitle != (String)updatedXMChan.SongTitle) && ((String)updatedXMChan.SongTitle != ""))
                        {
                            channel = updatedXMChan;
                            //**********************************************************************************************************
                            //NEED TO THREAD
                            //**********************************************************************************************************
                            UpdateAlbumArt(channel.ArtistName, channel.SongTitle, lastFMKey);

                            radioRect.Visibility = Visibility.Visible;
                            
                            artistLabel.Content = channel.ArtistName;
                            songLabel.Content = channel.SongTitle;
                            albumLabel.Content = channel.AlbumName;
                            chanNameLabel.Content = channel.ChanName;
                            chanNumLabel.Content = channel.ChanNum;
                            SetWindowTitle(channel.ArtistName, channel.SongTitle);
                        }
                        //System.Windows.MessageBox.Show(player.status);
                        updatedXMChan = null;
                    }
                }
            }
            tempXMChan = null;
            xmAllChan.Clear();
            xmAllChan = null;
            //System.Windows.MessageBox.Show("Thread Activated");
            //xmAllChan = null;
        }
        //*************
        // End Main Channel List and Whats Playing System
        //************/
        //*************
        //Disconnection System
        //************/
        //Starts new thread for logging into XM Radio so the gui does not freeze
        private void Disconnect()
        {
            NoArgDelegate fetcher = new NoArgDelegate(this.ThreadDisconnect);
            fetcher.BeginInvoke(null, null);

        }
        //Connects to XM Radio and disconnects. Then schedules thread with the main thread to be able to update the gui
        private void ThreadDisconnect()
        {
            try
            {
                xm.Disconnect();
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Unable to Connect to XM Server");
            }
            // Schedule the update function in the UI thread
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new NoArgDelegate(GuiUpdateDisconnect));
        }
        //Updates gui with the disconnection and stops the timers
        private void GuiUpdateDisconnect()
        {
            chanGrid.Items.Clear();
            connectButton.Content = "Connect";

            artistLabel.Content = "";
            songLabel.Content = "";
            albumLabel.Content = "";
            chanNameLabel.Content = "";
            chanNumLabel.Content = "";

            player.controls.stop();
            loginStatus = false;
        }
        //*************
        //END Disconnection System
        //************/
        //*************
        //Login System
        //************/
        //Starts new thread for logging into XM Radio so the gui does not freeze
        private void Login()
        {
            NoArgDelegate fetcher = new NoArgDelegate(this.ThreadLogin);
            fetcher.BeginInvoke(null, null);

        }
        //Connects to XM Radio and logins in. Returns status and then schedules thread with the main thread to be able to update the gui
        private void ThreadLogin()
        {
            try
            {
                loginStatus = xm.Login(settings.User, settings.Pass);

            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Unable to Connect to XM Server");
            }
            // Schedule the update function in the UI thread
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                new OneArgDelegateBool(UpdateGuiLogin),
                loginStatus);
        }
        //Updates gui with the status of the login and sets the timers to begin updating the channel information
        private void UpdateGuiLogin(bool loginStatus)
        {
            if (loginStatus)
            {
                connectButton.Content = "Disconnect";

                SetTimers();
            }
            else
            {
                System.Windows.MessageBox.Show("Incorrect Username / Password");
            }

        }
        //******************
        // End Login System
        //******************/

        private void SetTimers()
        {
            if (firstUpdate == false)
            {
                firstUpdate = true;
                DownloadChannelInformation();
            }
            chanTimer = new DispatcherTimer();
            chanTimer.Interval = TimeSpan.FromSeconds(15);
            chanTimer.Tick += new EventHandler(updateChannelInfoTimer);
            chanTimer.Start();

            loginTimer = new DispatcherTimer();
            loginTimer.Interval = TimeSpan.FromMinutes(30);
            loginTimer.Tick += new EventHandler(updateLoginInfoTimer);
            loginTimer.Start();
        }
        
        private void StopTimers()
        {
            chanTimer.Stop();
            loginTimer.Stop();
        }
        
        private void updateLoginInfoTimer(object sender, EventArgs e)
        {
            relogin = true;
            loginTimer.Stop();
        }
        
        private void updateChannelInfoTimer(object sender, EventArgs e)
        {
            DownloadChannelInformation();
        }
        
        private void settingsSaveButton_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedColors();

            settings.User = userNameTextBox.Text;
            if (passwordTextBox.Password != PasswordMask)
            {
                settings.Pass = passwordTextBox.Password;
            }
            settings.Color = colorCombo.SelectedItem.ToString();
            settings.TextColor = colorTextCombo.SelectedItem.ToString();

            try
            {
                // Opening the file with WRITE access
                output = new FileStream("settings.db", FileMode.OpenOrCreate, FileAccess.Write);
                formatter.Serialize(output, settings);
                output.Close();
            }
            catch (IOException)
            {
                // Notify user if file could not be opened
                MessageBox.Show("Error saving file");
            }
        }
        
        private void SetSelectedColors()
        {
            //Retrieves the selected color from dropdown by finding the appropriate property in the Colors class
            Type colors = (typeof(Colors));
            System.Reflection.PropertyInfo selectedColorProperty = colors.GetProperty(colorCombo.SelectedItem.ToString());
            System.Reflection.PropertyInfo selectedTextColorProperty = colors.GetProperty(colorTextCombo.SelectedItem.ToString());


            //Retrieves the property and then sets the approriate color for the window
            SolidColorBrush rectBrush = new SolidColorBrush((Color)selectedColorProperty.GetValue(null, null));
            infoRect.Fill = rectBrush;

            //Sets the text labels to what the user set
            SolidColorBrush textBrush = new SolidColorBrush((Color)selectedTextColorProperty.GetValue(null, null));
            artistLabel.Foreground = textBrush;
            songLabel.Foreground = textBrush;
            albumLabel.Foreground = textBrush;
            chanNameLabel.Foreground = textBrush;
            chanNumLabel.Foreground = textBrush;



            artistTextLabel.Foreground = textBrush;
            songTextLabel.Foreground = textBrush;
            albumTextLabel.Foreground = textBrush;
            channelTextLabel.Foreground = textBrush;
            channelNumberTextLabel.Foreground = textBrush;
        }

        private void UpdateAlbumArt(String artist, String song, String lastFMkey)
        {
            albumImage.Source = null;
            albumRect.Visibility = Visibility.Hidden;

            String albumURL = xm.GetAlbumArt(artist, song, lastFMKey);

            if (albumURL != "false")
            {
                if (albumURL != "")
                {
                    albumBitmap = new BitmapImage(new Uri(albumURL));
                    albumRect.Visibility = Visibility.Visible;
                    albumImage.Source = albumBitmap;
                    albumBitmap = null;
                }
                else
                {
                    albumImage.Source = null;
                    albumRect.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                albumImage.Source = null;
                albumRect.Visibility = Visibility.Hidden;
            }
            albumURL = null;
        }
        
        private void SetWindowTitle(String artistName, String songTitle)
        {
            Window.Title = "XMRadio Online - " + artistName + " - " + songTitle;
        }

        private void playButtonClick(object sender, RoutedEventArgs e)
        {
            playButton.Visibility = Visibility.Hidden;
            pauseButton.Visibility = Visibility.Visible;
            player.controls.play();
        }

        private void pauseButtonClick(object sender, RoutedEventArgs e)
        {
            playButton.Visibility = Visibility.Visible;
            pauseButton.Visibility = Visibility.Hidden;
            player.controls.stop();
        }

        private void TabChanged(object sender, SelectionChangedEventArgs e)
        {
            //Hides the password length after user switches tabs.
            passwordTextBox.Password = PasswordMask;
        }

        private void VolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Sets the volume when changed by the user
            player.settings.volume = Convert.ToInt32(volumeSlider.Value);
        }
    }
}