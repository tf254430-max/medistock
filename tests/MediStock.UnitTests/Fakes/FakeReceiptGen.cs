using MediStock.Application.Interfaces;

namespace MediStock.UnitTests.Fakes;

public class FakeReceiptGen : IReceiptNumberGenerator
{
    private int _counter;
    public Task<string> NextAsync(CancellationToken ct = default)
    {
        var n = Interlocked.Increment(ref _counter);
        return Task.FromResult($"R-TEST-{n:D6}");
    }
}
