using System.Collections.Concurrent;
using Ecunexo.Billing.Infrastructure.Sri;

namespace Ecunexo.Billing.Infrastructure.Sri;

/// <summary>Circuit breaker simple por URL de endpoint SRI.</summary>
public sealed class SriCircuitBreaker
{
    private readonly ConcurrentDictionary<string, BreakerState> _states = new(StringComparer.Ordinal);

    public bool IsOpen(string endpointKey, SriOptions options)
    {
        if (!_states.TryGetValue(endpointKey, out var state))
            return false;

        if (state.OpenedUntil is null)
            return false;

        if (DateTimeOffset.UtcNow >= state.OpenedUntil.Value)
        {
            _states[endpointKey] = state with { OpenedUntil = null, Failures = 0 };
            return false;
        }

        return true;
    }

    public void RecordSuccess(string endpointKey) =>
        _states[endpointKey] = new BreakerState(0, null);

    public void RecordFailure(string endpointKey, SriOptions options)
    {
        var threshold = Math.Max(1, options.CircuitBreakerFailureThreshold);
        var breakSeconds = Math.Clamp(options.CircuitBreakerBreakSeconds, 5, 600);
        var prev = _states.GetOrAdd(endpointKey, _ => new BreakerState(0, null));
        var failures = prev.Failures + 1;
        DateTimeOffset? opened = failures >= threshold
            ? DateTimeOffset.UtcNow.AddSeconds(breakSeconds)
            : null;
        _states[endpointKey] = new BreakerState(failures, opened);
    }

    private sealed record BreakerState(int Failures, DateTimeOffset? OpenedUntil);
}
