using IFSEngine.Utility;
using Microsoft.Toolkit.Mvvm.Input;
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
    private Vector _t;

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

        var vm = (IteratorViewModel)DataContext;
        if (e.ChangedButton == MouseButton.Left)
        {
            _t = GetCanvasPosition() - e.GetPosition(ParentCanvas);
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
            Position = BindablePoint.FromPoint(e.GetPosition(ParentCanvas) + _t);
        }
    }

    private Point GetCanvasPosition()
    {
        return new Point(Canvas.GetLeft(_parentContainer), Canvas.GetTop(_parentContainer));
    }

}
