using GalaSoft.MvvmLight;
using IFSEngine;
using IFSEngine.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WpfDisplay.Models
{
    /// <summary>
    /// The main workspace model that contains a <see cref="RendererGL"/> 
    /// and an <see cref="IFSEngine.Model.IFS"/> that it is rendering.
    /// </summary>
    public class Workspace : ObservableObject
    {
        private List<TransformFunction> loadedTransforms = new List<TransformFunction>();
        public IReadOnlyCollection<TransformFunction> LoadedTransforms => loadedTransforms;

        public readonly string TransformsDirectoryPath = @".\Functions\Transforms";

        private RendererGL renderer;
        public RendererGL Renderer
        {
            get { return renderer; }
            set {
                Set(ref renderer, value);
                if(ifs != null)
                    renderer.LoadParams(ifs);
            }
        }


        private IFS ifs;
        public IFS IFS
        {
            get { return ifs; }
            set {
                Set(ref ifs, value);
                renderer?.LoadParams(ifs);
            }
        }

        public Workspace(RendererGL r)
        {
            LoadTransformLibrary();
            Renderer = r;
            Renderer.Initialize(loadedTransforms);
            IFS = IFS.GenerateRandom(loadedTransforms);
        }

        public async Task ReloadTransforms()
        {
            LoadTransformLibrary();
            IFS.ReloadTransforms(loadedTransforms);
            await Renderer.LoadTransforms(loadedTransforms);
            RaisePropertyChanged(() => LoadedTransforms);
        }

        private void LoadTransformLibrary()
        {
            loadedTransforms = Directory
                .GetFiles(TransformsDirectoryPath)
                .Select(file => TransformFunction.FromFile(file))
                .ToList();
            RaisePropertyChanged(() => LoadedTransforms);
        }

    }
}
