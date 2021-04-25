using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFSEngine.Utility
{
    public class ChangeDetector<T>
    {
        private T value;
        public event EventHandler<T> ValueChanged;
        public static implicit operator T(ChangeDetector<T> prop)
        {
            return prop.value;
        }

        /// <returns>value changed</returns>
        public bool Update(T value)
        {
            if (this.value == null || !this.value.Equals(value))
            {
                this.value = value;
                ValueChanged?.Invoke(this, value);
                return true;
            }
            return false;
        }

        public ChangeDetector() { }

        public ChangeDetector(T defaulValue)
        {
            value = defaulValue;
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
