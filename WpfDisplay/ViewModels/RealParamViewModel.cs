using IFSEngine.Model;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class RealParamViewModel : ParamViewModelBase<double>
    {
        public RealParamViewModel(string name, Iterator iterator, Workspace workspace) : base(name, iterator, workspace) { }

        public double Value
        {
            get => iterator.RealParams[Name];
            set
            {
                iterator.RealParams[Name] = value;
                OnPropertyChanged(nameof(Value));
                workspace.Renderer.InvalidateParamsBuffer();
            }
        }

    }
}
