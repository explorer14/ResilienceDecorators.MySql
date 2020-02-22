using MySql.Data.MySqlClient;
using System;

namespace ResilienceDecorators.MySql
{
    [Obsolete("Please use RetryWrapper, these decorator classes will eventually be removed!")]
    public class ResilientMySqlCommandBuilder
    {
        private string commandText;
        private MySqlConnection connection;
        private ResilienceSettings resilienceSettings;
        private Action<MySqlException, TimeSpan> onRetry;

        /// <summary>
        /// For both SELECT and INSERT/UPDATE/DELETE commands
        /// </summary>
        /// <param name="commandText">The SQL statement you want to execute</param>
        /// <returns><see cref="ResilientMySqlCommandBuilder"/></returns>
        public ResilientMySqlCommandBuilder ForCommand(
            string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentNullException(
                    nameof(commandText),
                    "Command text cannot be null or empty!");

            this.commandText = commandText;

            return this;
        }

        public ResilientMySqlCommandBuilder WithConnection(
            MySqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(
                    nameof(connection),
                    "Connection cannot be null!");

            this.connection = connection;

            return this;
        }

        /// <summary>
        /// Resilience settings are optional but not avoidable
        /// (kinda the point of this exercise). If null, default
        /// resilience settings will be used.
        /// </summary>
        /// <param name="resilienceSettings"><see cref="ResilienceSettings"/></param>
        /// <returns><see cref="ResilientMySqlCommandBuilder"/></returns>
        public ResilientMySqlCommandBuilder WithResilienceSettings(
            ResilienceSettings resilienceSettings)
        {
            this.resilienceSettings = resilienceSettings;

            return this;
        }

        /// <summary>
        /// If you want to log retries or do something else when a retry
        /// occurs, you can pass a custom retry Action which will be invoked.
        /// </summary>
        /// <param name="onRetry"><see cref="Action{MySqlException, TimeSpan}"/></param>
        /// <returns><see cref="ResilientMySqlCommandBuilder"/></returns>
        public ResilientMySqlCommandBuilder WithOnRetryAction(
            Action<MySqlException, TimeSpan> onRetry)
        {
            this.onRetry = onRetry;

            return this;
        }

        public ResilientMySqlCommand Build() =>
            new ResilientMySqlCommand(
                new MySqlCommand(
                    commandText,
                    connection),
                resilienceSettings ??
                ResilienceSettings.DefaultFailoverResilienceSettings,
                onRetry);
    }
}