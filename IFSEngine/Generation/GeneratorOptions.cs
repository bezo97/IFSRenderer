﻿using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFSEngine.Generation;

public class GeneratorOptions
{
    public double MutationChance = 0.5;
    public double MutationStrength = 1.0;
    public bool MutateIterators = true;
    public bool MutateConnections = true;
    public bool MutateConnectionWeights = true;
    public bool MutateParameters = true;
    public bool MutatePalette = true;
    public bool MutateColoring = true;
    public IFS baseParams = new IFS
    {
        Brightness = 1,
        Gamma = 4,
        ImageResolution = new Size(1080, 1080)
    };
    //TODO: select transforms
}
