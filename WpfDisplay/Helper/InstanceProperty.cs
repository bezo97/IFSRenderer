using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WpfDisplay.Helper
{
    public class InstanceProperty// : INotifyPropertyChanged
    {

        public event /*PropertyChanged*/EventHandler PropertyChanged;

        private readonly object instance;
        private readonly PropertyInfo pi;

        public InstanceProperty(object instance, PropertyInfo pi)
        {
            this.instance = instance;
            this.pi = pi;
        }

        public string PropertyName => pi.Name;

        public double PropertyValue
        {
            get { return (double)pi.GetValue(instance); }
            set
            {
                pi.SetValue(instance, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
            }
        }

    }
}
