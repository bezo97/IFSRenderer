using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public bool Equals(Author other)
    {
        return Name == other?.Name;
    }
    public override bool Equals(object obj)
    {
        return Equals(obj as Author);
    }
    public static bool operator ==(Author lhs, Author rhs)
    {
        return lhs is null ? rhs is null : lhs.Equals(rhs);
    }
    public static bool operator !=(Author lhs, Author rhs)
    {
        return !(lhs == rhs);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
