﻿using ResilienceDecorators.MySql.RetryPolicies;
using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ResilienceDecorators.MySql.RetryHelpers
{
    /// <summary>
    /// Inheriting from this class and invoking its methods, will allow your MySql database
    /// interactions to recover from failovers.
    /// </summary>
    public abstract class RetryWrapper
    {
        /// <summary>
        /// You must override this method to return the connection string in use by the caller,
        /// for connection management reasons.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetConnectionString();

        /// <summary>
        /// Synchronous retry policy that returns T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="customResilienceSettings"></param>
        /// <param name="onRetry"></param>
        /// <returns></returns>
        protected T ExecuteWithSyncRetries<T>(
            Func<T> action,
            ResilienceSettings customResilienceSettings = null,
            Action<MySqlException, TimeSpan> onRetry = null) =>

            MySqlFailoverRetryPolicies
                .DefaultSyncPolicy(
                    customResilienceSettings,
                    onRetry)
                .Execute(() =>
                {
                    try
                    {
                        return action();
                    }
                    catch (MySqlException ex)
                    {
                        ClearConnectionPoolIfDatabaseFailingOver(ex);

                        throw;
                    }
                });

        /// <summary>
        /// Synchronous retry policy of void return type
        /// </summary>
        /// <param name="action"></param>
        /// <param name="customResilienceSettings"></param>
        /// <param name="onRetry"></param>
        protected void ExecuteWithSyncRetries(
            Action action,
            ResilienceSettings customResilienceSettings = null,
            Action<MySqlException, TimeSpan> onRetry = null) =>

            MySqlFailoverRetryPolicies
                .DefaultSyncPolicy(
                    customResilienceSettings,
                    onRetry)
                .Execute(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (MySqlException ex)
                    {
                        ClearConnectionPoolIfDatabaseFailingOver(ex);

                        throw;
                    }
                });

        /// <summary>
        /// Asynchronous retry policy that returns Task of <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="customResilienceSettings"></param>
        /// <param name="onRetry"></param>
        /// <param name="connv"></param>
        /// <returns></returns>
        protected async Task<T> ExecuteWithAsyncRetries<T>(Func<Task<T>> action,
            ResilienceSettings customResilienceSettings = null,
            Action<MySqlException, TimeSpan> onRetry = null) =>

            await MySqlFailoverRetryPolicies
                .DefaultAsyncPolicy(
                    customResilienceSettings,
                    onRetry)
                .ExecuteAsync(async () =>
                {
                    try
                    {
                        return await action();
                    }
                    catch (MySqlException ex)
                    {
                        ClearConnectionPoolIfDatabaseFailingOver(ex);

                        throw;
                    }
                });

        /// <summary>
        /// Asynchronous retry policy of Task return type
        /// </summary>
        /// <param name="action"></param>
        /// <param name="customResilienceSettings"></param>
        /// <param name="onRetry"></param>
        /// <returns></returns>
        protected async Task ExecuteWithAsyncRetries(
            Func<Task> action,
            ResilienceSettings customResilienceSettings = null,
            Action<MySqlException, TimeSpan> onRetry = null) =>

            await MySqlFailoverRetryPolicies
                .DefaultAsyncPolicy(
                    customResilienceSettings,
                    onRetry)
                .ExecuteAsync(async () =>
                {
                    try
                    {
                        await action();
                    }
                    catch (MySqlException ex)
                    {
                        ClearConnectionPoolIfDatabaseFailingOver(ex);

                        throw;
                    }
                });

        private void ClearConnectionPoolIfDatabaseFailingOver(MySqlException ex)
        {
            if (ex.IsFailoverException())
                MySqlConnection.ClearPool(new MySqlConnection(GetConnectionString()));
        }
    }
}