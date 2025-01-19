using System.Collections.Generic;

namespace IFSEngine.Model;

public class PaletteCollection
{
    public string Name { get; set; }
    public Author Author { get; set; }
    public List<ColorPalette> Palettes { get; set; }
}
