using GalaSoft.MvvmLight;
using IFSEngine;
using IFSEngine.Model;

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
                Set(ref renderer, value);
                if(ifs != null)
                    renderer.LoadParams(ifs);
            }
        }


        public IFS IFS
        {
            get { return ifs; }
            set {
                Set(ref ifs, value);
                renderer?.LoadParams(ifs);
            }
        }


    }
}
