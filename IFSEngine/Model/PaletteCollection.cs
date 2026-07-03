using System;
using System.Collections.Generic;

namespace IFSEngine.Model;

public class PaletteCollection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Unnamed collection";
    public string Description { get; set; } = "";
    public Author Author { get; set; } = Author.Unknown;
    public List<ColorPalette> Palettes { get; set; } = [];
}
