using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows.Input;

namespace WpfDisplay.ViewModels
{
    internal class SettingsViewModel : ObservableObject
    {
        private MainViewModel mainvm;

        public SettingsViewModel(MainViewModel mainvm)
        {
            this.mainvm = mainvm;
        }

        private RelayCommand okDialogCommand;
        public ICommand OkDialogCommand => okDialogCommand ??= new RelayCommand(OkDialog);

        private void OkDialog()
        {
        }

        private RelayCommand cancelDialogCommand;
        public ICommand CancelDialogCommand => cancelDialogCommand ??= new RelayCommand(CancelDialog);

        private void CancelDialog()
        {
        }

        private string artistName;

        public string ArtistName { get => artistName; set => SetProperty(ref artistName, value); }

        private string artistLink;

        public string ArtistLink { get => artistLink; set => SetProperty(ref artistLink, value); }

        private bool? watermark;

        public bool? Watermark { get => watermark; set => SetProperty(ref watermark, value); }

        private bool? notifications;

        public bool? Notifications { get => notifications; set => SetProperty(ref notifications, value); }

        private bool? perceptuallyUniformUpdates;

        public bool? PerceptuallyUniformUpdates { get => perceptuallyUniformUpdates; set => SetProperty(ref perceptuallyUniformUpdates, value); }
    }
}