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
    private ContentPresenter ParentContainer;
    private float tx, ty;

    public RelayCommand<IteratorViewModel> SelectCommand
    {
        get { return (RelayCommand<IteratorViewModel>)GetValue(SelectCommandProperty); }
        set { SetValue(SelectCommandProperty, value); }
    }
    public static readonly DependencyProperty SelectCommandProperty =
        DependencyProperty.Register("SelectCommand", typeof(RelayCommand<IteratorViewModel>), typeof(Node), new PropertyMetadata(null));

    public float NodePositionX
    {
        get { return (float)Canvas.GetLeft(ParentContainer); }
        set { Canvas.SetLeft(ParentContainer, value); }
    }

    public float NodePositionY
    {
        get { return (float)Canvas.GetTop(ParentContainer); }
        set { Canvas.SetTop(ParentContainer, value); }
    }

    public Node()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            ParentContainer = (ContentPresenter)System.Windows.Media.VisualTreeHelper.GetParent(this);
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
            tx = vm.XCoord - (float)e.GetPosition(null).X;//vm.XCoord - e.GetPosition(Map).X;
            ty = vm.YCoord - (float)e.GetPosition(null).Y;//vm.YCoord - e.GetPosition(Map).Y;
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
            float XCoord = tx + (float)e.GetPosition(null).X;
            float YCoord = ty + (float)e.GetPosition(null).Y;
            vm.UpdatePosition(XCoord, YCoord);
            NodePositionX = vm.XCoord;
            NodePositionY = vm.YCoord;
        }
    }

}
