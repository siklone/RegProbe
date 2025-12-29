using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WindowsOptimizer.Infrastructure;

/// <summary>
/// Interface for storing and retrieving favorite tweak IDs.
/// </summary>
public interface IFavoritesStore
{
    /// <summary>
    /// Gets the set of favorite tweak IDs.
    /// </summary>
    HashSet<string> GetFavorites();

    /// <summary>
    /// Checks if a tweak ID is a favorite.
    /// </summary>
    bool IsFavorite(string tweakId);

    /// <summary>
    /// Adds a tweak ID to favorites.
    /// </summary>
    void AddFavorite(string tweakId);

    /// <summary>
    /// Removes a tweak ID from favorites.
    /// </summary>
    void RemoveFavorite(string tweakId);

    /// <summary>
    /// Toggles a tweak ID's favorite status.
    /// </summary>
    bool ToggleFavorite(string tweakId);

    /// <summary>
    /// Saves favorites to persistent storage.
    /// </summary>
    void Save();
}

/// <summary>
/// File-based implementation of IFavoritesStore.
/// Stores favorites as a JSON file in the app data directory.
/// </summary>
public sealed class FavoritesStore : IFavoritesStore
{
    private readonly string _filePath;
    private readonly HashSet<string> _favorites;
    private readonly object _lock = new();

    public FavoritesStore(AppPaths paths)
    {
        _filePath = Path.Combine(paths.AppDataRoot, "favorites.json");
        _favorites = Load();
    }

    public HashSet<string> GetFavorites()
    {
        lock (_lock)
        {
            return new HashSet<string>(_favorites);
        }
    }

    public bool IsFavorite(string tweakId)
    {
        if (string.IsNullOrEmpty(tweakId))
            return false;

        lock (_lock)
        {
            return _favorites.Contains(tweakId);
        }
    }

    public void AddFavorite(string tweakId)
    {
        if (string.IsNullOrEmpty(tweakId))
            return;

        lock (_lock)
        {
            if (_favorites.Add(tweakId))
            {
                Save();
            }
        }
    }

    public void RemoveFavorite(string tweakId)
    {
        if (string.IsNullOrEmpty(tweakId))
            return;

        lock (_lock)
        {
            if (_favorites.Remove(tweakId))
            {
                Save();
            }
        }
    }

    public bool ToggleFavorite(string tweakId)
    {
        if (string.IsNullOrEmpty(tweakId))
            return false;

        lock (_lock)
        {
            bool isFavorite;
            if (_favorites.Contains(tweakId))
            {
                _favorites.Remove(tweakId);
                isFavorite = false;
            }
            else
            {
                _favorites.Add(tweakId);
                isFavorite = true;
            }
            Save();
            return isFavorite;
        }
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_favorites, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Ignore save errors - favorites will be lost on restart but app continues
        }
    }

    private HashSet<string> Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var favorites = JsonSerializer.Deserialize<HashSet<string>>(json);
                return favorites ?? new HashSet<string>();
            }
        }
        catch
        {
            // Ignore load errors - start with empty favorites
        }

        return new HashSet<string>();
    }
}
