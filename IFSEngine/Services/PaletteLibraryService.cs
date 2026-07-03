using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IFSEngine.Model;
using IFSEngine.Serialization;

using Newtonsoft.Json;

namespace IFSEngine.Services;

/// <summary>
/// Manages the palette library using per-collection JSON files.
/// </summary>
public class PaletteLibraryService
{
    private readonly string _libraryPath;
    private readonly string _favoritesFilePath;
    private readonly JsonSerializerSettings _jsonSettings;
    private HashSet<Guid> _favorites;
    private bool _favoritesLoaded;

    /// <summary>
    /// Creates a new PaletteLibraryService.
    /// </summary>
    /// <param name="libraryPath">Root directory for palette files (e.g., "Library/Palettes/").</param>
    public PaletteLibraryService(string libraryPath)
    {
        _libraryPath = libraryPath;
        _favoritesFilePath = Path.Combine(libraryPath, "favorites.json");
        _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        _favorites = [];
        _favoritesLoaded = false;
    }

    public async Task<List<PaletteCollection>> LoadCollectionsAsync()
    {
        EnsureDirectoryExists();
        var collections = await LoadAllCollectionFilesAsync();
        await LoadFavoritesAsync();
        return collections;
    }

    public async Task<List<PaletteCollection>> ReloadCollectionsAsync()
    {
        EnsureDirectoryExists();
        var collections = await LoadAllCollectionFilesAsync();
        return collections;
    }

    public async Task SaveCollectionAsync(PaletteCollection collection)
    {
        string filePath = GetCollectionFilePath(collection.Id);
        string json = JsonConvert.SerializeObject(collection, _jsonSettings);
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
    }

    public async Task<PaletteCollection> AddCollectionAsync(string name, string description = "")
    {
        EnsureDirectoryExists();
        var collection = new PaletteCollection
        {
            Name = name,
            Description = description,
            Author = Author.Unknown
        };
        await SaveCollectionAsync(collection);
        return collection;
    }

    public async Task RemoveCollectionAsync(Guid collectionId)
    {
        string filePath = GetCollectionFilePath(collectionId);
        if (File.Exists(filePath))
            await Task.Run(() => File.Delete(filePath));
    }

    public async Task AddPaletteAsync(Guid collectionId, ColorPalette palette)
    {
        string filePath = GetCollectionFilePath(collectionId);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Collection file not found: {filePath}");

        string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        var collection = JsonConvert.DeserializeObject<PaletteCollection>(json, _jsonSettings);
        if (collection == null)
            throw new InvalidDataException($"Failed to deserialize collection: {filePath}");

        collection.Palettes.Add(palette);
        await SaveCollectionAsync(collection);
    }

    public async Task UpdatePaletteAsync(ColorPalette palette, Guid collectionId)
    {
        string filePath = GetCollectionFilePath(collectionId);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Collection file not found: {filePath}");

        string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        var collection = JsonConvert.DeserializeObject<PaletteCollection>(json, _jsonSettings);
        if (collection == null)
            throw new InvalidDataException($"Failed to deserialize collection: {filePath}");

        var existing = collection.Palettes.FirstOrDefault(p => p.Id == palette.Id);
        if (existing != null)
        {
            collection.Palettes.Remove(existing);
            collection.Palettes.Add(palette);
        }
        else
        {
            collection.Palettes.Add(palette);
        }

        await SaveCollectionAsync(collection);
    }

    public async Task RemovePaletteAsync(Guid paletteId)
    {
        // Search all collections for the palette
        string[] files = Directory.GetFiles(_libraryPath, "collection-*.json");
        foreach (string filePath in files)
        {
            string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var collection = JsonConvert.DeserializeObject<PaletteCollection>(json, _jsonSettings);
            if (collection == null)
                continue;

            var palette = collection.Palettes.FirstOrDefault(p => p.Id == paletteId);
            if (palette != null)
            {
                collection.Palettes.Remove(palette);
                await SaveCollectionAsync(collection);
                return;
            }
        }
    }

    public async Task MovePaletteAsync(Guid paletteId, Guid sourceCollectionId, Guid targetCollectionId)
    {
        // Load source collection
        string sourcePath = GetCollectionFilePath(sourceCollectionId);
        string sourceJson = await File.ReadAllTextAsync(sourcePath, Encoding.UTF8);
        var sourceCollection = JsonConvert.DeserializeObject<PaletteCollection>(sourceJson, _jsonSettings);
        if (sourceCollection == null)
            throw new InvalidDataException($"Failed to deserialize source collection: {sourcePath}");

        var palette = sourceCollection.Palettes.FirstOrDefault(p => p.Id == paletteId);
        if (palette == null)
            throw new InvalidOperationException($"Palette {paletteId} not found in source collection.");

        sourceCollection.Palettes.Remove(palette);
        await SaveCollectionAsync(sourceCollection);

        // Add to target collection
        await AddPaletteAsync(targetCollectionId, palette);
    }

    public async Task SetFavoriteAsync(Guid paletteId, bool isFavorite)
    {
        await LoadFavoritesAsync();
        if (isFavorite)
            _favorites.Add(paletteId);
        else
            _favorites.Remove(paletteId);
        await SaveFavoritesAsync();
    }

    public async Task<HashSet<Guid>> GetFavoritesAsync()
    {
        await LoadFavoritesAsync();
        return _favorites;
    }

    public async Task<PaletteCollection> ImportFlameFileAsync(string filePath)
    {
        var collection = await FlameFormatImporter.ImportFromFileAsync(filePath);
        await SaveCollectionAsync(collection);
        return collection;
    }

    public async Task ExportCollectionAsync(Guid collectionId, string filePath)
    {
        string collectionPath = GetCollectionFilePath(collectionId);
        string json = await File.ReadAllTextAsync(collectionPath, Encoding.UTF8);
        var collection = JsonConvert.DeserializeObject<PaletteCollection>(json, _jsonSettings);
        if (collection == null)
            throw new InvalidDataException($"Failed to deserialize collection: {collectionPath}");

        await FlameFormatExporter.ExportCollectionAsync(collection, filePath);
    }

    // --- Internal helpers ---

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_libraryPath))
            Directory.CreateDirectory(_libraryPath);
    }

    private string GetCollectionFilePath(Guid collectionId)
    {
        return Path.Combine(_libraryPath, $"collection-{collectionId}.json");
    }

    private async Task<List<PaletteCollection>> LoadAllCollectionFilesAsync()
    {
        var collections = new List<PaletteCollection>();
        EnsureDirectoryExists();

        string[] files = Directory.GetFiles(_libraryPath, "collection-*.json");
        foreach (string filePath in files)
        {
            try
            {
                string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                var collection = JsonConvert.DeserializeObject<PaletteCollection>(json, _jsonSettings);
                if (collection != null)
                    collections.Add(collection);
            }
            catch
            {
                // Skip corrupted files
            }
        }

        return collections;
    }

    private async Task LoadFavoritesAsync()
    {
        if (_favoritesLoaded)
            return;

        if (File.Exists(_favoritesFilePath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(_favoritesFilePath, Encoding.UTF8);
                _favorites = JsonConvert.DeserializeObject<HashSet<Guid>>(json) ?? [];
            }
            catch
            {
                _favorites = [];
            }
        }
        else
        {
            _favorites = [];
        }

        _favoritesLoaded = true;
    }

    private async Task SaveFavoritesAsync()
    {
        EnsureDirectoryExists();
        string json = JsonConvert.SerializeObject(_favorites, _jsonSettings);
        await File.WriteAllTextAsync(_favoritesFilePath, json, Encoding.UTF8);
    }
}
