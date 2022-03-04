using Microsoft.Win32;
using System;
using System.IO;

namespace WpfDisplay.Helper;

public static class DialogHelper
{
    private static readonly string _paramsFilter = "IFSRenderer params|*.ifsjson;*.json";
    private static readonly string _imageFilter = "PNG Images|*.png";
    private static readonly string _exrFilter = "EXR Images|*.exr";
    private static readonly string _paletteFilter = "Flame Palettes|*.gradient;*.ugr";
    private static readonly Guid _openParamsGuid = Guid.Parse("71fbe830-5632-4672-ac43-31173efa82a2");
    private static readonly Guid _saveParamsGuid = Guid.Parse("b009dd42-ed44-421b-a49c-1ece1c888cc0");
    private static readonly Guid _exportImageGuid = Guid.Parse("c66d2b65-b5fe-427a-9d4b-940776fc9e8d");
    private static readonly Guid _exportExrGuid = Guid.Parse("4A3B3E3A-B2C9-465B-B95D-B49D7DEB1A0A");
    private static readonly Guid _openPaletteGuid = Guid.Parse("56bac078-5845-492b-a4b9-92ab66bb108c");
    private static readonly OpenFileDialog _openParamsDialog = new()
    {
        CheckFileExists = true,
        Filter = _paramsFilter,
        Tag = _openParamsGuid,
        Title = "Open params"
    };
    private static readonly SaveFileDialog _saveParamsDialog = new()
    {
        DefaultExt = ".ifsjson",
        Filter = _paramsFilter,
        Tag = _saveParamsGuid,
        Title = "Save params"
    };
    private static readonly SaveFileDialog _exportImageDialog = new()
    {
        DefaultExt = ".png",
        Filter = _imageFilter,
        Tag = _exportImageGuid,
        Title = "Export image"
    };
    private static readonly SaveFileDialog _exportExrDialog = new()
    {
        DefaultExt = ".exr",
        Filter = _exrFilter,
        Tag = _exportExrGuid,
        Title = "Export hdr image"
    };
    private static readonly OpenFileDialog _openPaletteDialog = new()
    {
        CheckFileExists = true,
        Filter = _paletteFilter,
        Tag = _openPaletteGuid,
        Title = "Open palette"
    };

    public static bool ShowOpenParamsDialog(out string FilePath)
    {
        bool selected = _openParamsDialog.ShowDialog() ?? false;
        FilePath = _openParamsDialog.FileName;
        return selected;
    }
    public static bool ShowSaveParamsDialog(string filenameHint, out string FilePath)
    {
        _saveParamsDialog.FileName = Path.Combine(_saveParamsDialog.InitialDirectory, filenameHint + ".ifsjson");
        bool selected = _saveParamsDialog.ShowDialog() ?? false;
        FilePath = _saveParamsDialog.FileName;
        return selected;
    }
    public static bool ShowExportImageDialog(string filenameHint, out string FilePath)
    {
        _exportImageDialog.FileName = Path.Combine(_exportImageDialog.InitialDirectory, filenameHint + ".png");
        bool selected = _exportImageDialog.ShowDialog() ?? false;
        FilePath = _exportImageDialog.FileName;
        return selected;
    }
    public static bool ShowExportExrDialog(string filenameHint, out string FilePath)
    {
        _exportExrDialog.FileName = Path.Combine(_exportExrDialog.InitialDirectory, filenameHint + ".exr");
        bool selected = _exportExrDialog.ShowDialog() ?? false;
        FilePath = _exportExrDialog.FileName;
        return selected;
    }
    public static bool ShowOpenPaletteDialog(out string FilePath)
    {
        bool selected = _openPaletteDialog.ShowDialog() ?? false;
        FilePath = _openPaletteDialog.FileName;
        return selected;
    }

}
