using Microsoft.Toolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for Node.xaml
/// </summary>
public partial class Node : UserControl
{
    private IteratorViewModel vm => (IteratorViewModel)DataContext;
    private ContentPresenter _parentContainer;
    private float _tx, _ty;

    public RelayCommand<IteratorViewModel> SelectCommand
    {
        get { return (RelayCommand<IteratorViewModel>)GetValue(SelectCommandProperty); }
        set { SetValue(SelectCommandProperty, value); }
    }
    public static readonly DependencyProperty SelectCommandProperty =
        DependencyProperty.Register("SelectCommand", typeof(RelayCommand<IteratorViewModel>), typeof(Node), new PropertyMetadata(null));

    public float NodePositionX
    {
        get { return (float)Canvas.GetLeft(_parentContainer); }
        set { Canvas.SetLeft(_parentContainer, value); }
    }

    public float NodePositionY
    {
        get { return (float)Canvas.GetTop(_parentContainer); }
        set { Canvas.SetTop(_parentContainer, value); }
    }

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
            NodePositionX = vm.XCoord;
            NodePositionY = vm.YCoord;
        };
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        e.Handled = true;

        var vm = (IteratorViewModel)DataContext;
        if (e.ChangedButton == MouseButton.Left)
        {
            _tx = vm.XCoord - (float)e.GetPosition(ParentCanvas).X;
            _ty = vm.YCoord - (float)e.GetPosition(ParentCanvas).Y;
            SelectCommand.Execute(vm);
        }
        else if (e.ChangedButton == MouseButton.Right)
        {
            vm.StartConnecting();
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        e.Handled = true;
        if (e.ChangedButton == MouseButton.Right)
        {
            var vm = (IteratorViewModel)DataContext;
            vm.FinishConnecting();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        e.Handled = true;

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            e.Handled = true;
            float XCoord = _tx + (float)e.GetPosition(ParentCanvas).X;
            float YCoord = _ty + (float)e.GetPosition(ParentCanvas).Y;
            vm.UpdatePosition(XCoord, YCoord);
            NodePositionX = vm.XCoord;
            NodePositionY = vm.YCoord;
        }
    }

}
