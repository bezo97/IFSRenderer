using IFSEngine.Animation;
using IFSEngine.Animation.ChannelDrivers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class AudioChannelViewModel
{
    private readonly Workspace _workspace;
    private readonly AudioChannelDriver _channelDriver;

    public AudioChannelViewModel(Workspace workspace, AudioChannelDriver channelDriver)
    {
        _workspace = workspace;
        _channelDriver = channelDriver;
    }
}
