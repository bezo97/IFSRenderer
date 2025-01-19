using System.Collections.Generic;

using IFSEngine.Model;

namespace WpfDisplay.Models;

public class PaletteCollectionExt
{
    public string Name { get; set; }
    public Author Author { get; set; }
    public List<ColorPaletteExt> Palettes { get; set; }
}
