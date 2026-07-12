namespace Tekram.E2E;

using Xunit;

/// <summary>
/// Shared live-API gating for the black-box e2e suite (issue #60).
///
/// <see cref="LiveFactAttribute"/> replaces the old per-fact skip attributes: when
/// E2E_BASE_URL is unset the fact is reported as SKIPPED (a bare `dotnet test` without
/// a live lane stack stays green, per TD-008); when it is set the fact runs like a
/// plain [Fact].
///
/// <see cref="LiveApiTestBase"/> is the single place HttpClient base-address setup
/// lives. Live test classes derive from it and use <see cref="Client"/>, or
/// <see cref="NewClient"/> when a second, differently-configured client is needed
/// (e.g. one carrying an Authorization header).
/// </summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(LiveApiTestBase.BaseUrl))
            Skip = "E2E_BASE_URL not set — no live lane API to test against.";
    }
}

public abstract class LiveApiTestBase : IDisposable
{
    public static string? BaseUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/');

    private HttpClient? _client;
    protected HttpClient Client => _client ??= NewClient();

    protected LiveApiTestBase() { }

    protected static HttpClient NewClient() =>
        new()
        {
            BaseAddress = new Uri(BaseUrl
                ?? throw new InvalidOperationException("E2E_BASE_URL is not set")),
        };

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
