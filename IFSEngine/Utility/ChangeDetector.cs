using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFSEngine.Utility;

public class ChangeDetector<T>
{
    private T _value;
    public event EventHandler<T> ValueChanged;
    public static implicit operator T(ChangeDetector<T> prop)
    {
        return prop._value;
    }

    /// <returns>value changed</returns>
    public bool Update(T value)
    {
        if (this._value == null || !this._value.Equals(value))
        {
            this._value = value;
            ValueChanged?.Invoke(this, value);
            return true;
        }
        return false;
    }

    public ChangeDetector() { }

    public ChangeDetector(T defaulValue)
    {
        _value = defaulValue;
    }

    public override string ToString()
    {
        return _value.ToString();
    }
}
