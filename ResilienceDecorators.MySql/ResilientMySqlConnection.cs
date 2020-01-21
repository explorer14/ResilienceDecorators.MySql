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
    ///     A resilient decorator around the native MySqlConnection class. Uses Polly retry policy to
    ///     retry failed connection attempts during database failover/reboot errors. The retries are
    ///     configurable and will always default to 5 if not provided explicitly.
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
    /// </para>
    ///
    /// Calling the CreateDbCommand() on an instance of this decorator will return an instance of
    /// <see cref="ResilientMySqlCommand"/> that will inherit the <see cref="ResilienceSettings"/>
    /// and the <see cref="Action{MySqlException, TimeSpan}"/> action
    /// </summary>
    public class ResilientMySqlConnection : DbConnection
    {
        private readonly MySqlConnection innerConnection;
        private readonly ResilienceSettings resilienceSettings;
        private readonly Action<MySqlException, TimeSpan> onRetry;
        private RetryPolicy retryPolicy;

        /// <summary>
        /// Create an instance of <see cref="ResilientMySqlConnection"/> as a wrapper around the
        /// underlying <see cref="MySqlConnection"/> instance with custom resilience settings and
        /// something to do when a retry occurs
        /// </summary>
        /// <param name="innerConnection"></param>
        /// <param name="resilienceSettings"></param>
        /// <param name="onRetry"></param>
        public ResilientMySqlConnection(
            MySqlConnection innerConnection,
            ResilienceSettings resilienceSettings,
            Action<MySqlException, TimeSpan> onRetry = null)
        {
            this.innerConnection = innerConnection;
            this.resilienceSettings = resilienceSettings;
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

        public override string ConnectionString { get => innerConnection.ConnectionString; set => innerConnection.ConnectionString = value; }

        public override string Database => innerConnection.Database;

        public override string DataSource => innerConnection.DataSource;

        public override string ServerVersion => innerConnection.ServerVersion;

        public override ConnectionState State => innerConnection.State;

        public MySqlConnection InnerConnection => innerConnection;

        public override void ChangeDatabase(string databaseName) =>
            innerConnection.ChangeDatabase(databaseName);

        public override void Close() =>
            ExecuteResiliently(innerConnection.Close);

        public override void Open() =>
            ExecuteResiliently(innerConnection.Open);

        protected override DbTransaction BeginDbTransaction(
            IsolationLevel isolationLevel)
        {
            return retryPolicy.Execute(() =>
            {
                try
                {
                    EnsureConnectionIsOpen();
                    return innerConnection.BeginTransaction(
                            isolationLevel == IsolationLevel.Unspecified
                            ? IsolationLevel.RepeatableRead // default for MySql is RepeatableRead
                            : isolationLevel);
                }
                catch (MySqlException mySqlEx)
                {
                    if (mySqlEx.IsFailoverException())
                        ResetConnection();

                    throw mySqlEx;
                }
            });
        }

        protected override DbCommand CreateDbCommand() =>
            new ResilientMySqlCommand(
                innerConnection.CreateCommand(),
                resilienceSettings,
                onRetry);

        private void ExecuteResiliently(Action executable)
        {
            retryPolicy.Execute(() =>
            {
                try
                {
                    executable();
                }
                catch (MySqlException mySqlEx)
                {
                    if (mySqlEx.IsFailoverException())
                        ResetConnection();

                    throw mySqlEx;
                }
            });
        }

        private void ResetConnection()
        {
            innerConnection.Dispose();
            // for this call to do anything, the idle pool should have non-zero connections
            // which it will after the connection is disposed. This increases the likelihood
            // of retries succeeding.
            MySqlConnection.ClearPool(innerConnection);
        }

        private void EnsureConnectionIsOpen()
        {
            if (innerConnection.State != ConnectionState.Open)
                innerConnection.Open();
        }
    }
}