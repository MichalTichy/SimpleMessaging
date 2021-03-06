﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly.RetryManager;
using SimpleMessaging.Base;

namespace SimpleMessaging.Worker.Worker
{
    public abstract class WorkerBase<TItem,TQueue> : IWorker<TItem> where TQueue : IWorkerQueue<TItem> ,new()
    {
        protected virtual WaitStrategy WaitStrategy => WaitStrategy.LinearWait;
        protected virtual TimeSpan DelayForRequeue { get; set; } = TimeSpan.FromSeconds(1);

        protected virtual bool CanBeTerminated => Queue.IsEmpty();
        protected virtual TimeSpan TerminationTimeout => TimeSpan.FromSeconds(60);

        protected ILogger Logger;
        protected CancellationTokenSource WorkerCancellationTokenSource;

        protected TQueue Queue = new TQueue();

        public async Task<WorkerStatus> GetStatus()
        {
            try
            {

                await WorkStatusSemaphore.WaitAsync();
                return Status;
            }
            finally
            {
                WorkStatusSemaphore.Release();
            }
        }

        protected async Task SetStatus(WorkerStatus status)
        {
            try
            {
                await WorkStatusSemaphore.WaitAsync();
                Status = status;
            }
            finally
            {
                WorkStatusSemaphore.Release();
            }
        }

        protected Thread WorkerThread;

        public WorkerBase(ILogger logger=null)
        {
            Logger = logger;
        }

        protected readonly SemaphoreSlim WorkStatusSemaphore = new SemaphoreSlim(1, 1);
        protected WorkerStatus Status;


        public virtual async Task Add(TItem workItem)
        {
            Queue.Add(workItem);

            Logger?.LogInformation($"{GetType().Name}: item inserted");

            if (await GetStatus() == WorkerStatus.Stopped)
            {
                Logger?.LogDebug($"{GetType().Name}: is currently stopped => starting");
                await Start();
            }
        }


        public virtual async Task Start()
        {
            await WorkStatusSemaphore.WaitAsync();

            try
            {
                if (Status == WorkerStatus.Running)
                {
                    return;
                }

                Logger?.LogDebug($"{GetType().Name}: Started");

                WorkerCancellationTokenSource = new CancellationTokenSource();
                using (ExecutionContext.SuppressFlow())
                {
                    WorkerThread = new Thread(async () =>
                    {
                        var cbPolicy = RetryManager.CircuitBreakerWithRetryAsync<Exception>(
                            WaitStrategy,
                            (exception, span) => Logger?.LogError($"{GetType().Name}: Unhandled exception occured", exception),
                            (exception, span) => Logger?.LogCritical($"{GetType().Name}: Circuit breaker tripped.", exception),
                            () => Logger?.LogInformation($"{GetType().Name}: Circuit breaker restored."));
                        await cbPolicy.ExecuteAsync(async () =>
                            await StartWorkItemProcessingAsync(WorkerCancellationTokenSource.Token)
                        );
                    });

                    WorkerThread.Start();
                    Status = WorkerStatus.Running;
                }
            }
            finally
            {
                WorkStatusSemaphore.Release();
            }
        }

        public virtual async Task Stop()
        {

            WorkerCancellationTokenSource?.Cancel();
            WorkerThread?.Join(TerminationTimeout);

            await SetStatus(WorkerStatus.Stopped);
            Logger?.LogDebug($"{GetType().Name}: Stopped");
        }

        protected virtual async Task StartWorkItemProcessingAsync(CancellationToken cancellationToken)
        {
            do
            {
                if (Queue.IsEmpty())
                {
                    await SetStatus(WorkerStatus.Stopped);
                    Logger?.LogDebug($"{GetType().Name}: No work items to process, stopping.");
                    break;
                }

                if (Queue.TryGet(out var workItem))
                {
                    await ProcessWorkItemAsync(workItem);
                }

            } while (!(cancellationToken.IsCancellationRequested && CanBeTerminated));
        }

        protected abstract Task ProcessWorkItemAsync(TItem workItem);

        protected async Task AddToQueWithDelay(TItem model)
        {
            await Task.Delay(DelayForRequeue);
            await Add(model);
        }
    }
}
