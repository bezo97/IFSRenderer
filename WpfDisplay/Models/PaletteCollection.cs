using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IFSEngine.Model;

namespace WpfDisplay.Models
{
    public class PaletteCollection
    {
        public string Name { get; set; }
        public Author Author { get; set; }
        public List<ColorPalette> Palettes { get; set; }
    }
}
