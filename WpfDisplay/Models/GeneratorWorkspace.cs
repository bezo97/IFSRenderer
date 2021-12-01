﻿using IFSEngine.Generation;
using IFSEngine.Model;
using IFSEngine.Rendering;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using OpenTK.Windowing.Desktop;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfDisplay.Models;

/// <summary>
/// The generator workspace model that contains a utility <see cref="RendererGL"/> 
/// and lists of <see cref="IFSEngine.Model.IFS"/> that it is rendering.
/// </summary>
[ObservableObject]
public partial class GeneratorWorkspace
{
    public IReadOnlyList<IFS> PinnedIFS => _pinnedIFS;
    public IReadOnlyList<IFS> GeneratedIFS => _generatedIFS;
    public IReadOnlyDictionary<IFS, ImageSource> Thumbnails => _thumbnails;

    private readonly RendererGL _renderer;
    private readonly Generator _generator;
    private readonly Dictionary<IFS, ImageSource> _thumbnails = new();
    private readonly List<IFS> _pinnedIFS = new();
    private readonly List<IFS> _generatedIFS = new();
    private readonly ConcurrentQueue<IFS> _renderQueue = new();

    /// <summary>
    /// Call <see cref="Initialize"/> before using
    /// </summary>
    /// <param name="loadedTransforms"></param>
    public GeneratorWorkspace(IReadOnlyCollection<IFSEngine.Model.Transform> loadedTransforms)
    {
        //init thumbnail renderer
        GameWindow hw = new(new GameWindowSettings
        {
            IsMultiThreaded = true
        }, new NativeWindowSettings
        {
            Flags = OpenTK.Windowing.Common.ContextFlags.Offscreen,
            IsEventDriven = true,
            StartFocused = false,
            StartVisible = false,
            WindowState = OpenTK.Windowing.Common.WindowState.Minimized
        });
        _renderer = new RendererGL(hw.Context);
        _renderer.SetDisplayResolution(200, 200);
        var transforms = loadedTransforms.ToList();
        _generator = new Generator(transforms);
    }

    public async Task Initialize()
    {
        await _renderer.Initialize(_generator.SelectedTransforms);
        //performance settings
        await _renderer.SetWorkgroupCount(10);
    }

    public void GenerateNewRandomBatch(GeneratorOptions options)
    {
        options.baseParams = _pinnedIFS.LastOrDefault() ?? options.baseParams;//TODO: use selection of pinned fractals
        _generatedIFS.Clear();

        foreach (var r in _generator.GenerateBatch(options, 30))
        {
            _generatedIFS.Add(r);
            _renderQueue.Enqueue(r);
        }
    }

    public void PinIFS(IFS ifs)
    {
        _pinnedIFS.Add(ifs);
        if (!_thumbnails.ContainsKey(ifs))
            _renderQueue.Enqueue(ifs);
    }

    public void UnpinIFS(IFS ifs)
    {
        _pinnedIFS.Remove(ifs);
    }

    public void processQueue()
    {
        //TODO: separate thread, make context current
        lock (_renderer)
        {
            while (_renderQueue.TryDequeue(out IFS ifs))
            {
                _renderer.LoadParams(ifs);
                _renderer.SetHistogramScaleToDisplay();
                _renderer.DispatchCompute();
                _renderer.RenderImage();
                WriteableBitmap wbm = new WriteableBitmap(_renderer.HistogramWidth, _renderer.HistogramHeight, 96, 96, PixelFormats.Bgra32, null);
                _renderer.CopyPixelDataToBitmap(wbm.BackBuffer).Wait();
                wbm.Freeze();
                var thumbnail = new FormatConvertedBitmap(wbm, PixelFormats.Bgr32, null, 0);
                _thumbnails[ifs] = thumbnail;
                OnPropertyChanged(nameof(Thumbnails));
            }
        }
        //cleanup old thumbnails
        var removedParams = _thumbnails.Keys.Where(k => !(_pinnedIFS.Contains(k) || _generatedIFS.Contains(k))).ToList();
        foreach (var t in removedParams)
            _thumbnails.Remove(t);

    }
}
