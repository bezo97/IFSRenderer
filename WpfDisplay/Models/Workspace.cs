using IFSEngine;
using IFSEngine.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace WpfDisplay.Models
{
    /// <summary>
    /// The main workspace model that contains a <see cref="RendererGL"/> 
    /// and an <see cref="IFSEngine.Model.IFS"/> that it is rendering.
    /// </summary>
    public class Workspace : ObservableObject
    {
        private RendererGL renderer;
        private IFS ifs;

        public RendererGL Renderer
        {
            get { return renderer; }
            set {
                SetProperty(ref renderer, value);
                if(ifs != null)
                    renderer.LoadParams(ifs);
            }
        }


        public IFS IFS
        {
            get { return ifs; }
            set {
                SetProperty(ref ifs, value);
                renderer?.LoadParams(ifs);
            }
        }


    }
}
