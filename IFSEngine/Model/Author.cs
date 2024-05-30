using System;

namespace IFSEngine.Model;

public class Author : IEquatable<Author>
{
    public string Name { get; set; }
    public string Link { get; set; }

    public static Author Unknown { get; } = new Author
    {
        Name = "Unknown Artist",
        Link = "-"
    };

    public bool Equals(Author other) => Name == other?.Name;
    public override bool Equals(object obj) => Equals(obj as Author);
    public static bool operator ==(Author lhs, Author rhs)
    {
        return lhs is null ? rhs is null : lhs.Equals(rhs);
    }
    public static bool operator !=(Author lhs, Author rhs)
    {
        return !(lhs == rhs);
    }

    public override int GetHashCode() => Name.GetHashCode();
}
