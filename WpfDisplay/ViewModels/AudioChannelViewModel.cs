#nullable enable
using Cavern.Utilities;
using IFSEngine.Animation;
using IFSEngine.Animation.ChannelDrivers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class AudioChannelViewModel
{
    //private readonly Workspace _workspace;
    private readonly AnimationViewModel _vm;
    private readonly AudioChannelDriver _channelDriver;

    public AudioChannelViewModel(AnimationViewModel vm, AudioChannelDriver channelDriver)
    {
        //_workspace = workspace;
        _vm = vm;
        _channelDriver = channelDriver;
        _channelDriver.SetSamplerFunction(Sampler);
    }

    private float Sampler(AudioChannelDriver d, double t)
    {
        if (_vm.LoadedAudioClip is null)
            return 0.0f;
        return CavernHelper.CavernSampler(_vm.LoadedAudioClip, _vm.AudioClipCache!, d, t);
    }

}
