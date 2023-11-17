using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFSEngine.Generation;

public class GeneratorOptions
{
    public double MutationChance { get; set; } = 0.5;
    public double MutationStrength { get; set; } = 1.0;
    public int BatchSize { get; set; } = 30;
    public bool MutateIterators { get; set; } = true;
    public bool MutateConnections { get; set; } = true;
    public bool MutateConnectionWeights { get; set; } = true;
    public bool MutateParameters { get; set; } = true;
    public bool MutatePalette { get; set; } = true;
    public bool MutateColoring { get; set; } = true;
    public IFS BaseParams { get; set; } = DefaultStartingIfs;
    //TODO: select transforms

    public static readonly IFS DefaultStartingIfs = new()
    {
        Brightness = 4,
        Gamma = 4,
        ImageResolution = new Size(1080, 1080),
    };

}
