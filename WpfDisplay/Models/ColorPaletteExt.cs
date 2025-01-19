using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDisplay.Models;

public class ColorPaletteExt : IFSEngine.Model.ColorPalette
{
    public bool IsFavorite { get; set; }
}
