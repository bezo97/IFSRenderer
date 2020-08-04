using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using WpfDisplay.Helper;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for RenderDisplay.xaml
    /// </summary>
    public partial class RenderDisplay : WindowsFormsHost
    {
        private IFSViewModel IFSViewModel { get; set; }
        private KeyboardController keyboard;
        //last mouse position
        private float lastX;
        private float lastY;

        private readonly Key[] translateKeys = 
        {
            Key.W, Key.S, Key.D, Key.A, Key.Q, Key.E, Key.C
        };

        private readonly Key[] rotateKeys = 
        {
            Key.I, Key.K, Key.J, Key.L, Key.U, Key.O
        };

        public RenderDisplay()
        {
            InitializeComponent();

            //Avoid threading problems
            DataContextChanged += (s, e) => IFSViewModel = (IFSViewModel)DataContext;

            keyboard = new KeyboardController(this);
            keyboard.KeyboardTick += KeydownHandler;
        }

        private void Display1_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            IFSViewModel.ifs.ViewSettings.FocusDistance += e.Delta * IFSViewModel.ifs.ViewSettings.FocusDistance * 0.001;
        }

        private void Display1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.None;
                float yawDelta = lastX - e.X;
                float pitchDelta = lastY - e.Y;
                IFSViewModel.ifs.ViewSettings.Camera.RotateWithSensitivity(new Vector3(yawDelta, pitchDelta, 0.0f));
                IFSViewModel.RaisePropertyChanged("InvalidateAccumulation");//
            }
            else
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            lastX = e.X;
            lastY = e.Y;
        }

        private void KeydownHandler(object sender, EventArgs e)
        {
            if (translateKeys.Any(k => keyboard.IsKeyDown(k)))
            {
                //Experiment: camera speed relates to focus distance
                float magnitude = (float)IFSViewModel.ifs.ViewSettings.FocusDistance;
                var direction = new Vector3(
                    ((keyboard.IsKeyDown(Key.D) ? 1 : 0) - (keyboard.IsKeyDown(Key.A) ? 1 : 0)),
                    ((keyboard.IsKeyDown(Key.E) ? 1 : 0) - ((keyboard.IsKeyDown(Key.C) || keyboard.IsKeyDown(Key.Q)) ? 1 : 0)),
                    ((keyboard.IsKeyDown(Key.W) ? 1 : 0) - (keyboard.IsKeyDown(Key.S) ? 1 : 0))
                );
                IFSViewModel.ifs.ViewSettings.Camera.TranslateWithSensitivity(magnitude * direction);
                IFSViewModel.RaisePropertyChanged("InvalidateAccumulation");//
            }

            if (rotateKeys.Any(k => keyboard.IsKeyDown(k)))
            {
                float magnitude = 2.0f;
                var direction = new Vector3(
                    ((keyboard.IsKeyDown(Key.J) ? 1 : 0) - (keyboard.IsKeyDown(Key.L) ? 1 : 0)),
                    ((keyboard.IsKeyDown(Key.I) ? 1 : 0) - (keyboard.IsKeyDown(Key.K) ? 1 : 0)),
                    ((keyboard.IsKeyDown(Key.O) ? 1 : 0) - (keyboard.IsKeyDown(Key.U) ? 1 : 0))
                );
                IFSViewModel.ifs.ViewSettings.Camera.RotateWithSensitivity(magnitude * direction);
                IFSViewModel.RaisePropertyChanged("InvalidateAccumulation");//
            }

        }

    }
}
