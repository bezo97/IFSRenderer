using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using WpfDisplay.Properties;

namespace WpfDisplay.ViewModels
{
    internal class SettingsViewModel : ObservableObject
    {
        private MainViewModel mainvm;
        public event EventHandler SettingsSaved;
        public event EventHandler SettingsCanceled;

        public SettingsViewModel(MainViewModel mainvm)
        {
            this.mainvm = mainvm;
        }

        private RelayCommand okDialogCommand;
        public ICommand OkDialogCommand => okDialogCommand ??= new RelayCommand(OkDialog);
        private void OkDialog()
        {
            Settings.Default.Save();//writes user.config in AppData
            mainvm.workspace.Renderer.EnablePerceptualUpdates = PerceptuallyUniformUpdates ?? false;
            mainvm.workspace.Renderer.SetWorkgroupCount(WorkgroupCount).Wait();
            mainvm.workspace.Renderer.TargetFramerate = TargetFramerate;
            SettingsSaved?.Invoke(this, null);
        }

        private RelayCommand cancelDialogCommand;
        public ICommand CancelDialogCommand => cancelDialogCommand ??= new RelayCommand(CancelDialog);
        private void CancelDialog()
        {
            Settings.Default.Reload();
            OnPropertyChanged(string.Empty);
            SettingsCanceled?.Invoke(this, null);
        }

        public string ArtistName
        {
            get => Settings.Default.ArtistName;
            set
            {
                Settings.Default.ArtistName = value;
                OnPropertyChanged(nameof(ArtistName));
            }
        }

        public string ArtistLink
        {
            get => Settings.Default.ArtistLink;
            set
            {
                Settings.Default.ArtistLink = value;
                OnPropertyChanged(nameof(ArtistLink));
            }
        }

        public bool? Watermark
        {
            get => Settings.Default.ApplyWatermark;
            set
            {
                Settings.Default.ApplyWatermark = value ?? false;
                OnPropertyChanged(nameof(Watermark));
            }
        }

        public bool? Notifications
        {
            get => Settings.Default.NotifyRenderFinished;
            set
            {
                Settings.Default.NotifyRenderFinished = value ?? false;
                OnPropertyChanged(nameof(Notifications));
            }
        }

        public bool? PerceptuallyUniformUpdates
        {
            get => Settings.Default.PerceptuallyUniformUpdates;
            set
            {
                Settings.Default.PerceptuallyUniformUpdates = value ?? false;
                OnPropertyChanged(nameof(PerceptuallyUniformUpdates));
            }
        }

        public int TargetFramerate
        {
            get => Settings.Default.TargetFramerate;
            set
            {
                Settings.Default.TargetFramerate = value;
                OnPropertyChanged(nameof(TargetFramerate));
            }
        }

        public int WorkgroupCount
        {
            get => Settings.Default.WorkgroupCount;
            set
            {
                Settings.Default.WorkgroupCount = value;
                OnPropertyChanged(nameof(WorkgroupCount));
            }
        }
    }
}