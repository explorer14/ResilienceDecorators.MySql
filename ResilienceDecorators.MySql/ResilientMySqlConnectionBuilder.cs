using MySql.Data.MySqlClient;
using System;

namespace ResilienceDecorators.MySql
{
    /// <summary>
    /// Fluent API to create an instance of <see cref="ResilientMySqlConnection"/> class
    /// </summary>
    public class ResilientMySqlConnectionBuilder
    {
        private string connectionString;
        private ResilienceSettings resilienceSettings;
        private Action<MySqlException, TimeSpan> onRetry;

        /// <summary>
        /// Create a new instance of the <see cref="ResilientMySqlConnectionBuilder"/> class
        /// </summary>
        /// <param name="connectionString">MySql connection string to use</param>
        public ResilientMySqlConnectionBuilder(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(
                    nameof(connectionString), 
                    "Cannot instantiate a resilient MySql connection without a valid connection string");

            this.connectionString = connectionString;
        }

        [Obsolete("This method is deprecated. Pass the connection string via the constructor instead")]
        public ResilientMySqlConnectionBuilder ForConnectionString(
            string connectionString)
        {
            this.connectionString = connectionString;

            return this;
        }

        /// <summary>
        /// Set up custom resilience settings. Calling this method in the builder chain is OPTIONAL
        /// because a <see cref="ResilienceSettings "/> instance with default values will be
        /// created. In other words, resilient out of the box.
        /// </summary>
        /// <param name="resilienceSettings"></param>
        /// <returns></returns>
        public ResilientMySqlConnectionBuilder WithResilienceSettings(
            ResilienceSettings resilienceSettings)
        {
            this.resilienceSettings = resilienceSettings;

            return this;
        }

        /// <summary>
        /// Set up what to do when a retry occurs. Calling this method is OPTIONAL unless, you want
        /// to take some action when a retry occurs for e.g. log the fact that it did occur.
        /// </summary>
        /// <param name="onRetry"></param>
        /// <returns></returns>
        public ResilientMySqlConnectionBuilder WithOnRetryAction(
            Action<MySqlException, TimeSpan> onRetry)
        {
            this.onRetry = onRetry;

            return this;
        }

        public ResilientMySqlConnection Build() =>
            new ResilientMySqlConnection(
                new MySqlConnection(connectionString),
                    resilienceSettings ??
                ResilienceSettings.DefaultFailoverResilienceSettings,
                    onRetry);
    }
}