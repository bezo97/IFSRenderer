using GalaSoft.MvvmLight;
using IFSEngine.Model;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class VariableViewModel : ObservableObject
    {
        private readonly Iterator iterator;
        private readonly Workspace workspace;

        public string Name { get; private set; }
        public double Value
        {
            get => iterator.TransformVariables[Name];
            set
            {
                iterator.TransformVariables[Name] = value;
                RaisePropertyChanged();
                workspace.Renderer.InvalidateParams();
            }
        }

        //TODO: min max increment

        public VariableViewModel(string name, Iterator iterator, Workspace workspace)
        {
            this.Name = name;
            this.iterator = iterator;
            this.workspace = workspace;
        }
    }
}
