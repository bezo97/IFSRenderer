using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfDisplay.Helper;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for Node.xaml
/// </summary>
public partial class Node : UserControl
{
    private IteratorViewModel vm => (IteratorViewModel)DataContext;
    private ContentPresenter _parentContainer;
    private Vector? _t;

    public static readonly DependencyProperty IsMouseDownProperty =
        DependencyProperty.Register("IsMouseDown", typeof(bool), typeof(Node), new PropertyMetadata(false));

    public RelayCommand<IteratorViewModel> SelectCommand
    {
        get { return (RelayCommand<IteratorViewModel>)GetValue(SelectCommandProperty); }
        set { SetValue(SelectCommandProperty, value); }
    }
    public static readonly DependencyProperty SelectCommandProperty =
        DependencyProperty.Register("SelectCommand", typeof(RelayCommand<IteratorViewModel>), typeof(Node), new PropertyMetadata(null));

    public BindablePoint Position
    {
        get { return (BindablePoint)GetValue(PositionProperty); }
        set { SetValue(PositionProperty, value); }
    }
    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.Register("Position", typeof(BindablePoint), typeof(Node), new PropertyMetadata(new BindablePoint(0, 0), (s,e) => {
            var container = ((Node)s)._parentContainer;
            if (container is not null)
            {
                Canvas.SetLeft(container, ((BindablePoint)e.NewValue).X);
                Canvas.SetTop(container, ((BindablePoint)e.NewValue).Y);
            }
        }));

    //Positioning is relative to this element
    public FrameworkElement ParentCanvas
    {
        get { return (FrameworkElement)GetValue(ParentCanvasProperty); }
        set { SetValue(ParentCanvasProperty, value); }
    }
    public static readonly DependencyProperty ParentCanvasProperty =
        DependencyProperty.Register("ParentCanvas", typeof(FrameworkElement), typeof(Node), new PropertyMetadata(null));

    public Node()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            _parentContainer = (ContentPresenter)System.Windows.Media.VisualTreeHelper.GetParent(this);

            Position = vm.Position;
            Canvas.SetLeft(_parentContainer, vm.Position.X);
            Canvas.SetTop(_parentContainer, vm.Position.Y);
            vm.PropertyChanged += (s, e) => { 
                if(e.PropertyName is nameof(vm.Position))
                {
                    Canvas.SetLeft(_parentContainer, vm.Position.X);
                    Canvas.SetTop(_parentContainer, vm.Position.Y);
                }
            };

        };
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        e.Handled = true; 
        SetValue(IsMouseDownProperty, true);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        e.Handled = true;
        SetValue(IsMouseDownProperty, false);
        _t = null;
        Mouse.Capture(null);
        if (e.ChangedButton == MouseButton.Left)
        {
            var vm = (IteratorViewModel)DataContext;
            vm.FinishConnecting();
            SelectCommand.Execute(vm);
        }
    }

    private Point GetCanvasPosition()
    {
        return new Point(Canvas.GetLeft(_parentContainer), Canvas.GetTop(_parentContainer));
    }

    private void dragHandle_MouseMove(object sender, MouseEventArgs e)
    {
        if (_t is not null)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Mouse.Capture((IInputElement)sender);
                e.Handled = true;
                Position = BindablePoint.FromPoint(e.GetPosition(ParentCanvas) + _t.Value);
            }
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            e.Handled = true;
            vm.StartConnecting();
        }
    }

    private void UserControl_GotFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        var vm = (IteratorViewModel)DataContext;
        SelectCommand.Execute(vm);
    }

    private void Label_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
        {
            _t = GetCanvasPosition() - e.GetPosition(ParentCanvas);
        }
    }

    private void ellipseBody_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.Focus();
    }
}
