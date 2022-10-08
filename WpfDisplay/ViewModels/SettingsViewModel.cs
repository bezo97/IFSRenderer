using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfDisplay.Properties;

namespace WpfDisplay.ViewModels;

[ObservableObject]
internal partial class SettingsViewModel
{
    private readonly MainViewModel _mainvm;
    public event EventHandler SettingsSaved;
    public event EventHandler SettingsCanceled;

    public SettingsViewModel(MainViewModel mainvm)
    {
        _mainvm = mainvm;
    }

    [RelayCommand]
    private async Task OkDialog()
    {
        Settings.Default.Save();//writes user.config in AppData

        await _mainvm.workspace.LoadUserSettings();

        SettingsSaved?.Invoke(this, null);
    }

    [RelayCommand]
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

    private ValueSliderViewModel _targetFramerate;
    public ValueSliderViewModel TargetFramerate => _targetFramerate ??= new ValueSliderViewModel(_mainvm)
    {
        ToolTip = "Define a target framerate (FPS) for interactive exploration. Default value is 60.",
        DefaultValue = 60,
        GetV = () => Settings.Default.TargetFramerate,
        SetV = (value) =>
        {
            Settings.Default.TargetFramerate = (int)value;
        },
        MinValue = 5,
        MaxValue = 144,
        Increment = 30,
    };

    private ValueSliderViewModel _workgroupCount;
    public ValueSliderViewModel WorkgroupCount => _workgroupCount ??= new ValueSliderViewModel(_mainvm)
    {
        ToolTip = "Number of workgroups to be dispatched. Each workgroup consists of 64 kernel invocations. Default value is 256.",
        DefaultValue = 256,
        GetV = () => Settings.Default.WorkgroupCount,
        SetV = (value) =>
        {
            Settings.Default.WorkgroupCount = (int)value;
        },
        MinValue = 1,
        MaxValue = 5000,
        Increment = 100,
    };

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

    private ValueSliderViewModel _sensitivity;
    public ValueSliderViewModel Sensitivity => _sensitivity ??= new ValueSliderViewModel(_mainvm)
    {
        DefaultValue = 1,
        GetV = () => Settings.Default.Sensitivity,
        SetV = (value) =>
        {
            Settings.Default.Sensitivity = value;
        },
        MinValue = 0.1,
        MaxValue = 10.0,
        Increment = 0.1,
    };

}
