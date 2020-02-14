using Dapper.Contrib.Extensions;
using Dapper;
using MySql.Data.MySqlClient;
using Polly;
using ResilienceDecorators.MySql;
using ResilienceDecorators.MySql.RetryHelpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace ConsoleApp17
{
    internal class Program
    {
        private static string ConnectionString = "Server=127.0.0.1;Port=3306;Database=Aman;Uid=root;Ssl Mode=Required;Pwd=pass123;Pooling=True;MinimumPoolSize=10;ConnectionLifetime=60";

        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            //DoWithoutResilience();

            //DoWithResilience().Wait();

            //DoWithTransactions();
            //DoWithRawResilience().Wait();

            //new TableFacade(ConnectionString).DoIt().Wait();
            var facade = new TableFacade(ConnectionString);            

            for (int x = 0; x < 1_000; x++)
            {
                var data = facade.GetIt().Result;
                Thread.Sleep(500);
            }

            //var adapter = new ResilienceAdapter(new DataAdapter(ConnectionString));
            //var data = adapter.GetAllRecords().Result;
            //Log.Logger.Information("{count} rows fetched!", data.Count);

            //Task.Delay(4000).Wait();

            //adapter.CreateRecord(2, "Blah").Wait();

            //Console.WriteLine("Record written to the database!");
        }

        private static void DoWithTransactions()
        {
            for (int i = 0; i < 10; i++)
            {
                using (var conn = new ResilientMySqlConnectionBuilder(ConnectionString)
                    .WithOnRetryAction(LogRetry)
                    .Build())
                {
                    conn.Open();

                    var itemToInsert = new Record { Id = i + 1, Name = $"Aman {i + 1}" };
                    var cmd = (ResilientMySqlCommand)conn.CreateCommand();
                    cmd.CommandText = $"INSERT INTO Aman.MyTable VALUES ({itemToInsert.Id}, '{itemToInsert.Name}')";

                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            var rows = cmd.ExecuteNonQuery();
                            tran.Commit();
                            Log.Logger.Information(
                                "Inserted {count} row successfully! Txn committed!", 
                                rows);
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            Log.Logger.Warning(ex, "Txn rolled back!");
                        }
                    }
                }

                Thread.Sleep(100);
            }
        }

        private static async Task DoWithRawResilience()
        {
            var retryPolicy = Policy
                .Handle<MySqlException>(x => x.IsFailoverException())
                .WaitAndRetryAsync(5,
                    retry => TimeSpan.FromSeconds(retry * 5),
                    (a, b) => LogRetry(a as MySqlException, b));

            for (var i = 0; i < 100; i++)
            {
                await retryPolicy.ExecuteAsync(async () =>
                {
                    using (var conn = new MySqlConnection(ConnectionString))
                    {
                        try
                        {
                            await conn.OpenAsync();

                            using (var tran = await conn.BeginTransactionAsync())
                            {
                                var itemToInsert = new Record { Id = i + 1, Name = $"Aman {i + 1}" };
                                var cmd = conn.CreateCommand();
                                cmd.CommandText = $"INSERT INTO Aman.MyTable VALUES ({itemToInsert.Id}, '{itemToInsert.Name}')";
                                try
                                {
                                    var rows = await cmd.ExecuteNonQueryAsync();
                                    tran.Commit();
                                }
                                catch (MySqlException mySqlEx)
                                {
                                    if (mySqlEx.IsFailoverException())
                                        MySqlConnection.ClearPool(conn);

                                    throw;
                                }
                                catch (Exception)
                                {
                                    tran.Rollback();
                                }

                                Log.Logger.Information("Inserted [1] row successfully!");
                            }
                        }
                        catch (MySqlException mySqlEx)
                        {
                            if (mySqlEx.IsFailoverException())
                                MySqlConnection.ClearPool(conn);

                            throw;
                        }
                    }
                });

                Thread.Sleep(100);
            }
        }

        private static async Task DoWithResilience()
        {
            for (var i = 0; i < 10_000; i++)
            {
                using (var conn = new ResilientMySqlConnectionBuilder(ConnectionString)
                    .WithOnRetryAction(LogRetry)
                    .Build())
                {
                    await conn.OpenAsync();

                    var itemToInsert = new Record { Id = i + 1, Name = $"Aman {i + 1}" };
                    var cmd = new ResilientMySqlCommandBuilder()
                        .ForCommand($"INSERT INTO Aman.MyTable VALUES ({itemToInsert.Id}, '{itemToInsert.Name}')")
                        .WithConnection(conn.InnerConnection)
                        .WithOnRetryAction(LogRetry)
                        .Build();

                    var rows = await cmd.ExecuteNonQueryAsync();

                    Log.Logger.Information("Inserted {count} row successfully!", rows);
                }

                Thread.Sleep(100);
            }
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
                using (var conn = new MySqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    var result = await conn.QueryAsync<Record>("SELECT * FROM Aman.MyTable");

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