using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using WpfDisplay.Properties;

namespace WpfDisplay.ViewModels
{
    [ObservableObject]
    internal partial class SettingsViewModel
    {
        private MainViewModel mainvm;
        public event EventHandler SettingsSaved;
        public event EventHandler SettingsCanceled;

        public SettingsViewModel(MainViewModel mainvm)
        {
            this.mainvm = mainvm;
        }

        [ICommand]
        private void OkDialog()
        {
            Settings.Default.Save();//writes user.config in AppData
            mainvm.workspace.LoadUserSettings();
            SettingsSaved?.Invoke(this, null);
        }

        [ICommand]
        private void CancelDialog()
        {
            Settings.Default.Reload();
            OnPropertyChanged(string.Empty);
            SettingsCanceled?.Invoke(this, null);
        }

        public string AuthorName
        {
            get => Settings.Default.AuthorName;
            set
            {
                Settings.Default.AuthorName = value;
                OnPropertyChanged(nameof(AuthorName));
            }
        }

        public string AuthorLink
        {
            get => Settings.Default.AuthorLink;
            set
            {
                Settings.Default.AuthorLink = value;
                OnPropertyChanged(nameof(AuthorLink));
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

        public bool? InvertAxisX
        {
            get => Settings.Default.InvertAxisX;
            set
            {
                Settings.Default.InvertAxisX = value ?? false;
                OnPropertyChanged(nameof(InvertAxisX));
            }
        }

        public bool? InvertAxisY
        {
            get => Settings.Default.InvertAxisY;
            set
            {
                Settings.Default.InvertAxisY = value ?? false;
                OnPropertyChanged(nameof(InvertAxisY));
            }
        }

        public bool? InvertAxisZ
        {
            get => Settings.Default.InvertAxisZ;
            set
            {
                Settings.Default.InvertAxisZ = value ?? false;
                OnPropertyChanged(nameof(InvertAxisZ));
            }
        }

        public double Sensitivity
        {
            get => Settings.Default.Sensitivity;
            set
            {
                Settings.Default.Sensitivity = value;
                OnPropertyChanged(nameof(Sensitivity));
            }
        }
    }
}
