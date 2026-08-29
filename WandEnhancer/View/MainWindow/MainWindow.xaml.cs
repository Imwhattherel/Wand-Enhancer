using System;
using System.IO;
using System.Net.Http;
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

        public MainWindow()
        {
            InitializeComponent();

            this.ViewModel = new MainWindowVm(this);
            this.DataContext = ViewModel;
            VersionLabel.Text = Constants.Version.ToString();
            Instance = this;

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
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WandEnhancer"
                );

                Directory.CreateDirectory(musicDirectory);

                _musicFilePath = Path.Combine(
                    musicDirectory,
                    "music.mp3"
                );

                // Download the music if it isn't already cached.
                if (!File.Exists(_musicFilePath))
                {
                    using (HttpClient client = new HttpClient())
                    {
                        byte[] musicData = await client.GetByteArrayAsync(MusicUrl);

                        await File.WriteAllBytesAsync(
                            _musicFilePath,
                            musicData
                        );
                    }
                }

                // Give WPF a local file instead of a remote URL.
                BackgroundMusic.Source = new Uri(
                    _musicFilePath,
                    UriKind.Absolute
                );

                BackgroundMusic.Play();
            }
            catch (Exception ex)
            {
                // Don't crash WandEnhancer if the music cannot be downloaded.
                System.Diagnostics.Debug.WriteLine(
                    "Could not start background music: " + ex
                );
            }
        }

        private void BackgroundMusic_MediaEnded(
            object sender,
            RoutedEventArgs e)
        {
            BackgroundMusic.Position = TimeSpan.Zero;
            BackgroundMusic.Play();
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
            BackgroundMusic.Stop();

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
