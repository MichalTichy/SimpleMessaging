using System;
using Polly;
using Polly.CircuitBreaker;
using Polly.Wrap;

namespace SimpleMessaging.RetryManager
{
    public static class RetryManager
    {
        public const int RetryCount = 5;
        public const int DurationOfBreakInSeconds = 60;


        public static AsyncPolicyWrap CircuitBreakerWithRetryAsync<T>(WaitStrategy waitStrategy,
            Action<Exception, TimeSpan> onRetry, Action<Exception, TimeSpan> onBreak, Action onReset, int retryCount = RetryCount, int durationOfBreakInSeconds = DurationOfBreakInSeconds) where T : Exception
        {

            var waitStrategyImplementation = GetWaitStrategyImplementation(waitStrategy);
            return CircuitBreakerWithRetryAsync<T>(waitStrategyImplementation, onRetry, retryCount,
                TimeSpan.FromSeconds(durationOfBreakInSeconds), onBreak, onReset);
        }
        public static PolicyWrap CircuitBreakerWithRetry<T>(WaitStrategy waitStrategy,
            Action<Exception, TimeSpan> onRetry, Action<Exception, TimeSpan> onBreak, Action onReset, int retryCount = RetryCount, int durationOfBreakInSeconds = DurationOfBreakInSeconds) where T : Exception
        {

            var waitStrategyImplementation = GetWaitStrategyImplementation(waitStrategy);
            return CircuitBreakerWithRetry<T>(waitStrategyImplementation, onRetry, retryCount,
                TimeSpan.FromSeconds(durationOfBreakInSeconds), onBreak, onReset);
        }

        public static Func<int, TimeSpan> GetWaitStrategyImplementation(WaitStrategy waitStrategy)
        {
            switch (waitStrategy)
            {
                case WaitStrategy.MinimalWait:
                    return i => TimeSpan.FromMilliseconds(10);
                case WaitStrategy.ShortWait:
                    return i => TimeSpan.FromSeconds(5);
                case WaitStrategy.LongWait:
                    return i => TimeSpan.FromSeconds(10);
                case WaitStrategy.LinearWait:
                    return i => TimeSpan.FromSeconds(5 * i);
                case WaitStrategy.LinearWaitLong:
                    return i => TimeSpan.FromSeconds(10 * i);
                case WaitStrategy.CappedLinearWait:
                    return i =>
                    {
                        var wait = TimeSpan.FromSeconds(5 * i);
                        return wait > TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : wait;
                    };
                case WaitStrategy.CappedLinearWaitLong:
                    return i =>
                    {
                        var wait = TimeSpan.FromSeconds(10 * i);
                        return wait > TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : wait;
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(waitStrategy), waitStrategy, null);
            }
        }

        public static Policy Retry<TException>(int retryCount = RetryCount, WaitStrategy waitStrategy = WaitStrategy.LinearWait, Action<Exception, TimeSpan, int, Context> onRetry = null) where TException : Exception
        {
            return Policy.Handle<TException>().WaitAndRetry(retryCount, GetWaitStrategyImplementation(waitStrategy), onRetry ?? ((exception, span, arg3, arg4) => { }));
        }
        public static AsyncPolicy RetryAsync<TException>(int retryCount = RetryCount, WaitStrategy waitStrategy = WaitStrategy.LinearWait, Action<Exception, TimeSpan, int, Context> onRetry = null) where TException : Exception
        {
            return Policy.Handle<TException>().WaitAndRetryAsync(retryCount, GetWaitStrategyImplementation(waitStrategy), onRetry ?? ((exception, span, arg3, arg4) => { }));
        }

        private static AsyncPolicyWrap CircuitBreakerWithRetryAsync<T>(Func<int, TimeSpan> sleepDuration,
            Action<Exception, TimeSpan> onRetry, int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak,
            Action<Exception, TimeSpan> onBreak, Action onReset) where T : Exception
        {
            var policy = Policy.Handle<BrokenCircuitException>().WaitAndRetryForeverAsync(i => TimeSpan.FromSeconds(1));

            var retry = Policy.Handle<T>(e => !(e is BrokenCircuitException)).WaitAndRetryForeverAsync(sleepDuration, onRetry);
            var circuitBreaker = Policy.Handle<T>().CircuitBreakerAsync(exceptionsAllowedBeforeBreaking, durationOfBreak, onBreak, onReset);
            return Policy.WrapAsync(policy, retry, circuitBreaker);

        }
        private static PolicyWrap CircuitBreakerWithRetry<T>(Func<int, TimeSpan> sleepDuration,
            Action<Exception, TimeSpan> onRetry, int exceptionsAllowedBeforeBreaking, TimeSpan durationOfBreak,
            Action<Exception, TimeSpan> onBreak, Action onReset) where T : Exception
        {

            var policy = Policy.Handle<BrokenCircuitException>().WaitAndRetryForever(i => TimeSpan.FromSeconds(1));
            var retry = Policy.Handle<T>(e => !(e is BrokenCircuitException)).WaitAndRetryForever(sleepDuration, onRetry);
            var circuitBreaker = Policy.Handle<T>().CircuitBreaker(exceptionsAllowedBeforeBreaking, durationOfBreak, onBreak, onReset);

            return Policy.Wrap(policy, retry, circuitBreaker);
        }


    }
}
