using IFSEngine.Model;
using System.Collections.Generic;
using System.Linq;
using WpfDisplay.Helper;

namespace WpfDisplay.Serialization;

/// <summary>
/// Helper class for serializing <see cref="IFSEngine.Model.IFS"/> with node positions.
/// </summary>
public class IfsNodes
{
    public IFS IFS { get; set; }

    public List<BindablePoint> Positions
    {
        get
        {
            return IFS.Iterators.Select(i => i.GetPosition()).ToList();
        }
        set
        {
            var iteratorList = IFS.Iterators.ToList();
            for (int i = 0; i < value.Count; i++)
                iteratorList[i].SetPosition(value[i]);
        }
    }

    public IfsNodes(IFS ifs)
    {
        this.IFS = ifs;
    }
}
