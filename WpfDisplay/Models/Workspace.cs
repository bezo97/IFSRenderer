using IFSEngine.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using IFSEngine.Rendering;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WpfDisplay.Helper;

namespace WpfDisplay.Models
{
    /// <summary>
    /// The main workspace model that contains a <see cref="RendererGL"/> 
    /// and an <see cref="IFSEngine.Model.IFS"/> that it is rendering.
    /// </summary>
    public class Workspace : ObservableObject
    {
        private readonly IFSHistoryTracker tracker = new();
        private List<TransformFunction> loadedTransforms = new List<TransformFunction>();

        public IReadOnlyCollection<TransformFunction> LoadedTransforms => loadedTransforms;

        public readonly string TransformsDirectoryPath = @".\Functions\Transforms";

        private RendererGL renderer;
        public RendererGL Renderer
        {
            get => renderer;
            set
            {
                SetProperty(ref renderer, value);
                if (ifs != null)
                    renderer.LoadParams(ifs);
            }
        }


        private IFS ifs;
        public IFS IFS
        {
            get => ifs;
            set
            {
                SetProperty(ref ifs, value);
                renderer?.LoadParams(ifs);
                //ClearHistory();
            }
        }

        public bool IsHistoryUndoable => tracker.IsHistoryUndoable;
        public bool IsHistoryRedoable => tracker.IsHistoryRedoable;

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
            OnPropertyChanged(nameof(LoadedTransforms));
        }

        private void LoadTransformLibrary()
        {
            loadedTransforms = Directory
                .GetFiles(TransformsDirectoryPath)
                .Select(file => TransformFunction.FromFile(file))
                .ToList();
            OnPropertyChanged(nameof(LoadedTransforms));
        }
        public void UndoHistory()
        {
            IFS = tracker.Undo(IFS, LoadedTransforms);
        }

        public void RedoHistory()
        {
            IFS = tracker.Redo(IFS, LoadedTransforms);
        }

        public void ClearHistory()
        {
            tracker.Clear();
        }

        public void TakeSnapshot()
        {
            System.Diagnostics.Debug.WriteLine(IFS.Iterators.Count);
            tracker.TakeSnapshot(IFS);
        }

    }
}
