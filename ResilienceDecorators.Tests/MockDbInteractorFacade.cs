using MySql.Data.MySqlClient;
using ResilienceDecorators.MySql;
using ResilienceDecorators.MySql.RetryHelpers;
using System;
using System.Threading.Tasks;

namespace ResilienceDecorators.Tests
{
    internal class MockDbInteractorFacade : RetryWrapper
    {
        public int Retries { get; private set; }

        private readonly ResilienceSettings resilienceSettings;
        private readonly Action<MySqlException, TimeSpan> onRetry;

        public MockDbInteractorFacade(
            ResilienceSettings resilienceSettings,
            Action<MySqlException, TimeSpan> onRetry)
        {
            this.resilienceSettings = resilienceSettings;
            this.onRetry = onRetry;
        }

        public async Task DoWriteAsync()
        {
            await ExecuteWithAsyncRetries(async () =>
            {
                if (Retries >= resilienceSettings.RetryCount)
                    return;

                await Task.Delay(1);
                ++Retries;

                ThrowMySqlException();
            },
            customResilienceSettings: resilienceSettings,
            onRetry: onRetry);
        }

        public void DoWriteSync()
        {
            ExecuteWithSyncRetries(() =>
            {
                if (Retries >= resilienceSettings.RetryCount)
                    return;

                ++Retries;

                ThrowMySqlException();
            },
            customResilienceSettings: resilienceSettings,
            onRetry: onRetry);
        }

        public Record GetOneSync()
        {
            return ExecuteWithSyncRetries(() =>
            {
                if (Retries >= resilienceSettings.RetryCount)
                    return Record.SampleRecord;

                ++Retries;

                ThrowMySqlException();

                return Record.SampleRecord;
            },
            customResilienceSettings: resilienceSettings,
            onRetry: onRetry);
        }

        public async Task<Record> GetOneAsync()
        {
            return await ExecuteWithAsyncRetries(async () =>
            {
                if (Retries >= resilienceSettings.RetryCount)
                    return Record.SampleRecord;

                ++Retries;

                await Task.Delay(1);

                ThrowMySqlException();

                return Record.SampleRecord;
            },
            customResilienceSettings: resilienceSettings,
            onRetry: onRetry);
        }

        public Record[] GetMultipleSync()
        {
            return ExecuteWithSyncRetries(() =>
            {
                if (Retries >= resilienceSettings.RetryCount)
                    return new[] { Record.SampleRecord };

                ++Retries;

                ThrowMySqlException();

                return new[] { Record.SampleRecord };
            },
            customResilienceSettings: resilienceSettings,
            onRetry: onRetry);
        }

        public async Task<Record[]> GetMultipleAsync()
        {
            return await ExecuteWithAsyncRetries(async () =>
            {
                if (Retries >= resilienceSettings.RetryCount)
                    return new[] { Record.SampleRecord };

                ++Retries;

                await Task.Delay(1);

                ThrowMySqlException();

                return new[] { Record.SampleRecord };
            },
            customResilienceSettings: resilienceSettings,
            onRetry: onRetry);
        }

        protected override string GetConnectionString() =>
            string.Empty;

        public void ThrowMySqlException() =>
            new MySqlConnection("Server=255.255.255.255;Database=NODB;ConnectionTimeout=1")
                .Open();
    }
}