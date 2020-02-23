using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using IFSEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using WpfDisplay.Helper;

namespace WpfDisplay.ViewModels
{
    public class PerformanceViewModel : ObservableObject
    {
        private readonly RendererGL renderer;

        private DispatcherTimer dt;
        private int fpsCounter = 0;
        private ulong lastTotalIters = 0;

        //TODO: add total render time, account for pauses
        public int Fps { get; private set; }
        public string IterationSpeed { get; private set; }
        public string TotalIterations { get; private set; }
        public double WorkgroupCount
        {
            get => renderer.workgroupCount;
            set
            {
                renderer.workgroupCount = (int)value;
                RaisePropertyChanged(() => WorkgroupCount);
            }
        }

        public bool UpdateDisplay
        {
            get => renderer.UpdateDisplayOnRender;
            set
            {
                renderer.UpdateDisplayOnRender = value;
                RaisePropertyChanged(() => UpdateDisplay);
            }
        }

        public PerformanceViewModel(RendererGL renderer)
        {
            this.renderer = renderer;
            renderer.DisplayFrameCompleted += (s, e) => fpsCounter++;
            dt = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (s, e) => UpdateValues(), Dispatcher.CurrentDispatcher);
            dt.Start();

        }

        public void UpdateValues()
        {
            TotalIterations = renderer.TotalIterations.ToKMB();
            Fps = fpsCounter;
            fpsCounter = 0;
            lastTotalIters = Math.Min(renderer.TotalIterations, lastTotalIters);
            IterationSpeed = (renderer.TotalIterations - lastTotalIters).ToKMB() + " /s";
            lastTotalIters = renderer.TotalIterations;
            RaisePropertyChanged(() => Fps);
            RaisePropertyChanged(() => IterationSpeed);
            RaisePropertyChanged(() => TotalIterations);
        }



    }
}
