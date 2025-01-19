using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace IFSEngine.WPF.InteractiveDisplay;

//KeyboardDelay workaround
//https://codereview.stackexchange.com/questions/44404/preventing-keydown-delay
public class KeyboardHelper
{
    private readonly HashSet<Key> _pressedKeys;
    private readonly Lock _pressedKeysLock = new();
    
    public KeyboardHelper(UIElement c)
    {
        c.LostFocus += LostFocus;
        c.KeyDown += WinKeyDown;
        c.KeyUp += WinKeyUp;
        _pressedKeys = [];
    }

    private void LostFocus(object sender, RoutedEventArgs e)
    {
        lock (_pressedKeysLock)
        {
            _pressedKeys.Clear();
        }
    }

    public bool IsKeyDown(Key key)
    {
        lock (_pressedKeysLock)
        {
            return _pressedKeys.Contains(key);
        }
    }

    private void WinKeyDown(object sender, KeyEventArgs e)
    {
        lock (_pressedKeysLock)
        {
            _pressedKeys.Add(e.Key);
        }
    }

    private void WinKeyUp(object sender, KeyEventArgs e)
    {
        lock (_pressedKeysLock)
        {
            _pressedKeys.Remove(e.Key);
        }
    }

}
