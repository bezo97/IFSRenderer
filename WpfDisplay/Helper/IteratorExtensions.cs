using IFSEngine.Model;
using System.Runtime.CompilerServices;

namespace WpfDisplay.Helper;

public static class IteratorExtensions
{
    private static ConditionalWeakTable<Iterator, BindablePoint> Position { get; } = new();

    public static BindablePoint GetPosition(this Iterator iterator)
    {
        if (Position.TryGetValue(iterator, out var position))
            return position;
        return null;
    }

    public static void SetPosition(this Iterator iterator, BindablePoint position)
    {
        Position.AddOrUpdate(iterator, position);
    }

}
