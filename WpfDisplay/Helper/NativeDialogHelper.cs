using System;
using System.IO;
using System.Windows.Forms;

namespace WpfDisplay.Helper;

public static class DialogHelper
{
    private static readonly string _paramsFilter = "IFSRenderer params|*.ifsjson;*.json";
    private static readonly string _imageFilter = "PNG Images|*.png";
    private static readonly string _exrFilter = "EXR Images|*.exr";
    private static readonly string _paletteFilter = "Flame Palettes|*.gradient;*.ugr";
    private static readonly string _audioFilter = "Audio files|" + Cavern.Format.AudioReader.filter;//allow any file supported by Cavern
    private static readonly string _ffmpegExeFilter = "FFmpeg executable|ffmpeg.exe";
    private static readonly Guid _openParamsGuid = Guid.Parse("71fbe830-5632-4672-ac43-31173efa82a2");
    private static readonly Guid _saveParamsGuid = Guid.Parse("b009dd42-ed44-421b-a49c-1ece1c888cc0");
    private static readonly Guid _exportImageGuid = Guid.Parse("c66d2b65-b5fe-427a-9d4b-940776fc9e8d");
    private static readonly Guid _exportExrGuid = Guid.Parse("4A3B3E3A-B2C9-465B-B95D-B49D7DEB1A0A");
    private static readonly Guid _openPaletteGuid = Guid.Parse("56bac078-5845-492b-a4b9-92ab66bb108c");
    private static readonly Guid _openAudioGuid = Guid.Parse("24E3A7D5-A565-4E98-87C4-06D676A9EBD3");
    private static readonly Guid _animationFolderBrowserGuid = Guid.Parse("8E896C73-B414-447C-909E-7D15B2569357");
    private static readonly Guid _ffmpegSelectorGuid = Guid.Parse("324D0CB0-3B30-4C91-94C3-094BC67C8100");
    private static readonly OpenFileDialog _openParamsDialog = new()
    {
        CheckFileExists = true,
        Filter = _paramsFilter,
        ClientGuid = _openParamsGuid,
        RestoreDirectory = true,
        Title = "Open params"
    };
    private static readonly SaveFileDialog _saveParamsDialog = new()
    {
        DefaultExt = ".ifsjson",
        Filter = _paramsFilter,
        ClientGuid = _saveParamsGuid,
        RestoreDirectory = true,
        Title = "Save params"
    };
    private static readonly SaveFileDialog _exportImageDialog = new()
    {
        DefaultExt = ".png",
        Filter = _imageFilter,
        ClientGuid = _exportImageGuid,
        RestoreDirectory = true,
        Title = "Export image"
    };
    private static readonly SaveFileDialog _exportExrDialog = new()
    {
        DefaultExt = ".exr",
        Filter = _exrFilter,
        ClientGuid = _exportExrGuid,
        RestoreDirectory = true,
        Title = "Export hdr image"
    };
    private static readonly OpenFileDialog _openPaletteDialog = new()
    {
        CheckFileExists = true,
        Filter = _paletteFilter,
        ClientGuid = _openPaletteGuid,
        RestoreDirectory = true,
        Title = "Open palette"
    };
    private static readonly OpenFileDialog _openAudioDialog = new()
    {
        CheckFileExists = true,
        Filter = _audioFilter,
        ClientGuid = _openAudioGuid,
        RestoreDirectory = true,
        Title = "Open audio"
    };
    private static readonly FolderBrowserDialog _animationFolderBrowserDialog = new()
    {
        ClientGuid = _animationFolderBrowserGuid,
        Description = "Select the folder where animation frames will be saved.",
    };
    private static readonly OpenFileDialog _ffmpegSelectorDialog = new()
    {
        CheckFileExists = true,
        Filter = _ffmpegExeFilter,
        ClientGuid = _ffmpegSelectorGuid,
        RestoreDirectory = true,
        Title = "Select FFmpeg executable (ffmpeg.exe)"
    };

    public static bool ShowOpenParamsDialog(out string FilePath)
    {
        bool selected = _openParamsDialog.ShowDialog() is DialogResult.OK;
        FilePath = _openParamsDialog.FileName;
        return selected;
    }
    public static bool ShowSaveParamsDialog(string filenameHint, out string FilePath)
    {
        _saveParamsDialog.FileName = Path.Combine(_saveParamsDialog.InitialDirectory, filenameHint + ".ifsjson");
        bool selected = _saveParamsDialog.ShowDialog() is DialogResult.OK;
        FilePath = _saveParamsDialog.FileName;
        return selected;
    }
    public static bool ShowExportImageDialog(string filenameHint, out string FilePath)
    {
        _exportImageDialog.FileName = Path.Combine(_exportImageDialog.InitialDirectory, filenameHint + ".png");
        bool selected = _exportImageDialog.ShowDialog() is DialogResult.OK;
        FilePath = _exportImageDialog.FileName;
        return selected;
    }
    public static bool ShowExportExrDialog(string filenameHint, out string FilePath)
    {
        _exportExrDialog.FileName = Path.Combine(_exportExrDialog.InitialDirectory, filenameHint + ".exr");
        bool selected = _exportExrDialog.ShowDialog() is DialogResult.OK;
        FilePath = _exportExrDialog.FileName;
        return selected;
    }
    public static bool ShowOpenPaletteDialog(out string FilePath)
    {
        bool selected = _openPaletteDialog.ShowDialog() is DialogResult.OK;
        FilePath = _openPaletteDialog.FileName;
        return selected;
    }
    public static bool ShowOpenAudioDialog(out string FilePath)
    {
        bool selected = _openAudioDialog.ShowDialog() is DialogResult.OK;
        FilePath = _openAudioDialog.FileName;
        return selected;
    }
    public static bool ShowAnimationFolderBrowserDialog(out string FolderPath)
    {
        bool selected = _animationFolderBrowserDialog.ShowDialog() is DialogResult.OK;
        FolderPath = _animationFolderBrowserDialog.SelectedPath;
        return selected;
    }
    public static bool ShowFfmpegPathSelectorDialog(out string ExePath)
    {
        bool selected = _ffmpegSelectorDialog.ShowDialog() is DialogResult.OK;
        ExePath = _ffmpegSelectorDialog.FileName;
        return selected;
    }

}
