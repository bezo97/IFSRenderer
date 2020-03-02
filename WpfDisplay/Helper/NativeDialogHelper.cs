using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfDisplay.Helper
{
    public static class NativeDialogHelper
    {
        private static Guid OpenParamsGuid = Guid.Parse("71fbe830-5632-4672-ac43-31173efa82a2");
        private static Guid SaveParamsGuid = Guid.Parse("b009dd42-ed44-421b-a49c-1ece1c888cc0");
        private static Guid SaveImageGuid = Guid.Parse("c66d2b65-b5fe-427a-9d4b-940776fc9e8d");
        private static Guid OpenPaletteGuid = Guid.Parse("56bac078-5845-492b-a4b9-92ab66bb108c");

        public static bool ShowFileSelectorDialog(DialogSetting ds, out string DialogResult)
        {
            DialogResult = System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var dlg = new CommonOpenFileDialog //Save??
                {
                    Title = ds.ToString(),
                    AddToMostRecentlyUsedList = true,
                    EnsurePathExists = true,
                    Multiselect = false,
                    ShowPlacesList = true,
                    CookieIdentifier =
                        ds == DialogSetting.OpenParams ? OpenParamsGuid :
                        ds == DialogSetting.SaveParams ? SaveParamsGuid :
                        ds == DialogSetting.SaveImage ? SaveImageGuid :
                        ds == DialogSetting.OpenPalette ? OpenPaletteGuid :
                        Guid.Empty,
                    DefaultExtension =
                        ds == DialogSetting.OpenParams ? "json" :
                        ds == DialogSetting.SaveParams ? "json" :
                        ds == DialogSetting.SaveImage ? "png" :
                        ds == DialogSetting.OpenPalette ? "gradient" :
                        ""
                };

                switch (ds)
                {
                    case DialogSetting.OpenParams:
                        dlg.Filters.Add(new CommonFileDialogFilter("JSON params", "*.json"));
                        break;
                    case DialogSetting.SaveParams:
                        dlg.Filters.Add(new CommonFileDialogFilter("JSON params", "*.json"));
                        break;
                    case DialogSetting.SaveImage:
                        dlg.Filters.Add(new CommonFileDialogFilter("PNG Image", "*.png"));
                        break;
                    case DialogSetting.OpenPalette:
                        dlg.Filters.Add(new CommonFileDialogFilter("Palette", "*.gradient;*.ugr"));
                        break;
                    default:
                        break;
                }

                if (dlg.ShowDialog(Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive)) == CommonFileDialogResult.Ok)
                    return dlg.FileName;
                else
                    return null;

            });

            if (DialogResult is null)
                return false;
            else
                return true;
        }
    }

    //refactor..
    public enum DialogSetting
    {
        OpenParams,
        SaveParams,
        SaveImage,
        OpenPalette
    }

}
