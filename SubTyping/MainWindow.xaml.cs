using System.ComponentModel;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Threading;
using YoutubeExplode.Videos;

namespace SubTyping
{
    public partial class MainWindow : Window
    {
        private readonly LocalAudioPlayer localAudioPlayer = new();
        private readonly DispatcherTimer localPlaybackTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
        private bool isLocalTrackLoaded;
        private bool isFullScreen;

        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainViewModel();
            viewModel.PropertyChanged += ViewModelOnPropertyChanged;
            viewModel.VideoLoadRequested += ViewModelOnVideoLoadRequested;
            viewModel.PlayRequested += ViewModelOnPlayRequested;
            viewModel.PauseRequested += ViewModelOnPauseRequested;
            viewModel.ContinueRequested += ViewModelOnContinueRequested;
            viewModel.LocalTrackLoaded += ViewModelOnLocalTrackLoaded;
            DataContext = viewModel;
            YouTubePlayer.CurrentTimeChanged += (_, seconds) =>
            {
                viewModel.OnPlaybackTime(TimeSpan.FromSeconds(seconds));
            };
            localPlaybackTimer.Tick += (_, _) => viewModel.OnPlaybackTime(localAudioPlayer.Position);
            Loaded += async (_, _) =>
            {
                try
                {
                    await YouTubePlayer.InitializeAsync();
                }
                catch (Exception exception)
                {
                    viewModel.StatusMessage = $"WebView2 is unavailable: {exception.Message}";
                }
            };
        }

        private async void ViewModelOnVideoLoadRequested(object? sender, string url)
        {
            isLocalTrackLoaded = false;
        localPlaybackTimer.Stop();
            try
            {
                await YouTubePlayer.LoadVideoAsync(VideoId.Parse(url).Value);
            }
            catch (Exception exception)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.StatusMessage = $"Could not load the YouTube player: {exception.Message}";
                }
            }
        }

        private async void ViewModelOnPlayRequested(object? sender, EventArgs e)
        {
            if (isLocalTrackLoaded)
            {
                localAudioPlayer.Play();
                return;
            }

            try
            {
                await YouTubePlayer.PlayAsync();
            }
            catch (Exception exception)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.StatusMessage = $"Could not play the video: {exception.Message}";
                }
            }
        }

        private async void ViewModelOnPauseRequested(object? sender, EventArgs e)
        {
            if (isLocalTrackLoaded)
            {
                localAudioPlayer.Pause();
                return;
            }

            try
            {
                await YouTubePlayer.PauseAsync();
            }
            catch
            {
                // The player may already be closed while the window is shutting down.
            }
        }

        private async void ViewModelOnContinueRequested(object? sender, EventArgs e)
        {
            if (isLocalTrackLoaded)
            {
                localAudioPlayer.Play();
                return;
            }

            try
            {
                await YouTubePlayer.PlayAsync();
            }
            catch (Exception exception)
            {
                if (DataContext is MainViewModel viewModel)
            {
                    viewModel.StatusMessage = $"Could not resume the video: {exception.Message}";
            }
            }
        }

        private void PracticeInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter || DataContext is not MainViewModel viewModel)
            {
                return;
            }

            e.Handled = true;
            viewModel.ContinueAfterEnter();
        }

        private void ToggleFullScreen_Click(object sender, RoutedEventArgs e)
        {
            isFullScreen = !isFullScreen;
            WindowStyle = isFullScreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;
            WindowState = isFullScreen ? WindowState.Maximized : WindowState.Normal;
            ResizeMode = isFullScreen ? ResizeMode.NoResize : ResizeMode.CanResize;
        }

        private async void ChooseMp3_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "MP3 audio (*.mp3)|*.mp3",
                Title = "Choose an MP3 song"
            };
            if (dialog.ShowDialog(this) == true && DataContext is MainViewModel viewModel)
            {
                await viewModel.LoadMp3Async(dialog.FileName);
            }
        }

        private void ViewModelOnLocalTrackLoaded(object? sender, string filePath)
        {
            try
            {
                localAudioPlayer.Open(filePath);
                isLocalTrackLoaded = true;
                localAudioPlayer.Play();
                localPlaybackTimer.Start();
            }
            catch (Exception exception)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.StatusMessage = $"Could not open MP3 playback: {exception.Message}";
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            localAudioPlayer.Dispose();
            base.OnClosed(e);
        }

        private static void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainViewModel.IsDarkTheme) || sender is not MainViewModel viewModel)
            {
                return;
            }

            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var theme = new ResourceDictionary
            {
                Source = new Uri(
                    viewModel.IsDarkTheme ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml",
                    UriKind.Relative)
            };

            if (dictionaries.Count > 0)
            {
                dictionaries[0] = theme;
            }
        }
    }
}