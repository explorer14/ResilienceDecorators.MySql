using Dapper.Contrib.Extensions;
using MySql.Data.MySqlClient;
using ResilienceDecorators.MySql;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp17
{
    internal class Program
    {
        private static string ConnectionString = "Server=127.0.0.1;Port=3306;Database=Aman;Uid=root;Ssl Mode=Required;Pwd=pass123;Pooling=True;ConnectionLifetime=60";

        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            //DoWithoutResilience();

            //DoWithResilience().Wait();

            DoWithTransactions();
        }

        private static void DoWithTransactions()
        {
            for (int i = 0; i < 10_1000; i++)
            {
                using (var conn = new ResilientMySqlConnectionBuilder()
                    .ForConnectionString(ConnectionString)
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
                            Log.Logger.Information("Inserted {count} row successfully!", rows);
                        }
                        catch (Exception)
                        {
                            tran.Rollback();
                        }
                    }
                }

                Thread.Sleep(100);
            }
        }

        private static async Task DoWithResilience()
        {
            for (var i = 0; i < 10_000; i++)
            {
                using (var conn = new ResilientMySqlConnectionBuilder()
                    .ForConnectionString(ConnectionString)
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

    [Table("AmanTable")]
    public class Record
    {
        [ExplicitKey]
        public int Id { get; set; }

        public string Name { get; set; }
    }
}