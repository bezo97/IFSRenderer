using Microsoft.Win32;
using System;
using System.IO;

namespace WpfDisplay.Helper;

public static class DialogHelper
{
    private static readonly string ParamsFilter = "IFSRenderer params|*.ifsjson;*.json";
    private static readonly string ImageFilter = "PNG Images|*.png";
    private static readonly string ExrFilter = "EXR Images|*.exr";
    private static readonly string PaletteFilter = "Flame Palettes|*.gradient";
    private static readonly Guid OpenParamsGuid = Guid.Parse("71fbe830-5632-4672-ac43-31173efa82a2");
    private static readonly Guid SaveParamsGuid = Guid.Parse("b009dd42-ed44-421b-a49c-1ece1c888cc0");
    private static readonly Guid ExportImageGuid = Guid.Parse("c66d2b65-b5fe-427a-9d4b-940776fc9e8d");
    private static readonly Guid ExportExrGuid = Guid.Parse("4A3B3E3A-B2C9-465B-B95D-B49D7DEB1A0A");
    private static readonly Guid OpenPaletteGuid = Guid.Parse("56bac078-5845-492b-a4b9-92ab66bb108c");
    private static readonly OpenFileDialog OpenParamsDialog = new()
    {
        CheckFileExists = true,
        Filter = ParamsFilter,
        Tag = OpenParamsGuid,
        Title = "Open params"
    };
    private static readonly SaveFileDialog SaveParamsDialog = new()
    {
        DefaultExt = ".ifsjson",
        Filter = ParamsFilter,
        Tag = SaveParamsGuid,
        Title = "Save params"
    };
    private static readonly SaveFileDialog ExportImageDialog = new()
    {
        DefaultExt = ".png",
        Filter = ImageFilter,
        Tag = ExportImageGuid,
        Title = "Export image"
    };
    private static readonly SaveFileDialog ExportExrDialog = new()
    {
        DefaultExt = ".exr",
        Filter = ExrFilter,
        Tag = ExportExrGuid,
        Title = "Export hdr image"
    };
    private static readonly OpenFileDialog OpenPaletteDialog = new()
    {
        CheckFileExists = true,
        Filter = PaletteFilter,
        Tag = OpenPaletteGuid,
        Title = "Open palette"
    };

    public static bool ShowOpenParamsDialog(out string FilePath)
    {
        bool selected = OpenParamsDialog.ShowDialog() ?? false;
        FilePath = OpenParamsDialog.FileName;
        return selected;
    }
    public static bool ShowSaveParamsDialog(string filenameHint, out string FilePath)
    {
        SaveParamsDialog.FileName = Path.Combine(SaveParamsDialog.InitialDirectory, filenameHint + ".ifsjson");
        bool selected = SaveParamsDialog.ShowDialog() ?? false;
        FilePath = SaveParamsDialog.FileName;
        return selected;
    }
    public static bool ShowExportImageDialog(string filenameHint, out string FilePath)
    {
        ExportImageDialog.FileName = Path.Combine(ExportImageDialog.InitialDirectory, filenameHint + ".png");
        bool selected = ExportImageDialog.ShowDialog() ?? false;
        FilePath = ExportImageDialog.FileName;
        return selected;
    }
    public static bool ShowExportExrDialog(string filenameHint, out string FilePath)
    {
        ExportExrDialog.FileName = Path.Combine(ExportExrDialog.InitialDirectory, filenameHint + ".exr");
        bool selected = ExportExrDialog.ShowDialog() ?? false;
        FilePath = ExportExrDialog.FileName;
        return selected;
    }
    public static bool ShowOpenPaletteDialog(out string FilePath)
    {
        bool selected = OpenPaletteDialog.ShowDialog() ?? false;
        FilePath = OpenPaletteDialog.FileName;
        return selected;
    }

}
