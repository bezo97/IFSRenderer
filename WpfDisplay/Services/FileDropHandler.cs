#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using WpfDisplay.Models;

namespace WpfDisplay.Services;

internal class FileDropHandler
{
    private static readonly Dictionary<string, DroppedFile> _acceptedFileExtensions = new()
    {
        { ".gradient", DroppedFile.Palette },
        { ".ifsjson", DroppedFile.Params },
        { ".ifstf", DroppedFile.Transform },
        { ".png", DroppedFile.Image },
        { ".ugr", DroppedFile.Palette },
    };
 
    /// <returns>Copy effect when file extension is supported or None.</returns>
    public static DragDropEffects GetDragDropEffect(IDataObject dataObject)
    {
        var ext = Path.GetExtension(GetSingleFilePath(dataObject)) ?? "";
        if (_acceptedFileExtensions.ContainsKey(ext))
            return DragDropEffects.Copy;
        else
            return DragDropEffects.None;
    }

    /// <returns>True if the dropped file is supported.</returns>
    public static bool IsDropSupported(IDataObject dataObject, out DroppedFile droppedFileType, out string filePath)
    {
        var path = GetSingleFilePath(dataObject);
        filePath = path!;
        var ext = Path.GetExtension(path);
        return _acceptedFileExtensions.TryGetValue(ext ?? "", out droppedFileType);
    }

    /// <returns>A single dragged file's path or null.</returns>
    private static string? GetSingleFilePath(IDataObject dataObject)
    {
        if (dataObject.GetDataPresent(DataFormats.FileDrop, true))
        {
            var filePaths = dataObject.GetData(DataFormats.FileDrop, true) as string[];
            return File.Exists(filePaths?.FirstOrDefault()) ? filePaths[0] : null;
        }
        return null;
    }
}
