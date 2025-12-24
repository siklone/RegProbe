using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core.Cloud;

/// <summary>
/// Client for interacting with the cloud preset repository
/// Enables downloading community presets, uploading custom presets, and accessing crowdsourced optimization data
/// </summary>
public sealed class PresetRepositoryClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly string? _apiKey;
    private bool _disposed;

    public PresetRepositoryClient(string apiBaseUrl = "https://api.windowsoptimizer.com", string? apiKey = null)
    {
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        _apiKey = apiKey;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        }

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "WindowsOptimizer/1.0");
    }

    /// <summary>
    /// Search for presets in the repository
    /// </summary>
    public async Task<PresetSearchResult> SearchPresetsAsync(PresetSearchFilter filter, CancellationToken ct)
    {
        try
        {
            var queryParams = BuildSearchQueryParams(filter);
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/presets/search?{queryParams}", ct);

            if (!response.IsSuccessStatusCode)
            {
                return new PresetSearchResult
                {
                    Presets = new List<CloudPreset>(),
                    TotalCount = 0,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalPages = 0
                };
            }

            var result = await response.Content.ReadFromJsonAsync<PresetSearchResult>(cancellationToken: ct);
            return result ?? new PresetSearchResult
            {
                Presets = new List<CloudPreset>(),
                TotalCount = 0,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = 0
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Preset search failed: {ex.Message}");
            return new PresetSearchResult
            {
                Presets = new List<CloudPreset>(),
                TotalCount = 0,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = 0
            };
        }
    }

    /// <summary>
    /// Download a specific preset by ID
    /// </summary>
    public async Task<PresetDownloadResult> DownloadPresetAsync(string presetId, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/presets/{presetId}", ct);

            if (!response.IsSuccessStatusCode)
            {
                return new PresetDownloadResult
                {
                    Success = false,
                    ErrorMessage = $"HTTP {response.StatusCode}",
                    IsSignatureValid = false,
                    DownloadedAt = DateTime.UtcNow
                };
            }

            var preset = await response.Content.ReadFromJsonAsync<CloudPreset>(cancellationToken: ct);

            if (preset == null)
            {
                return new PresetDownloadResult
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize preset",
                    IsSignatureValid = false,
                    DownloadedAt = DateTime.UtcNow
                };
            }

            // Verify digital signature if present
            var isSignatureValid = string.IsNullOrEmpty(preset.DigitalSignature)
                ? false
                : VerifyPresetSignature(preset);

            // Track download count (fire and forget)
            _ = TrackDownloadAsync(presetId, ct);

            return new PresetDownloadResult
            {
                Success = true,
                Preset = preset,
                IsSignatureValid = isSignatureValid,
                DownloadedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new PresetDownloadResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                IsSignatureValid = false,
                DownloadedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Upload a custom preset to the repository (requires authentication)
    /// </summary>
    public async Task<PresetUploadResult> UploadPresetAsync(CloudPreset preset, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new PresetUploadResult
                {
                    Success = false,
                    ErrorMessage = "Authentication required to upload presets",
                    UploadedAt = DateTime.UtcNow
                };
            }

            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/v1/presets", preset, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                return new PresetUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Upload failed: {response.StatusCode} - {errorContent}",
                    UploadedAt = DateTime.UtcNow
                };
            }

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: ct);
            var presetId = result?.GetValueOrDefault("id") ?? string.Empty;

            return new PresetUploadResult
            {
                Success = true,
                PresetId = presetId,
                UploadedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new PresetUploadResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                UploadedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Submit a rating for a preset
    /// </summary>
    public async Task<bool> SubmitRatingAsync(PresetRating rating, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return false; // Requires authentication
            }

            var response = await _httpClient.PostAsJsonAsync(
                $"{_apiBaseUrl}/api/v1/presets/{rating.PresetId}/ratings",
                rating,
                ct
            );

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get crowdsourced effectiveness data for a specific tweak
    /// </summary>
    public async Task<TweakEffectivenessData?> GetTweakEffectivenessAsync(string tweakId, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/tweaks/{tweakId}/effectiveness", ct);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TweakEffectivenessData>(cancellationToken: ct);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get featured presets (editor's choice, trending, etc.)
    /// </summary>
    public async Task<List<CloudPreset>> GetFeaturedPresetsAsync(CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/v1/presets/featured", ct);

            if (!response.IsSuccessStatusCode)
            {
                return new List<CloudPreset>();
            }

            return await response.Content.ReadFromJsonAsync<List<CloudPreset>>(cancellationToken: ct)
                ?? new List<CloudPreset>();
        }
        catch
        {
            return new List<CloudPreset>();
        }
    }

    /// <summary>
    /// Get presets by category
    /// </summary>
    public async Task<List<CloudPreset>> GetPresetsByCategoryAsync(PresetCategory category, int limit = 10, CancellationToken ct = default)
    {
        var filter = new PresetSearchFilter
        {
            Category = category,
            PageSize = limit,
            PageNumber = 1,
            SortOrder = PresetSortOrder.HighestRated
        };

        var result = await SearchPresetsAsync(filter, ct);
        return result.Presets;
    }

    /// <summary>
    /// Check if a newer version of a preset is available
    /// </summary>
    public async Task<(bool HasUpdate, CloudPreset? NewVersion)> CheckForPresetUpdateAsync(string presetId, string currentVersion, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_apiBaseUrl}/api/v1/presets/{presetId}/version?current={currentVersion}",
                ct
            );

            if (!response.IsSuccessStatusCode)
            {
                return (false, null);
            }

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: ct);
            var hasUpdate = result?.GetValueOrDefault("hasUpdate")?.ToString() == "True";

            if (hasUpdate)
            {
                var downloadResult = await DownloadPresetAsync(presetId, ct);
                return (true, downloadResult.Preset);
            }

            return (false, null);
        }
        catch
        {
            return (false, null);
        }
    }

    /// <summary>
    /// Track a preset download (increments download counter)
    /// </summary>
    private async Task TrackDownloadAsync(string presetId, CancellationToken ct)
    {
        try
        {
            await _httpClient.PostAsync($"{_apiBaseUrl}/api/v1/presets/{presetId}/download", null, ct);
        }
        catch
        {
            // Fire and forget - don't fail if tracking fails
        }
    }

    /// <summary>
    /// Verify the digital signature of a preset
    /// </summary>
    private bool VerifyPresetSignature(CloudPreset preset)
    {
        // TODO: Implement proper Authenticode or PGP signature verification
        // For now, just check if signature exists and is not empty
        return !string.IsNullOrEmpty(preset.DigitalSignature);
    }

    /// <summary>
    /// Build query parameters for search
    /// </summary>
    private string BuildSearchQueryParams(PresetSearchFilter filter)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(filter.Query))
        {
            queryParams.Add($"q={Uri.EscapeDataString(filter.Query)}");
        }

        if (filter.Category.HasValue)
        {
            queryParams.Add($"category={filter.Category.Value}");
        }

        if (filter.Tags?.Count > 0)
        {
            queryParams.Add($"tags={string.Join(",", filter.Tags)}");
        }

        if (filter.MinRating.HasValue)
        {
            queryParams.Add($"minRating={filter.MinRating.Value}");
        }

        if (filter.VerifiedOnly == true)
        {
            queryParams.Add("verified=true");
        }

        if (filter.OfficialOnly == true)
        {
            queryParams.Add("official=true");
        }

        queryParams.Add($"sort={filter.SortOrder}");
        queryParams.Add($"page={filter.PageNumber}");
        queryParams.Add($"pageSize={filter.PageSize}");

        return string.Join("&", queryParams);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
