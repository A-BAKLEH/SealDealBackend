namespace Web.Processing;

public class TestProcessor
{
    private readonly ILogger<TestProcessor> _logger;
    public TestProcessor(ILogger<TestProcessor> logger)
    {
        _logger = logger;
    }
    public async Task testSlow(CancellationToken cancellationToken)
    {
        //this should be aborted as it doesnt repond to cancellation tokens and takes around 50 seconds
        //should be requeued
        int count = 0;
        do
        {
            _logger.LogWarning("{testWhere} working test lmao with count {count}","slow",count);
            await Task.Delay(1000);
            count++;
        } while (count <= 50);
    }

    public async Task testQuick(CancellationToken cancellationToken)
    {
        //will run for 50 secs but stops at cancellation, so job should be marked complete
        int count = 0;
        do
        {
            _logger.LogWarning("{testWhere} working test lmao with count {count}","quick", count);
            await Task.Delay(1000);
            count++;
        } while (count <= 50 && !cancellationToken.IsCancellationRequested);
    }
}
