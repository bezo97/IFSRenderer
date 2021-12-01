using System;
using System.ComponentModel;
using System.Reflection;

namespace WpfDisplay.Helper;

public class InstanceProperty// : INotifyPropertyChanged
{

    public event /*PropertyChanged*/EventHandler PropertyChanged;

    private readonly object _instance;
    private readonly PropertyInfo _pi;

    public InstanceProperty(object instance, PropertyInfo pi)
    {
        _instance = instance;
        _pi = pi;
    }

    public string PropertyName => _pi.Name;

    public double PropertyValue
    {
        get { return (double)_pi.GetValue(_instance); }
        set
        {
            _pi.SetValue(_instance, value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
    }

}
