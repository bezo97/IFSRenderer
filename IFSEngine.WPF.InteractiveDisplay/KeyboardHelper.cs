using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace IFSEngine.WPF.InteractiveDisplay
{
    //KeyboardDelay workaround
    //https://codereview.stackexchange.com/questions/44404/preventing-keydown-delay
    public class KeyboardHelper
    {        
        private HashSet<Key> pressedKeys;
        private readonly object pressedKeysLock = new object();

        public KeyboardHelper(UIElement c)
        {
            c.LostFocus += LostFocus;
            c.KeyDown += WinKeyDown;
            c.KeyUp += WinKeyUp;
            pressedKeys = new HashSet<Key>();
        }

        private void LostFocus(object sender, RoutedEventArgs e)
        {
            lock (pressedKeysLock)
            {
                pressedKeys.Clear();
            }
        }

        public bool IsKeyDown(Key key)
        {
            lock (pressedKeysLock)
            {
                return pressedKeys.Contains(key);
            }
        }

        private void WinKeyDown(object sender, KeyEventArgs e)
        {
            lock (pressedKeysLock)
            {
                pressedKeys.Add(e.Key);
            }
        }

        private void WinKeyUp(object sender, KeyEventArgs e)
        {
            lock (pressedKeysLock)
            {
                pressedKeys.Remove(e.Key);
            }
        }

    }
}
