using GalaSoft.MvvmLight;
using IFSEngine.Model;

namespace WpfDisplay.ViewModels
{
    public class VariableViewModel : ObservableObject
    {
        private readonly Iterator iterator;

        public string Name { get; private set; }
        public double Value
        {
            get => iterator.TransformVariables[Name];
            set
            {
                iterator.TransformVariables[Name] = value;
                RaisePropertyChanged();
                RaisePropertyChanged("InvalidateParams");
            }
        }

        //TODO: min max increment

        public VariableViewModel(string name, Iterator iterator)
        {
            this.Name = name;
            this.iterator = iterator;
        }
    }
}
