using IFSEngine.Animation;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfDisplay.Views.Animation;

/// <summary>
/// Interaction logic for DopeButton.xaml
/// </summary>
public partial class DopeButton : UserControl
{
    public Keyframe ControlPoint { get; private set; }
    public EventHandler<MouseEventArgs> OnDrag;
    private bool _isDragging = false;
    public DopeButton()
    {
        InitializeComponent();
    }

    public void SetControlPoint(Keyframe controlPoint) => this.ControlPoint = controlPoint;
    public void SetTime(double t) => ControlPoint.t = t;

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _isDragging)
        {
            OnDrag?.Invoke(this, e);
        }

    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
    }
}
