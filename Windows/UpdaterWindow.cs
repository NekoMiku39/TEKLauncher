﻿using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using TEKLauncher.Net;
using static System.Diagnostics.Process;
using static System.IO.File;
using static System.Windows.Application;
using static System.Windows.Media.Brushes;
using static TEKLauncher.App;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Windows
{
    public partial class UpdaterWindow : Window
    {
        internal UpdaterWindow()
        {
            InitializeComponent();
            ProgressBar.ProgressUpdated = ProgressUpdatedHandler;
        }
        private void DownloadBeganHandler()
        {
            TaskbarItemInfo.ProgressState = ProgressBar.Progress.Total < 1L ? TaskbarItemProgressState.Indeterminate : TaskbarItemProgressState.Normal;
            Status.Text = "Downloading update";
        }
        private void DownloadManually(object Sender, RoutedEventArgs Args)
        {
            Execute(GDriveLauncherFile);
            Current.Shutdown();
        }
        private void Drag(object Sender, MouseButtonEventArgs Args) => DragMove();
        private void Install(string Executable)
        {
            if (FileExists($"{Executable}.old"))
                Delete($"{Executable}.old");
            Move(Executable, $"{Executable}.old");
            Move($"{Executable}.new", Executable);
            Instance.PipeServer?.Close();
            Execute(Executable);
            Current.Shutdown();
        }
        private async void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            string Executable;
            using (Process CurrentProcess = GetCurrentProcess())
                Executable = CurrentProcess.MainModule.FileName;
            if (await new Downloader(ProgressBar.Progress) { DownloadBegan = DownloadBeganHandler }.TryDownloadFileAsync($"{Executable}.new", $"{Seedbox}TEKLauncher/TEKLauncher.exe", GDriveLauncherFile))
                Install(Executable);
            else
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                Status.Foreground = DarkRed;
                Status.Text = "Both download methods failed, download new version manually ";
                Hyperlink Link = new Hyperlink { Foreground = (SolidColorBrush)FindResource("CyanBrush") };
                Link.Inlines.Add("here");
                Link.Click += DownloadManually;
                Status.Inlines.Add(Link);
            }
        }
        private void ProgressUpdatedHandler()
        {
            if (ProgressBar.Progress.Total > 0L)
                TaskbarItemInfo.ProgressValue = ProgressBar.Progress.Ratio;
        }
    }
}