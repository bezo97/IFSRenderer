using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GLDisplayWpf
{
    /// <summary>
    /// Interaction logic for IteratorView.xaml
    /// </summary>
    public partial class IteratorView : UserControl
    {
        private List<ValueControl> affineTrans;

        public IteratorView()
        {
            InitializeComponent();

            affineTrans = new List<ValueControl> {
                affineOX, affineOY, affineOZ,
                affineXX, affineXY, affineXZ,
                affineYX, affineYY, affineYZ,
                affineZX, affineZY, affineZZ
            };

            foreach (var trans in affineTrans)
            {
                trans.RangeToValue = f => (f * 2.0f - 1.0f) * 1.5f;
                trans.ValueToRange = f => (f / 1.5f + 1.0f) / 2.0f;
            }

            iteratorCS.RangeToValue = f => (f * 2.0f - 1.0f) * 0.1f;
            iteratorCS.ValueToRange = f => (f / 0.1f + 1.0f) / 2.0f;

            BindModify();

            IteratorManipulator.EditStateChanged += EditStateChanged_Handler;
            IteratorManipulator.IteratorChanged += IteratorChanged_Handler;

            EditStateChanged_Handler(null, null);
        }



        private Iterator Iterator {
            get => iter;

            set {
                iter = value;

                affineOX.Value = iter.aff.ox;
                affineOY.Value = iter.aff.oy;
                affineOZ.Value = iter.aff.oz;

                affineXX.Value = iter.aff.xx;
                affineXY.Value = iter.aff.xy;
                affineXZ.Value = iter.aff.xz;

                affineYX.Value = iter.aff.yx;
                affineYY.Value = iter.aff.yy;
                affineYZ.Value = iter.aff.yz;

                affineZX.Value = iter.aff.zx;
                affineZY.Value = iter.aff.zy;
                affineZZ.Value = iter.aff.zz;

                iteratorW.Value = iter.w;
                iteratorCS.Value = iter.cs;
                iteratorCI.Value = iter.ci;
                iteratorOp.Value = iter.op;

                iteratorSph.IsChecked = iter.tfID == 1;
            }
        }

        private Iterator iter;

        private void BindModify()
        {
            affineOX.ValueChanged += (s, e) => iter.aff.ox = e.Value;
            affineOY.ValueChanged += (s, e) => iter.aff.oy = e.Value;
            affineOZ.ValueChanged += (s, e) => iter.aff.oz = e.Value;

            affineXX.ValueChanged += (s, e) => iter.aff.xx = e.Value;
            affineXY.ValueChanged += (s, e) => iter.aff.xy = e.Value;
            affineXZ.ValueChanged += (s, e) => iter.aff.xz = e.Value;

            affineYX.ValueChanged += (s, e) => iter.aff.yx = e.Value;
            affineYY.ValueChanged += (s, e) => iter.aff.yy = e.Value;
            affineYZ.ValueChanged += (s, e) => iter.aff.yz = e.Value;

            affineZX.ValueChanged += (s, e) => iter.aff.zx = e.Value;
            affineZY.ValueChanged += (s, e) => iter.aff.zy = e.Value;
            affineZZ.ValueChanged += (s, e) => iter.aff.zz = e.Value;

            iteratorW.ValueChanged += (s, e) => iter.w = e.Value;
            iteratorCS.ValueChanged += (s, e) => iter.cs = e.Value;
            iteratorCI.ValueChanged += (s, e) => iter.ci = e.Value;
            iteratorOp.ValueChanged += (s, e) => iter.op = e.Value;

            iteratorSph.Checked += (s, e) => iter.tfID = ((s as CheckBox).IsChecked ?? false) ? 0 : 1;
        }


        private void ButtonPrevious_Click(object sender, RoutedEventArgs e)
        {
            IteratorManipulator.EditState--;
        }

        private void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            IteratorManipulator.EditState++;
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonAddNew_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditStateChanged_Handler(object sender, EventArgs e)
        {
            labelIterator.Content = "Iterator <" + IteratorManipulator.IteratorCount + "/" + (IteratorManipulator.EditState + 1) + ">";
            Iterator = IteratorManipulator.Iterator;
        }

        private void IteratorChanged_Handler(object sender, EventArgs e)
        {
            if (sender != this)
            {
                Iterator = IteratorManipulator.Iterator;
            }
        }

        private void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            IteratorManipulator.ModifyIterator(i => iter, this, EventArgs.Empty);
            Refresh_CallBack?.Invoke(this, EventArgs.Empty);
        }

        public EventHandler Refresh_CallBack;
    }
}
