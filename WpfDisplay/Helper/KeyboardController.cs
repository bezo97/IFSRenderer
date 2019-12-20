using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace WpfDisplay.Helper
{
    //KeyboardDelay workaround
    //https://codereview.stackexchange.com/questions/44404/preventing-keydown-delay
    public class KeyboardController
    {
        public event EventHandler KeyboardTick;
        private Timer timer;
        private HashSet<Key> pressedKeys;
        private readonly object pressedKeysLock = new object();

        public KeyboardController(UIElement c)
        {
            c.KeyDown += WinKeyDown;
            c.KeyUp += WinKeyUp;
            pressedKeys = new HashSet<Key>();

            timer = new Timer();
            timer.Elapsed += kbTimer_Tick;
            timer.Interval = 10;//ms
            timer.Start();
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

        private void kbTimer_Tick(object sender, EventArgs e)
        {   
            KeyboardTick?.Invoke(this, EventArgs.Empty);
        }
    }
}
