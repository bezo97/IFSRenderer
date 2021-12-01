using IFSEngine.Generation;
using IFSEngine.Model;
using IFSEngine.Rendering;
using IFSEngine.Serialization;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WpfDisplay.Helper;
using WpfDisplay.Properties;

namespace WpfDisplay.Models
{
    /// <summary>
    /// The main workspace model that contains a <see cref="RendererGL"/> 
    /// and an <see cref="IFSEngine.Model.IFS"/> that it is rendering.
    /// </summary>
    [ObservableObject]
    public partial class Workspace
    {
        private readonly IFSHistoryTracker tracker = new();
        private List<Transform> loadedTransforms = new();

        public event EventHandler<string> StatusTextChanged;
        public string TransformsDirectoryPath { get; } = Path.Combine(App.AppDataPath, "Transforms");
        public IReadOnlyCollection<Transform> LoadedTransforms => loadedTransforms;
        public Author CurrentUser { get; set; } = Author.Unknown;
        public bool InvertAxisX, InvertAxisY, InvertAxisZ;
        public double Sensitivity;

        private RendererGL renderer;
        public RendererGL Renderer
        {
            get => renderer;
            set
            {
                SetProperty(ref renderer, value);
                if (Ifs != null)
                    renderer.LoadParams(Ifs);
            }
        }


        [ObservableProperty] private IFS _ifs;

        public bool IsHistoryUndoable => tracker.IsHistoryUndoable;
        public bool IsHistoryRedoable => tracker.IsHistoryRedoable;

        public Workspace(RendererGL r)
        {
            LoadTransformLibrary();
            Renderer = r;
            Renderer.Initialize(loadedTransforms);
            Ifs = new IFS();
            Renderer.LoadParams(Ifs);
            LoadUserSettings();
        }

        public async Task ReloadTransforms()
        {
            LoadTransformLibrary();
            Ifs.ReloadTransforms(LoadedTransforms);
            await Renderer.LoadTransforms(LoadedTransforms);
            OnPropertyChanged(nameof(LoadedTransforms));
        }

        private void LoadTransformLibrary()
        {
            loadedTransforms = Directory
                .GetFiles(TransformsDirectoryPath, "*.ifstf", SearchOption.AllDirectories)
                .Select(file => Transform.FromFile(file))
                .ToList();
            OnPropertyChanged(nameof(LoadedTransforms));
        }

        public void LoadParams(IFS ifs)
        {
            TakeSnapshot();
            renderer?.LoadParams(ifs);
            Ifs = ifs;
            if (!Renderer.IsRendering)
                Renderer.StartRenderLoop();
        }

        public async Task LoadParamsFileAsync(string path)
        {
            IFS ifs;
            try
            {
                ifs = await IfsSerializer.LoadJsonFileAsync(path, LoadedTransforms, false);
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                if (System.Windows.MessageBox.Show("Loading params failed. Try again and ignore transform versions?", "Loading failed", System.Windows.MessageBoxButton.OKCancel)
                    == System.Windows.MessageBoxResult.OK)
                {
                    ifs = await IfsSerializer.LoadJsonFileAsync(path, LoadedTransforms, true);
                }
                else
                    throw;
            }
            LoadParams(ifs);
        }

        public void LoadBlankParams()
        {
            LoadParams(new IFS());
        }

        public void LoadRandomParams()
        {
            Generator g = new Generator(LoadedTransforms);
            IFS r = g.GenerateOne(new GeneratorOptions());
            r.ImageResolution = new System.Drawing.Size(1920, 1080);
            LoadParams(r);
        }

        public async Task SaveParamsFileAsync(string path)
        {
            Ifs.AddAuthor(CurrentUser);
            await IfsSerializer.SaveJsonFileAsync(Ifs, path);
        }

        public void UndoHistory()
        {
            //LoadParams without taking snapshot
            Ifs = tracker.Undo(Ifs, LoadedTransforms);
            renderer?.LoadParams(Ifs);
        }

        public void RedoHistory()
        {
            //LoadParams without taking snapshot
            Ifs = tracker.Redo(Ifs, LoadedTransforms);
            renderer?.LoadParams(Ifs);
        }

        public void ClearHistory()
        {
            tracker.Clear();
        }

        public void TakeSnapshot()
        {
            tracker.TakeSnapshot(Ifs);
        }

        public void LoadUserSettings()
        {
            if (!ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).HasFile)
            {//migrate user settings from previous version
                Settings.Default.Upgrade();
                Settings.Default.Save();
            }
            Renderer.EnablePerceptualUpdates = Settings.Default.PerceptuallyUniformUpdates;
            Renderer.SetWorkgroupCount(Settings.Default.WorkgroupCount).Wait();
            Renderer.TargetFramerate = Settings.Default.TargetFramerate;
            InvertAxisX = Settings.Default.InvertAxisX;
            InvertAxisY = Settings.Default.InvertAxisY;
            InvertAxisZ = Settings.Default.InvertAxisZ;
            Sensitivity = Settings.Default.Sensitivity;
            CurrentUser = new Author
            {
                Name = Settings.Default.AuthorName,
                Link = Settings.Default.AuthorLink
            };
        }

        public void UpdateStatusText(string statusText)
        {
            StatusTextChanged?.Invoke(this, statusText);
        }

    }
}
