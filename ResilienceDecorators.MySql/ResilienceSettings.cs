namespace ResilienceDecorators.MySql
{
    /// <summary>
    /// Resilience settings to use while creating resilience policies
    /// </summary>
    public sealed class ResilienceSettings
    {
        private const int DEFAULT_RETRY_COUNT = 5;
        private const int DEFAULT_RETRY_INTERVAL_FACTOR = 5;

        /// <summary>
        /// Uses the default value for retry count = 5 and interval factor = 5. These defaults will
        /// create retries at 5s, 10s, 15, 20s, 25s which should be enough time to recover from an
        /// instance reboot or failover
        /// </summary>
        public ResilienceSettings()
            : this(
                  DEFAULT_RETRY_COUNT,
                  DEFAULT_RETRY_INTERVAL_FACTOR)
        { }

        /// <summary>
        /// Override the defaults with custom values
        /// </summary>
        /// <param name="retryCount">How many times to retry upon initial failure?</param>
        /// <param name="retryIntervalFactor">
        /// What factor to multiply the intervals between retries by?
        /// </param>
        public ResilienceSettings(
            int retryCount,
            int retryIntervalFactor)
        {
            RetryCount = retryCount > 0 ?
                retryCount :
                DEFAULT_RETRY_COUNT;

            RetryIntervalFactor = retryIntervalFactor > 0 ?
                retryIntervalFactor :
                DEFAULT_RETRY_INTERVAL_FACTOR;
        }

        /// <summary>
        /// The number of retries to be executed after the initial failure.
        /// Default value is 5
        /// </summary>
        public int RetryCount { get; }

        /// <summary>
        /// The interval between retries (in seconds) = retryAttempt * RetryIntervalFactor.
        /// The higher the value of this property, the higher the delay between retries.
        /// Default value is 5 (i.e. retry with a delay increasing by 5 second)
        /// to prevent rapid retries which will likely fail
        /// and eliminate benefits of a retry policy
        /// </summary>
        public int RetryIntervalFactor { get; }

        /// <summary>
        /// Convenience property to call when using the default settings
        /// </summary>
        public static ResilienceSettings DefaultFailoverResilienceSettings =>
            new ResilienceSettings();
    }
}