using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using YoutubeExtractor;
using System.Windows.Threading;

namespace YoutubeDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        bool videoWorking = false;
        bool audioWorking = false;

        VideoInfo selectedVideoInfo;
        List<VideoInfo> videos = new List<VideoInfo>();

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                videos = DownloadUrlResolver.GetDownloadUrls(tbxYoutubeLink.Text).ToList();
                listBoxVideos.ItemsSource = videos;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Varning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void listBoxVideos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedVideoInfo = (VideoInfo)listBoxVideos.SelectedItem as VideoInfo;

            tbxTitle.Text = selectedVideoInfo.Title;
            lblRes.Content = selectedVideoInfo.Resolution;
            lblVideoExtension.Content = selectedVideoInfo.VideoExtension;
            lblAudioBit.Content = selectedVideoInfo.AudioBitrate;
            lblAudioExtension.Content = selectedVideoInfo.AudioExtension;

            if (selectedVideoInfo.CanExtractAudio)
            {
                cbxExtractAudio.IsEnabled = true;
            }
            else
            {
                cbxExtractAudio.IsEnabled = false;
                cbxExtractAudio.IsChecked = false;
            }
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (selectedVideoInfo == null)
            {
                MessageBox.Show("No video/audio version selected.", "Varning!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {

                string saveFolderPath = null;
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    saveFolderPath = fbd.SelectedPath;
                    VideoInfo video = selectedVideoInfo;

                    if (cbxDownloadVideo.IsChecked == true)
                    {
                        string filePath = System.IO.Path.Combine(saveFolderPath, video.Title + video.VideoExtension);

                        if (!File.Exists(filePath))
                        {
                            var videoDownloader = new VideoDownloader(video, filePath);
                            videoWorking = true;
                            btnDownload.IsEnabled = false;

                            ThreadStart t = delegate()
                            {
                                videoDownloader.DownloadProgressChanged += (sen, args) =>
                                {
                                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action<double>(UpdateVideoDownloadProgressbar), args.ProgressPercentage);
                                };

                                videoDownloader.DownloadFinished += (sen, args) =>
                                {
                                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action<EventArgs, bool, bool>(DownloadingDone), args, false, true);
                                };

                                videoDownloader.Execute();
                            };

                            new Thread(t).Start();
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("Error! File already exist!");
                            sb.AppendLine(filePath);

                            MessageBox.Show(sb.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    if (cbxExtractAudio.IsChecked == true && video.CanExtractAudio == true)
                    {
                        string filePath = System.IO.Path.Combine(saveFolderPath, video.Title + video.AudioExtension);
                        if (!File.Exists(filePath))
                        {
                            var audioDownloader = new AudioDownloader(video, filePath);
                            audioWorking = true;
                            btnDownload.IsEnabled = false;

                            ThreadStart t = delegate()
                            {
                                audioDownloader.DownloadProgressChanged += (sen, args) =>
                                {
                                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action<double>(UpdateAudioDownloadProgressbar), args.ProgressPercentage);
                                };

                                audioDownloader.AudioExtractionProgressChanged += (sen, args) =>
                                {
                                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action<double>(UpdateAudioExtractionsProgressbar), args.ProgressPercentage);
                                };

                                audioDownloader.DownloadFinished += (sen, args) =>
                                {
                                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action<EventArgs, bool, bool>(DownloadingDone), args, true, false);
                                };

                                audioDownloader.Execute();
                            };

                            new Thread(t).Start();
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("Error! File already exist!");
                            sb.AppendLine(filePath);

                            MessageBox.Show(sb.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }            
        }

        private void UpdateVideoDownloadProgressbar(double value)
        {
            progressbarVideo.Value = value;
        }

        private void UpdateAudioDownloadProgressbar(double value)
        {
            progressbarAudio.Value = value;
        }

        private void UpdateAudioExtractionsProgressbar(double value)
        {
            progressbarExAudio.Value = value;
        }

        private void DownloadingDone(EventArgs args, bool fromAudio, bool fromVideo)
        {
            if (fromAudio)
            {
                audioWorking = false;
                UpdateAudioDownloadProgressbar(0);
                UpdateAudioExtractionsProgressbar(0);
            }

            if (fromVideo)
            {
                videoWorking = false;
                UpdateVideoDownloadProgressbar(0);
            }

            if (!videoWorking && !audioWorking)
            {
                btnDownload.IsEnabled = true;
            }
        }


    }
}
