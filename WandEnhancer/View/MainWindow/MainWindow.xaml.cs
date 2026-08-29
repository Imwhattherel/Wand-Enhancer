using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WandEnhancer.View.MainWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Instance;
        public readonly MainWindowVm ViewModel;

        private const string MusicUrl =
            "https://r2.fivemanage.com/2i6WAFjuTz1VPYHiXPojU/music.mp3";

        private string _musicFilePath;

        private bool _isMuted;
        private double _volumeBeforeMute = 0.05;

        public MainWindow()
        {
            InitializeComponent();

            this.ViewModel = new MainWindowVm(this);
            this.DataContext = ViewModel;
            VersionLabel.Text = Constants.Version.ToString();
            Instance = this;

            // Start the music after the WPF window has loaded.
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await StartBackgroundMusic();
        }

        private async Task StartBackgroundMusic()
        {
            try
            {
                string musicDirectory = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "WandEnhancer"
                );

                Directory.CreateDirectory(musicDirectory);

                _musicFilePath = Path.Combine(
                    musicDirectory,
                    "music.mp3"
                );

                // Download the MP3 only if it isn't already cached.
                if (!File.Exists(_musicFilePath) ||
                    new FileInfo(_musicFilePath).Length == 0)
                {
                    using (var client = new WebClient())
                    {
                        await client.DownloadFileTaskAsync(
                            new Uri(MusicUrl),
                            _musicFilePath
                        );
                    }
                }

                // Make sure the downloaded file actually exists.
                if (!File.Exists(_musicFilePath) ||
                    new FileInfo(_musicFilePath).Length == 0)
                {
                    return;
                }

                // Use a LOCAL file for MediaElement.
                BackgroundMusic.Source = new Uri(
                    _musicFilePath,
                    UriKind.Absolute
                );

                // Remember the starting volume so mute/unmute has
                // something sensible to restore to.
                _volumeBeforeMute = BackgroundMusic.Volume;

                BackgroundMusic.Play();
            }
            catch (Exception ex)
            {
                // Music failure should never prevent WandEnhancer from running.
                System.Diagnostics.Debug.WriteLine(
                    "Background music could not be started: " + ex
                );
            }
        }

        private void BackgroundMusic_MediaEnded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                BackgroundMusic.Position = TimeSpan.Zero;
                BackgroundMusic.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Background music loop failed: " + ex
                );
            }
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            _isMuted = !_isMuted;

            try
            {
                if (_isMuted)
                {
                    // Only remember a non-zero volume, so repeated
                    // clicks don't collapse the remembered level to 0.
                    if (BackgroundMusic.Volume > 0)
                    {
                        _volumeBeforeMute = BackgroundMusic.Volume;
                    }

                    BackgroundMusic.Volume = 0;
                    MuteButton.Tag = FindResource("VolumeMuteIcon");
                    MuteButton.ToolTip = "Unmute background music";
                }
                else
                {
                    BackgroundMusic.Volume = _volumeBeforeMute;
                    MuteButton.Tag = FindResource("VolumeIcon");
                    MuteButton.ToolTip = "Mute background music";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Failed to toggle background music mute: " + ex
                );
            }
        }

        public void OpenPopup(
            FrameworkElement content,
            string title = null)
        {
            this.PopupHost.PopupContent = content;
            PopupHost.Title.Text = title;
            PopupHost.IsOpen = true;
        }

        private void OnDragMove(
            object sender,
            MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void OnClosing(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                BackgroundMusic.Stop();
            }
            catch
            {
                // Ignore media shutdown errors.
            }

            Application.Current.Shutdown();
        }

        public void ClosePopup()
        {
            PopupHost.IsOpen = false;
        }

        private void OpenSourceClicked(
            object sender,
            MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(
                Constants.RepositoryUrl
            );
        }
    }
}
