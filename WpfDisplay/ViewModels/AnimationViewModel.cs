using IFSEngine.Animation;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class AnimationViewModel
{
    private readonly Workspace _workspace;

    private readonly Timer _realtimePlayer;
    public TimeOnly CurrentTime { get; set; }

    public AnimationViewModel(Workspace workspace)
    {
        this._workspace = workspace;
        workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
        _realtimePlayer = new Timer(TimeSpan.FromSeconds(1.0/workspace.Ifs.Dopesheet.Fps).TotalMilliseconds);//TODO: update interval when fps changes
        _realtimePlayer.Elapsed += OnPlayerTick;
        _realtimePlayer.AutoReset = true;
    }

    [ICommand]
    public void PlayPause()
    {
        _realtimePlayer.Enabled = !_realtimePlayer.Enabled;
    }

    [ICommand]
    public void JumpToStart()
    {
        _realtimePlayer.Stop();
        CurrentTime = TimeOnly.MinValue;
        _realtimePlayer.Start();
    }

    private void OnPlayerTick(object sender, ElapsedEventArgs e)
    {
        CurrentTime = CurrentTime.Add(TimeSpan.FromSeconds(1.0 / _workspace.Ifs.Dopesheet.Fps));
        _workspace.Ifs.Dopesheet.EvaluateAt(_workspace.Ifs, CurrentTime);
        //raise..
    }

}
