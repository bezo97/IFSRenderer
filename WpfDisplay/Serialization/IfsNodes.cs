using System.Collections.Generic;
using System.Linq;

using IFSEngine.Model;

using WpfDisplay.Helper;

namespace WpfDisplay.Serialization;

/// <summary>
/// Helper class for serializing <see cref="IFSEngine.Model.IFS"/> with node positions.
/// </summary>
public class IfsNodes(IFS ifs)
{
    public IFS IFS { get; set; } = ifs;

    public List<BindablePoint> Positions
    {
        get => IFS.Iterators.Select(i => i.GetPosition()).ToList();
        set
        {
            var iteratorList = IFS.Iterators.ToList();
            for (int i = 0; i < value.Count; i++)
                iteratorList[i].SetPosition(value[i]);
        }
    }
}
