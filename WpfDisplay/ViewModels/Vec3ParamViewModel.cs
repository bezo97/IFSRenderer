using IFSEngine.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Numerics;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    [ObservableObject]
    public partial class Vec3ParamViewModel : ParamViewModelBase<Vector3>
    {
        public Vec3ParamViewModel(string name, Iterator iterator, Workspace workspace) : base(name, iterator, workspace) { }

        public float ValueX
        {
            get => iterator.Vec3Params[Name].X;
            set
            {
                iterator.Vec3Params[Name] = new Vector3(value, iterator.Vec3Params[Name].Y, iterator.Vec3Params[Name].Z);
                OnPropertyChanged(nameof(ValueX));
                workspace.Renderer.InvalidateParamsBuffer();
            }
        }
        public float ValueY
        {
            get => iterator.Vec3Params[Name].Y;
            set
            {
                iterator.Vec3Params[Name] = new Vector3(iterator.Vec3Params[Name].X, value, iterator.Vec3Params[Name].Z);
                OnPropertyChanged(nameof(ValueY));
                workspace.Renderer.InvalidateParamsBuffer();
            }
        }
        public float ValueZ
        {
            get => iterator.Vec3Params[Name].Z;
            set
            {
                iterator.Vec3Params[Name] = new Vector3(iterator.Vec3Params[Name].X, iterator.Vec3Params[Name].Y, value);
                OnPropertyChanged(nameof(ValueZ));
                workspace.Renderer.InvalidateParamsBuffer();
            }
        }

    }
}
