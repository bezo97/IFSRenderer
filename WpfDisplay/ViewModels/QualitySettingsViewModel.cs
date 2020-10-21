using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using IFSEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace WpfDisplay.ViewModels
{
    public class QualitySettingsViewModel : ObservableObject
    {
        private readonly RendererGL renderer;

        public QualitySettingsViewModel(RendererGL renderer)
        {
            this.renderer = renderer;
        }

        private RelayCommand _startRenderingCommand;
        public RelayCommand StartRenderingCommand
        {
            get => _startRenderingCommand ?? (
                _startRenderingCommand = new RelayCommand(() =>
                {
                    renderer.StartRendering();
                }));
        }

        public bool EnableDE
        {
            get => renderer.EnableDE;
            set
            {
                renderer.EnableDE = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DEPanelVisibility));
                renderer.UpdateDisplay();
            }
        }
        public Visibility DEPanelVisibility => EnableDE ? Visibility.Visible : Visibility.Collapsed;

        public int DEMaxRadius
        {
            get => renderer.DEMaxRadius;
            set
            {
                renderer.DEMaxRadius = value;
                RaisePropertyChanged();
                renderer.UpdateDisplay();
            }
        }

        public double DEThreshold
        {
            get => renderer.DEThreshold;
            set
            {
                renderer.DEThreshold = value;
                RaisePropertyChanged();
                renderer.UpdateDisplay();
            }
        }

        public double DEPower
        {
            get => renderer.DEPower;
            set
            {
                renderer.DEPower = value;
                RaisePropertyChanged();
                renderer.UpdateDisplay();
            }
        }

        public bool EnableTAA
        {
            get => renderer.EnableTAA;
            set
            {
                renderer.EnableTAA = value;
                RaisePropertyChanged();
                renderer.UpdateDisplay();
            }
        }

        public bool EnablePerceptualUpdates
        {
            get => renderer.EnablePerceptualUpdates;
            set
            {
                renderer.EnablePerceptualUpdates = value;
                RaisePropertyChanged();
                renderer.UpdateDisplay();
            }
        }

        public int EntropyInv
        {
            get => (int)(1.0 / renderer.Entropy);
            set
            {
                renderer.Entropy = 1.0 / value;
                RaisePropertyChanged();
                renderer.InvalidateAccumulation();
            }
        }

        public int PassIters
        {
            get => renderer.PassIters;
            set
            {
                renderer.PassIters = value;
                RaisePropertyChanged();
            }
        }

        public int Warmup
        {
            get => renderer.Warmup;
            set
            {
                renderer.Warmup = value;
                RaisePropertyChanged();
                renderer.InvalidateAccumulation();
            }
        }

        public int MaxFilterRadius
        {
            get => renderer.MaxFilterRadius;
            set
            {
                renderer.MaxFilterRadius = value;
                RaisePropertyChanged();
                RaisePropertyChanged(()=>FilterText);
                renderer.InvalidateAccumulation();
            }
        }

        public string FilterText => "Max Filter Radius" + (MaxFilterRadius > 1 ? "" : " (Off)");

        private RelayCommand _previewPresetCommand;
        public RelayCommand PreviewPresetCommand
        {
            get => _previewPresetCommand ?? (
                _previewPresetCommand = new RelayCommand(async () =>
                {
                    await renderer.SetHistogramScaleToDisplay();
                    //EnableDE = true;
                    //EnableTAA = true;
                    //EnablePerceptualUpdates = false;
                    //PassIters = 100;
                    //Warmup = 10;
                    EntropyInv = 100;
                    MaxFilterRadius = 1;
                }));
        }

        private RelayCommand _finalPresetCommand;
        public RelayCommand FinalPresetCommand
        {
            get => _finalPresetCommand ?? (
                _finalPresetCommand = new RelayCommand(async () =>
                {
                    EnablePerceptualUpdates = true;
                    EnableTAA = false;
                    EnableDE = false;
                    //PassIters = 500;
                    //Warmup = 30;
                    EntropyInv = 10000;
                    MaxFilterRadius = 3;
                    await renderer.SetHistogramScale(1.0);
                }));
        }


    }
}
