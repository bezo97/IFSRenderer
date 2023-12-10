using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfDisplay.Helper;
using WpfDisplay.Properties;

namespace WpfDisplay.ViewModels;

internal partial class SettingsViewModel : ObservableObject
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

        await _mainvm.workspace.ApplyUserSettings();

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

    public bool? UseWhiteForBlankParams
    {
        get => Settings.Default.UseWhiteForBlankParams;
        set
        {
            Settings.Default.UseWhiteForBlankParams = value ?? false;
            OnPropertyChanged(nameof(UseWhiteForBlankParams));
        }
    }

    public bool? IsWelcomeShownOnStartup
    {
        get => Settings.Default.IsWelcomeShownOnStartup;
        set
        {
            Settings.Default.IsWelcomeShownOnStartup = value ?? true;
            OnPropertyChanged(nameof(IsWelcomeShownOnStartup));
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

    private ValueSliderSettings _targetFramerate;
    public ValueSliderSettings TargetFramerateSlider => _targetFramerate ??= new()
    {
        ToolTip = "Define a target framerate (FPS) for interactive exploration. Default value is 60.",
        DefaultValue = 60,
        MinValue = 5,
        MaxValue = 144,
        Increment = 30,
    };

    public int WorkgroupCount
    {
        get => Settings.Default.WorkgroupCount;
        set
        {
            Settings.Default.WorkgroupCount = value;
            OnPropertyChanged(nameof(WorkgroupCount));
        }
    }

    private ValueSliderSettings _workgroupCount;
    public ValueSliderSettings WorkgroupCountSlider => _workgroupCount ??= new()
    {
        ToolTip = "Number of workgroups to be dispatched. Each workgroup consists of 64 kernel invocations. Default value is 256.",
        DefaultValue = 256,
        MinValue = 1,
        MaxValue = 5000,
        Increment = 100,
    };

    public bool? InvertAxisY
    {
        get => Settings.Default.InvertAxisY;
        set
        {
            Settings.Default.InvertAxisY = value ?? false;
            OnPropertyChanged(nameof(InvertAxisY));
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

    private ValueSliderSettings _sensitivity;
    public ValueSliderSettings SensitivitySlider => _sensitivity ??= new()
    {
        DefaultValue = 1,
        ToolTip = "Fine-tune the sensitivity of camera movement controls. This applies to mouse, keyboard and gamepad thumbsticks. Default value is 1.",
        MinValue = 0.1,
        MaxValue = 10.0,
        Increment = 0.1,
    };

    public bool? IsExportVideoFileEnabled
    {
        get => Settings.Default.IsExportVideoFileEnabled;
        set
        {
            Settings.Default.IsExportVideoFileEnabled = value ?? false;
            OnPropertyChanged(nameof(IsExportVideoFileEnabled));
        }
    }

    public string FfmpegPath
    {
        get => Settings.Default.FfmpegPath;
        set
        {
            Settings.Default.FfmpegPath = value;
            OnPropertyChanged(nameof(FfmpegPath));
            OnPropertyChanged(nameof(IsFfmpegPathSet));
        }
    }

    public bool IsFfmpegPathSet => !string.IsNullOrEmpty(FfmpegPath);

    public string FfmpegArgs
    {
        get => Settings.Default.FfmpegArgs;
        set
        {
            Settings.Default.FfmpegArgs = value;
            OnPropertyChanged(nameof(FfmpegArgs));
        }
    }

    [RelayCommand]
    private void ShowFfmpegPathSelectorDialog()
    {
        if (DialogHelper.ShowFfmpegPathSelectorDialog(out string selectedPath))
            FfmpegPath = selectedPath;
    }

    public bool? IsRawFrameExportEnabled
    {
        get => Settings.Default.IsRawFrameExportEnabled;
        set
        {
            Settings.Default.IsRawFrameExportEnabled = value ?? false;
            OnPropertyChanged(nameof(IsRawFrameExportEnabled));
        }
    }

    public IReadOnlyDictionary<string, string> FfmpegPresets => _mainvm.workspace.FfmpegPresets;

    [RelayCommand]
    private void ApplyFfmpegPreset(string args)
    {
        FfmpegArgs = args;
    }

}
