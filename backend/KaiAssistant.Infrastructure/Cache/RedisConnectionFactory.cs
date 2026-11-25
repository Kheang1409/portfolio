using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
namespace KaiAssistant.Infrastructure.Cache;

public sealed class RedisConnectionFactory : IRedisConnectionFactory
{
    private const int MaxConnectAttempts = 3;
    private static readonly TimeSpan WarningWindow = TimeSpan.FromMinutes(1);
    private readonly ILogger<RedisConnectionFactory> _logger;
    private readonly Lazy<Task<IConnectionMultiplexer?>> _lazyConnection;
    private readonly ConfigurationOptions? _options;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastWarningAt = new(StringComparer.Ordinal);
    public RedisConnectionFactory(IConfiguration configuration, IHostEnvironment environment, ILogger<RedisConnectionFactory> logger)
    {
        _logger = logger;
        var redisConnectionString = Environment.GetEnvironmentVariable("REDIS__CONNECTIONSTRING") ?? configuration["Redis:ConnectionString"];
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            IsConfigured = false;
            _lazyConnection = new Lazy<Task<IConnectionMultiplexer?>>(() => Task.FromResult<IConnectionMultiplexer?>(null));
            RedisMetrics.SetConnected(false);
            return;
        }
        if (!environment.IsDevelopment() &&
            !redisConnectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Redis connection must use rediss:// in non-development environments.");
        }
        _options = BuildOptions(redisConnectionString, environment.IsDevelopment());
        IsConfigured = true;
        _lazyConnection = new Lazy<Task<IConnectionMultiplexer?>>(ConnectWithRetryAsync);
    }
    public bool IsConfigured { get; }
    public bool IsConnected
    {
        get
        {
            if (!_lazyConnection.IsValueCreated)
            {
                return false;
            }
            var task = _lazyConnection.Value;
            if (!task.IsCompletedSuccessfully || task.Result is null)
            {
                return false;
            }
            return task.Result.IsConnected;
        }
    }
    public async ValueTask<IConnectionMultiplexer?> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return null;
        }
        var connection = await _lazyConnection.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        RedisMetrics.SetConnected(connection?.IsConnected == true);
        return connection;
    }
    private async Task<IConnectionMultiplexer?> ConnectWithRetryAsync()
    {
        if (_options is null)
        {
            return null;
        }
        for (var attempt = 1; attempt <= MaxConnectAttempts; attempt++)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var connection = await ConnectionMultiplexer.ConnectAsync(_options).ConfigureAwait(false);
                sw.Stop();
                RedisMetrics.RecordLatency(sw.Elapsed.TotalMilliseconds, "connect");
                RedisMetrics.SetConnected(connection.IsConnected);
                _logger.LogInformation("Redis connected on attempt {Attempt}.", attempt);
                return connection;
            }
            catch (Exception ex)
            {
                sw.Stop();
                RedisMetrics.RecordFailure("connect");
                RedisMetrics.SetConnected(false);
                if (ShouldLog("connect"))
                {
                    _logger.LogWarning(ex, "Redis connection attempt {Attempt} failed; continuing with fallback paths.", attempt);
                }
                if (attempt < MaxConnectAttempts)
                {
                    var delay = TimeSpan.FromMilliseconds(Math.Min(5000, 500 * Math.Pow(2, attempt - 1)));
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }
        }
        return null;
    }
    private bool ShouldLog(string key)
    {
        var now = DateTimeOffset.UtcNow;
        var last = _lastWarningAt.GetOrAdd(key, DateTimeOffset.MinValue);
        if (now - last < WarningWindow)
        {
            return false;
        }
        _lastWarningAt[key] = now;
        return true;
    }
    private static ConfigurationOptions BuildOptions(string connectionString, bool isDevelopment)
    {
        var options = BuildOptionsFromConnectionString(connectionString);
        options.AbortOnConnectFail = false;
        options.ConnectRetry = Math.Max(options.ConnectRetry, 5);
        options.ConnectTimeout = Math.Max(options.ConnectTimeout, 5000);
        options.SyncTimeout = Math.Max(options.SyncTimeout, 5000);
        options.AsyncTimeout = Math.Max(options.AsyncTimeout, 5000);
        options.KeepAlive = Math.Max(options.KeepAlive, 30);
        options.ReconnectRetryPolicy = new ExponentialRetry(5000);
        if (connectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase) || !isDevelopment)
        {
            options.Ssl = true;
            // For local production testing with self-signed certificates, accept any certificate
            // In real production, this should use proper CA-signed certificates
            if (isDevelopment || Environment.GetEnvironmentVariable("ALLOW_SELF_SIGNED_REDIS_CERT") == "true")
            {
                options.SslClientAuthenticationOptions = _ => new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true
                };
            }
        }
        return options;
    }
    private static ConfigurationOptions BuildOptionsFromConnectionString(string connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
            && (string.Equals(uri.Scheme, "redis", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, "rediss", StringComparison.OrdinalIgnoreCase)))
        {
            var options = new ConfigurationOptions();
            var host = uri.Host;
            if (!string.IsNullOrWhiteSpace(host))
            {
                var port = uri.IsDefaultPort ? 6379 : uri.Port;
                options.EndPoints.Add(host, port);
            }
            var userInfo = uri.UserInfo;
            if (!string.IsNullOrWhiteSpace(userInfo))
            {
                var parts = userInfo.Split(':', 2);
                if (parts.Length == 2)
                {
                    options.User = Uri.UnescapeDataString(parts[0]);
                    options.Password = Uri.UnescapeDataString(parts[1]);
                }
                else
                {
                    options.Password = Uri.UnescapeDataString(parts[0]);
                }
            }
            var path = uri.AbsolutePath.Trim('/');
            if (int.TryParse(path, out var db))
            {
                options.DefaultDatabase = db;
            }
            return options;
        }
        return ConfigurationOptions.Parse(connectionString, true);
    }
}