using GalaSoft.MvvmLight;
using System;
using System.Windows.Threading;
using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class PerformanceViewModel : ObservableObject
    {
        private readonly Workspace workspace;
        private DispatcherTimer dt;
        private int fpsCounter = 0;
        private ulong lastTotalIters = 0;

        //TODO: add total render time, account for pauses
        public int Fps { get; private set; }
        public string IterationSpeed { get; private set; }
        public string TotalIterations { get; private set; }
        public double WorkgroupCount
        {
            get => workspace.Renderer.WorkgroupCount;
            set
            {
                workspace.Renderer.setWorkgroupCount((int)value).Wait();
                RaisePropertyChanged(() => WorkgroupCount);
            }
        }

        public bool UpdateDisplay
        {
            get => workspace.Renderer.UpdateDisplayOnRender;
            set
            {
                workspace.Renderer.UpdateDisplayOnRender = value;
                RaisePropertyChanged(() => UpdateDisplay);
            }
        }

        public int PassIters
        {
            get => workspace.Renderer.PassIters;
            set
            {
                workspace.Renderer.PassIters = value;
                RaisePropertyChanged(() => PassIters);
            }
        }

        public PerformanceViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => RaisePropertyChanged(string.Empty);
            workspace.Renderer.DisplayFramebufferUpdated += (s, e) => fpsCounter++;
            dt = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (s, e) => UpdateValues(), Dispatcher.CurrentDispatcher);
            dt.Start();
        }

        public void UpdateValues()
        {
            TotalIterations = workspace.Renderer.TotalIterations.ToKMB();
            Fps = fpsCounter;
            fpsCounter = 0;
            lastTotalIters = Math.Min(workspace.Renderer.TotalIterations, lastTotalIters);
            IterationSpeed = (workspace.Renderer.TotalIterations - lastTotalIters).ToKMB() + " /s";
            lastTotalIters = workspace.Renderer.TotalIterations;
            RaisePropertyChanged(() => Fps);
            RaisePropertyChanged(() => IterationSpeed);
            RaisePropertyChanged(() => TotalIterations);
        }



    }
}
