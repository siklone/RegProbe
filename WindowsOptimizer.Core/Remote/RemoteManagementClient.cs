using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Core.Remote;

/// <summary>
/// Client for connecting to remote management server
/// Runs on each managed workstation/agent
/// </summary>
public sealed class RemoteManagementClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private readonly string _apiKey;
    private ClientWebSocket? _webSocket;
    private string? _sessionToken;
    private string? _agentId;
    private Timer? _heartbeatTimer;
    private bool _disposed;

    public event EventHandler<RemoteCommand>? CommandReceived;
    public event EventHandler<string>? ConnectionStatusChanged;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public RemoteManagementClient(string serverUrl, string apiKey, string? agentId = null)
    {
        _serverUrl = serverUrl.TrimEnd('/');
        _apiKey = apiKey;
        _agentId = agentId ?? GenerateAgentId();

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "WindowsOptimizerAgent/1.0");
    }

    /// <summary>
    /// Register agent with the management server
    /// </summary>
    public async Task<bool> RegisterAsync(CancellationToken ct)
    {
        try
        {
            var registration = new AgentRegistration
            {
                AgentId = _agentId!,
                MachineName = Environment.MachineName,
                AgentVersion = "1.0.0",
                OsVersion = Environment.OSVersion.ToString(),
                HardwareProfile = GetHardwareProfile(),
                RegisteredAt = DateTime.UtcNow,
                ApiKey = _apiKey
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/v1/agents/register",
                registration,
                ct
            );

            if (!response.IsSuccessStatusCode)
            {
                ConnectionStatusChanged?.Invoke(this, $"Registration failed: {response.StatusCode}");
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<AgentRegistrationResponse>(cancellationToken: ct);

            if (result?.Success == true && result.SessionToken != null)
            {
                _sessionToken = result.SessionToken;
                ConnectionStatusChanged?.Invoke(this, "Registered successfully");

                // Start heartbeat
                StartHeartbeat(result.HeartbeatIntervalSeconds);

                return true;
            }

            ConnectionStatusChanged?.Invoke(this, result?.ErrorMessage ?? "Registration failed");
            return false;
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Registration error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Connect to management server via WebSocket for real-time communication
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(_sessionToken))
            {
                ConnectionStatusChanged?.Invoke(this, "Not registered - call RegisterAsync first");
                return false;
            }

            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("X-Session-Token", _sessionToken);

            var wsUrl = _serverUrl.Replace("https://", "wss://").Replace("http://", "ws://");
            await _webSocket.ConnectAsync(new Uri($"{wsUrl}/ws/agent/{_agentId}"), ct);

            ConnectionStatusChanged?.Invoke(this, "Connected");

            // Start listening for commands
            _ = Task.Run(() => ListenForCommandsAsync(ct), ct);

            return true;
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Connection error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Send status report to server
    /// </summary>
    public async Task<bool> SendStatusReportAsync(AgentStatusReport report, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/v1/agents/{_agentId}/status",
                report,
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
    /// Send command response back to server
    /// </summary>
    public async Task<bool> SendCommandResponseAsync(RemoteCommandResponse response, CancellationToken ct)
    {
        try
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                var json = JsonSerializer.Serialize(response);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    ct
                );
                return true;
            }

            // Fallback to HTTP if WebSocket is not available
            var httpResponse = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/v1/agents/{_agentId}/responses",
                response,
                ct
            );

            return httpResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Send real-time event to server
    /// </summary>
    public async Task SendEventAsync(AgentEvent agentEvent, CancellationToken ct)
    {
        try
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                var json = JsonSerializer.Serialize(agentEvent);
                var bytes = Encoding.UTF8.GetBytes(json);
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    ct
                );
            }
            else
            {
                // Fallback to HTTP
                await _httpClient.PostAsJsonAsync(
                    $"{_serverUrl}/api/v1/agents/{_agentId}/events",
                    agentEvent,
                    ct
                );
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to send event: {ex.Message}");
        }
    }

    /// <summary>
    /// Disconnect from server
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
        }

        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;

        ConnectionStatusChanged?.Invoke(this, "Disconnected");
    }

    /// <summary>
    /// Listen for incoming commands from server
    /// </summary>
    private async Task ListenForCommandsAsync(CancellationToken ct)
    {
        if (_webSocket == null) return;

        var buffer = new byte[4096];

        try
        {
            while (_webSocket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed connection", ct);
                    ConnectionStatusChanged?.Invoke(this, "Server closed connection");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var command = JsonSerializer.Deserialize<RemoteCommand>(json);

                    if (command != null)
                    {
                        CommandReceived?.Invoke(this, command);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Listen error: {ex.Message}");
        }
    }

    /// <summary>
    /// Start heartbeat timer
    /// </summary>
    private void StartHeartbeat(int intervalSeconds)
    {
        _heartbeatTimer?.Dispose();

        _heartbeatTimer = new Timer(
            async _ => await SendHeartbeatAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(intervalSeconds)
        );
    }

    /// <summary>
    /// Send heartbeat to server
    /// </summary>
    private async Task SendHeartbeatAsync()
    {
        try
        {
            await _httpClient.PostAsync(
                $"{_serverUrl}/api/v1/agents/{_agentId}/heartbeat",
                null,
                CancellationToken.None
            );
        }
        catch
        {
            // Heartbeat failure - connection may be lost
        }
    }

    /// <summary>
    /// Generate unique agent ID based on machine
    /// </summary>
    private string GenerateAgentId()
    {
        var machineId = Environment.MachineName + "_" + Environment.UserName;
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(machineId))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Get hardware profile string
    /// </summary>
    private string GetHardwareProfile()
    {
        return $"CPU:{Environment.ProcessorCount}cores_RAM:{GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024 / 1024}GB";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _heartbeatTimer?.Dispose();
            _webSocket?.Dispose();
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
