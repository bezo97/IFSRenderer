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
    private readonly FFTCache _cache;

    public AudioChannelViewModel(AnimationViewModel vm, AudioChannelDriver channelDriver)
    {
        //_workspace = workspace;
        _vm = vm;
        _channelDriver = channelDriver;
        _cache = new FFTCache(512);
        _channelDriver.SetSamplerFunction(Sampler);
    }

    private float Sampler(AudioChannelDriver d, double t)
    {
        if (_vm.Clip is null)
            return 0.0f;
        return CavernHelper.CavernSampler(_vm.Clip, _cache, d, t);
    }

}
