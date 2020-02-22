using MySql.Data.MySqlClient;
using Polly;
using Polly.Retry;
using System;
using System.Data;
using System.Data.Common;

namespace ResilienceDecorators.MySql
{
    /// <summary>
    /// <para>
    ///     A resilient decorator around the native MySqlCommand class. Uses Polly retry policy to retry
    ///     failed commands in case of database failover errors. The retries are configurable and will
    ///     always default to 5 if not provided explicitly.
    /// </para>
    /// 
    /// <para>
    ///     The reason to write this decorator is that the native .NET driver doesn't seem to provide
    ///     any failover handling. What's there with the ReplicationManager etc is complicated and
    ///     requires to set up alternative hosts in the connection string.
    ///
    ///     This is not always possible for e.g. AWS Aurora only exposes a reader and a writer endpoint
    ///     and many times, a writer is all you need since it can do both reads and writes. It is then
    ///     upto the client application to handle failover scenarios themselves by attempting to reconnect.
    /// </para>
    /// 
    /// <para>
    ///     Resilience should be almost transparent for the consumer and not be
    ///     smeared throughout the whole code with Polly's often verbose syntax.
    ///     Hence this decorator!
    ///     
    ///     PLEASE NB: If you are using Dapper or other ORMs to talk to MySql this decorator will not
    ///     work since Dapper extensions are on MySqlConnection instance whereas this decorator is
    ///     around MySqlCommand instance. Also because I wanted to keep as much stuff native as possible.
    /// </para>
    /// </summary>
    [Obsolete("Please use RetryWrapper, these decorator classes will eventually be removed!")]
    public sealed class ResilientMySqlCommand : DbCommand
    {
        private readonly MySqlCommand innerCommand;
        private readonly ResilienceSettings resilienceSettings;
        private readonly Action<MySqlException, TimeSpan> onRetry;
        private RetryPolicy retryPolicy;

        /// <summary>
        /// Create an instance of <see cref="ResilientMySqlCommand"/> class
        /// </summary>
        /// <param name="innerCommand">The underlying <see cref="MySqlCommand"/></param>
        /// <param name="resilienceSettings">The <see cref="ResilienceSettings"/> to use.</param>
        /// <param name="onRetry">
        /// Custom <see cref="Action{MySqlException, TimeSpan}"/> to invoke when retries occur
        /// </param>
        public ResilientMySqlCommand(
            MySqlCommand innerCommand,
            ResilienceSettings resilienceSettings,
            Action<MySqlException, TimeSpan> onRetry = null)
        {
            this.innerCommand = innerCommand ??
                throw new ArgumentNullException(
                    nameof(innerCommand),
                    "The underlying MySqlCommand cannot be null or invalid!");
            
            this.resilienceSettings = resilienceSettings ??
                throw new ArgumentNullException(
                    nameof(resilienceSettings),
                    "Cannot create an instance of ResilientDbCommand without ResilienceSettings");

            this.onRetry = onRetry;
            BuildResiliencePolicy();
        }

        private void BuildResiliencePolicy()
        {
            retryPolicy = Policy
                .Handle<MySqlException>(x => x.IsFailoverException())
                .WaitAndRetry(resilienceSettings.RetryCount,
                    retry => TimeSpan.FromSeconds(retry * resilienceSettings.RetryIntervalFactor),
                    (a, b) => onRetry?.Invoke(a as MySqlException, b));
        }

        public override string CommandText { get => innerCommand.CommandText; set => innerCommand.CommandText = value; }
        public override int CommandTimeout { get => innerCommand.CommandTimeout; set => innerCommand.CommandTimeout = value; }
        public override CommandType CommandType { get => innerCommand.CommandType; set => innerCommand.CommandType = value; }
        public override bool DesignTimeVisible { get => innerCommand.DesignTimeVisible; set => innerCommand.DesignTimeVisible = value; }
        public override UpdateRowSource UpdatedRowSource { get => innerCommand.UpdatedRowSource; set => innerCommand.UpdatedRowSource = value; }
        protected override DbConnection DbConnection { get => innerCommand.Connection; set => innerCommand.Connection = (MySqlConnection)value; }

        protected override DbParameterCollection DbParameterCollection => innerCommand.Parameters;

        protected override DbTransaction DbTransaction { get => innerCommand.Transaction; set => innerCommand.Transaction = (MySqlTransaction)value; }

        public override void Cancel() =>
            innerCommand.Cancel();

        public override int ExecuteNonQuery() =>
            ExecuteResiliently(innerCommand.ExecuteNonQuery);

        public override object ExecuteScalar() =>
            ExecuteResiliently(innerCommand.ExecuteScalar);

        public override void Prepare() =>
            innerCommand.Prepare();

        protected override DbParameter CreateDbParameter() =>
            innerCommand.CreateParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
            ExecuteResiliently(innerCommand.ExecuteReader, behavior);

        private TReturnType ExecuteResiliently<TReturnType>(
            Func<TReturnType> executable) =>
            ExecuteResiliently<MySqlCommand, TReturnType>(_ => executable(), null);

        private TReturnType ExecuteResiliently<TInput, TReturnType>(
            Func<TInput, TReturnType> executable,
            TInput input = default)
        {
            var result = retryPolicy.Execute(() =>
            {
                try
                {
                    EnsureConnectionIsOpen();
                    return executable(input);
                }
                catch (MySqlException mySqlEx)
                {
                    if (mySqlEx.IsFailoverException())
                        ResetConnection();

                    throw mySqlEx;
                }
            });

            return result;
        }

        private void ResetConnection()
        {
            Connection.Dispose();
            // for this call to do anything, the idle pool should have non-zero connections
            // which it will after the connection is disposed. This increases the likelihood
            // of retries succeeding.
            MySqlConnection.ClearPool(Connection as MySqlConnection);
        }

        private void EnsureConnectionIsOpen()
        {
            if (Connection.State != ConnectionState.Open)
                Connection.Open();
        }
    }
}