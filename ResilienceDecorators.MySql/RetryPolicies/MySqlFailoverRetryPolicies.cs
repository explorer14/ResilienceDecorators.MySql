using Polly;
using Polly.Retry;
using System;
using MySql.Data.MySqlClient;

namespace ResilienceDecorators.MySql.RetryPolicies
{
    /// <summary>
    /// Pair of default retry policies - sync and async
    /// </summary>
    public class MySqlFailoverRetryPolicies
    {
        /// <summary>
        /// Default sync policy
        /// </summary>
        /// <param name="resilienceSettings"></param>
        /// <param name="onRetry"></param>
        /// <returns></returns>
        public static RetryPolicy DefaultSyncPolicy(
                ResilienceSettings resilienceSettings = null,
                Action<MySqlException, TimeSpan> onRetry = null)
        {
            resilienceSettings = resilienceSettings ??
                ResilienceSettings.DefaultFailoverResilienceSettings;

            return Policy
                .Handle<MySqlException>(x => x.IsFailoverException())
                .WaitAndRetry(resilienceSettings.RetryCount,
                    retry => TimeSpan.FromSeconds(
                        retry * resilienceSettings.RetryIntervalFactor),
                    (failure, nextRetryIn) =>
                        onRetry?.Invoke(failure as MySqlException, nextRetryIn));
        }

        /// <summary>
        /// Default async policy
        /// </summary>
        /// <param name="resilienceSettings"></param>
        /// <param name="onRetry"></param>
        /// <returns></returns>
        public static AsyncRetryPolicy DefaultAsyncPolicy(
            ResilienceSettings resilienceSettings = null,
            Action<MySqlException, TimeSpan> onRetry = null)
        {
            resilienceSettings = resilienceSettings ??
                ResilienceSettings.DefaultFailoverResilienceSettings;

            return Policy
                .Handle<MySqlException>(x => x.IsFailoverException())
                .WaitAndRetryAsync(resilienceSettings.RetryCount,
                    retry => 
                        TimeSpan.FromSeconds(retry * resilienceSettings.RetryIntervalFactor),
                    (failure, nextRetryIn) =>
                        onRetry?.Invoke(failure as MySqlException, nextRetryIn));
        }
    }
}