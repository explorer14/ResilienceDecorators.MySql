using Dapper;
using Dapper.Contrib.Extensions;
using ResilienceDecorators.MySql;
using ResilienceDecorators.MySql.RetryHelpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ConsoleApp17
{
    internal class Program
    {
        private static string ConnectionString = "Server=127.0.0.1;Port=3306;Database=Aman;Uid=root;Ssl Mode=Required;Pwd=pass123;Pooling=True;MinimumPoolSize=10;ConnectionLifetime=60";

        private static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            //DoWithoutResilience();

            await DoWithResilience();
        }

        private static void DoWithoutResilience()
        {
            for (var i = 0; i < 10_000; i++)
            {
                try
                {
                    using (var conn = new MySqlConnection(ConnectionString))
                    {
                        conn.Open();
                        conn.Insert(new Record { Id = i + 1, Name = $"Aman {i + 1}" });
                        Log.Logger.Information("Inserted 1 row successfully!");
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Error");
                }

                Thread.Sleep(100);
            }
        }

        private static async Task DoWithResilience()
        {
            var facade = new TableFacade(ConnectionString);

            for (int x = 0; x < 1_000; x++)
            {
                var data = await facade.GetIt();
                await Task.Delay(500);
            }
        }

        private static void LogRetry(MySqlException mySqlException, TimeSpan time) =>
            Log.Logger.Warning("Failed with {message}. Retrying in {@ts}...",
                               mySqlException.Message,
                               time);
    }

    [Table("MyTable")]
    public class Record
    {
        [ExplicitKey]
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class TableFacade : RetryWrapper
    {
        private readonly string connectionString;

        public TableFacade(string connectionString)
        {
            this.connectionString = connectionString;
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        }

        protected override string GetConnectionString() => connectionString;

        public async Task DoIt()
        {
            for (int x = 0; x < 100; x++)
            {
                await ExecuteWithAsyncRetries(async () =>
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        conn.Insert(new Record { Id = x + 1, Name = $"Aman {x + 1}" });
                        Log.Logger.Information("Inserted 1 row successfully!");
                    }
                },
                ResilienceSettings.DefaultFailoverResilienceSettings,
                LogRetry);

                await Task.Delay(500);
            }
        }

        public async Task<IReadOnlyCollection<Record>> GetIt()
        {
            return await ExecuteWithAsyncRetries<IReadOnlyCollection<Record>>(async () =>
            {
                using (var connv = new MySqlConnection(connectionString))
                {
                    await connv.OpenAsync();
                    var result = await connv.QueryAsync<Record>("SELECT * FROM Aman.MyTable");

                    Log.Logger.Information("Retrieved!");

                    return result.ToList();
                }
            },
            ResilienceSettings.DefaultFailoverResilienceSettings,
            LogRetry);
        }

        private static void LogRetry(MySqlException mySqlException, TimeSpan time) =>
            Log.Logger.Warning("Failed with {message}. Retrying in {@ts}...",
                               mySqlException.Message,
                               time);
    }
}