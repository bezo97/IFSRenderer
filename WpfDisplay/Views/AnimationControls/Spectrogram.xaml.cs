using Cavern.QuickEQ;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDisplay.Helper;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views.AnimationControls;

/// <summary>
/// Interaction logic for Spectrogram.xaml
/// </summary>
public partial class Spectrogram : UserControl
{

    public Spectrogram()
    {
        InitializeComponent();
    }

    //TODO: on click: log scale -> linear scale, SweepGenerator.ExponentialFreqs(4, endFreq, samples.Length / 2);

}
