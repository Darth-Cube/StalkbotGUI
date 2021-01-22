﻿using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using StalkbotGUI.Stalkbot.Discord;
using StalkbotGUI.Stalkbot.Utilities;
using StalkbotGUI.Stalkbot.Utilities.UI;
using ProgressBar = StalkbotGUI.Stalkbot.Utilities.UI.ProgressBar;

namespace StalkbotGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Client responsible for everything related to "stalking"
        /// </summary>
        private readonly StalkbotClient _client;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Logger.Init(ref LogText, ref LogView);
            Task.Delay(1000);
            CheckRequirements();
            _client = new StalkbotClient();
            if (Config.Instance.AutoStartDiscord)
                new Action(() => OnOffButton_Click(null, null))();
            
            InitButtons();
        }
        
        #region ButtonHandlers
        /// <summary>
        /// Handles clicking the off/on button
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">Event args</param>
        private async void OnOffButton_Click(object sender, RoutedEventArgs e)
        {
            await _client.StartStopDiscord();
            OnOffButton.Background = new SolidColorBrush(_client.IsRunning ? Colors.DarkGreen : Colors.DarkRed);
            OnOffButton.Content = _client.IsRunning ? "On" : "Off";
        }
        /// <summary>
        /// Handles clicking the webcam toggle button
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">Event args</param>
        private void WebcamToggle_Click(object sender, RoutedEventArgs e)
        {
            Config.Instance.CamEnabled = !Config.Instance.CamEnabled;
            Logger.Log($"Webcam: {Config.Instance.CamEnabled}", LogLevel.Info);
            UiHelpers.UpdateButton("webcam", ref WebcamToggle);
        }

        /// <summary>
        /// Handles clicking the screenshot toggle button
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">Event args</param>
        private void ScreenshotToggle_Click(object sender, RoutedEventArgs e)
        {
            Config.Instance.SsEnabled = !Config.Instance.SsEnabled;
            Logger.Log($"Screenshot: {Config.Instance.SsEnabled}", LogLevel.Info);
            UiHelpers.UpdateButton("screenshot", ref ScreenshotToggle);
        }

        /// <summary>
        /// Handles clicking the TTS toggle button
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">Event args</param>
        private void TtsToggle_Click(object sender, RoutedEventArgs e)
        {
            Config.Instance.TtsEnabled = !Config.Instance.TtsEnabled;
            Logger.Log($"Text to Speech: {Config.Instance.TtsEnabled}", LogLevel.Info);
            UiHelpers.UpdateButton("tts", ref TtsToggle);
        }

        /// <summary>
        /// Handles clicking the play toggle button
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">Event args</param>
        private void PlayToggle_Click(object sender, RoutedEventArgs e)
        {
            Config.Instance.PlayEnabled = !Config.Instance.PlayEnabled;
            Logger.Log($"Playsound: {Config.Instance.PlayEnabled}", LogLevel.Info);
            UiHelpers.UpdateButton("play", ref PlayToggle);
        }

        /// <summary>
        /// Handles clicking the processes toggle button
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">Event args</param>
        private void ProcToggle_Click(object sender, RoutedEventArgs e)
        {
            Config.Instance.ProcessesEnabled = !Config.Instance.ProcessesEnabled;
            Logger.Log($"Processes: {Config.Instance.ProcessesEnabled}", LogLevel.Info);
            UiHelpers.UpdateButton("proc", ref ProcToggle);
        }

        /// <summary>
        /// Handles clicking the clipboard toggle button
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">Event args</param>
        private void ClipboardToggle_Click(object sender, RoutedEventArgs e)
        {
            Config.Instance.ClipboardEnabled = !Config.Instance.ClipboardEnabled;
            Logger.Log($"Clipboard: {Config.Instance.ClipboardEnabled}", LogLevel.Info);
            UiHelpers.UpdateButton("clipboard", ref ClipboardToggle);
        }

        /// <summary>
        /// Handles clicking the undo button
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">Event args</param>
        private async void UndoButton_Click(object sender, RoutedEventArgs e)
            => await _client.DeleteLastMessage();

        /// <summary>
        /// Handles clicking the config button
        /// </summary>
        /// <param name="sender">Button object</param>
        /// <param name="e">Event args</param>
        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var cfg = new ConfigWindow();
            cfg.Show();
        }

        #endregion

        #region Utilities
        /// <summary>
        /// Colors the UI toggle buttons
        /// </summary>
        private void InitButtons()
        {
            UiHelpers.UpdateButton("webcam", ref WebcamToggle);
            UiHelpers.UpdateButton("screenshot", ref ScreenshotToggle);
            UiHelpers.UpdateButton("play", ref PlayToggle);
            UiHelpers.UpdateButton("tts", ref TtsToggle);
            UiHelpers.UpdateButton("proc", ref ProcToggle);
            UiHelpers.UpdateButton("clipboard", ref ClipboardToggle);
        }

        /// <summary>
        /// Checks for a config and ffmpeg
        /// </summary>
        private async void CheckRequirements()
        {
            // Check for config.json
            if (!File.Exists("config.json"))
            {
                Logger.Log("config.json was not found, creating a default one", LogLevel.Warning);
                Config.Instance.SaveConfig();
            }

            if (File.Exists("ffmpeg.exe")) return;
            Logger.Log("ffmpeg was not found in directory, downloading it...", LogLevel.Warning);
            if (!await DownloadFfmpeg())
                Logger.Log("Error downloading ffmpeg, please try starting again or download it manually", LogLevel.Error);
            else
                Logger.Log("Successfully downloaded ffmpeg!", LogLevel.Info);
        }

        /// <summary>
        /// Downloads ffmpeg with progress bar
        /// </summary>
        /// <returns>Whether the download was successful</returns>
        private async Task<bool> DownloadFfmpeg()
        {
            var error = false;
            await Dispatcher.Invoke(async () =>
            {
                var pbw = new ProgressBar();
                pbw.Show();

                using (var client = new WebClient())
                {
                    client.DownloadProgressChanged += (sender, args) => pbw.UpdateProgress(args.ProgressPercentage);
                    client.DownloadDataCompleted += (sender, args) => error = args.Cancelled || args.Error != null;
                    await client.DownloadFileTaskAsync(new Uri("https://timostestdoma.in/res/ffmpeg.exe"), "ffmpeg.exe");
                }
            });
            return !error;
        }
        #endregion
    }
}
