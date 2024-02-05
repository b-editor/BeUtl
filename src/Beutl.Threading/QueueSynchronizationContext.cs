﻿namespace Beutl.Threading;

internal sealed class QueueSynchronizationContext : SynchronizationContext
{
    private readonly OperationQueue _operationQueue = new();
    private readonly TimerQueue _timerQueue = new();

    private bool _running;
    private CancellationTokenSource? _waitToken;

    internal void Start()
    {
        _running = true;

        while (_running)
        {
            ExecuteAvailableOperations();
            WaitForPendingOperations();
        }
    }

    internal void Execute()
    {
        _running = true;
        ExecuteAvailableOperations();
    }

    internal void Stop()
    {
        _running = false;
        _waitToken?.Cancel();
    }

    private void ExecuteAvailableOperations()
    {
        FlushTimerQueue();

        while (_running && _operationQueue.TryDequeue(out DispatcherOperation? operation))
        {
            operation.Run();
            FlushTimerQueue();
        }
    }

    private void WaitForPendingOperations()
    {
        if (!_running)
            return;

        _waitToken = new CancellationTokenSource();

        if (_timerQueue.Next is DateTime next)
            _waitToken.CancelAfter(next - DateTime.UtcNow);

        _waitToken.Token.WaitHandle.WaitOne();
        _waitToken = null;
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        Send(DispatchPriority.High, () => d(state), default).Wait();
    }

    internal Task Send(DispatchPriority priority, Action operation, CancellationToken ct)
    {
        var task = new Task(operation, ct);
        Post(priority, () => task.RunSynchronously(), ct);
        return task;
    }

    internal Task<T> Send<T>(DispatchPriority priority, Func<T> operation, CancellationToken ct)
    {
        var task = new Task<T>(operation, ct);
        Post(priority, () => task.RunSynchronously(), ct);
        return task;
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        Post(DispatchPriority.High, () => d(state), default);
    }

    internal void Post(DispatchPriority priority, Action operation, CancellationToken ct)
    {
        _operationQueue.Enqueue(new(operation, priority, ct));
        _waitToken?.Cancel();
    }

    internal void Post(DispatcherOperation operation)
    {
        _operationQueue.Enqueue(operation);
        _waitToken?.Cancel();
    }

    internal void PostDelayed(DateTime dateTime, DispatchPriority priority, Action action, CancellationToken ct)
    {
        _timerQueue.Enqueue(dateTime, priority, action, ct);
        _waitToken?.Cancel();
    }

    internal bool HasQueuedTasks(DispatchPriority priority)
    {
        return _operationQueue.Any(priority);
    }

    private void FlushTimerQueue()
    {
        while (_timerQueue.TryDequeue(out List<DispatcherOperation>? operations))
        {
            foreach (DispatcherOperation operation in operations)
            {
                Post(operation);
            }
        }
    }
}
