# Introduction 
This package adds simple decorators around the official MySql.Data package to achieve resilience and fault tolerance (against transient faults like failovers and reboots) using Polly retries. This is NOT a replacement for the official package and you are free to use either.

These decorators ARE NOT guaranteed to work with ORMs so please exercise caution if you are using ORMs in your projects. Also, only retries are supported at the minute which means if the database doesn't recover for the duration of retries, the original exception will be propagated up to the application and if not handled gracefully, will stop the application.

# Getting Started

There are following main classes in this package:

- `ResilientMySqlConnection` (wraps the `MySqlConnection::Open` inside a Polly retry with default `ResilienceSettings`)
- `ResilientMySqlConnectionBuilder` (fluent API to create an instance of `ResilientMySqlConnection`)
- `ResilientMySqlCommand` (wraps the `MySqlCommand::Execute*` operations inside a Polly retry with default `ResilienceSettings`)
- `ResilientMySqlCommandBuilder` (fluent API to create an instance of `ResilientMySqlCommand`)

Then there are support classes:

- `ResilienceSettings` (stores setting for Polly retries)
- `MySqlExceptionExtensions` (contains extension methods on the official `MySqlException` class)

## Basic Usage

```
using (var conn = new ResilientMySqlConnectionBuilder()
                    .ForConnectionString(ConnectionString)                    
                    .Build())
{
    await conn.OpenAsync();

    var itemToInsert = new Record { Id = i + 1, Name = $"Aman {i + 1}" };
    var cmd = new ResilientMySqlCommandBuilder()
        .ForCommand($"INSERT INTO Aman.MyTable VALUES ({itemToInsert.Id}, '{itemToInsert.Name}')")
        .WithConnection(conn.InnerConnection)        
        .Build();

    var rows = await cmd.ExecuteNonQueryAsync();    
}
```
## Use the connection object to create a `DbCommand`

```
using (var conn = new ResilientMySqlConnectionBuilder()
                    .ForConnectionString(ConnectionString)                    
                    .Build())
{
    await conn.OpenAsync();
    var itemToInsert = new Record { Id = i + 1, Name = $"Aman {i + 1}" };

    *var cmd = (ResilientMySqlCommand)conn.CreateCommand();*
    *cmd.CommandText = $"INSERT INTO Aman.MyTable VALUES ({itemToInsert.Id}, '{itemToInsert.Name}')";*    

    var rows = await cmd.ExecuteNonQueryAsync();    
}
```

## Execute a custom action when a retry occurs (following an initial failure) for e.g. log the retry:

```
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
}

private static void LogRetry(MySqlException mySqlException, TimeSpan time) =>
    Log.Logger.Warning("Failed with {message}. Retrying in {@ts}...",
                       mySqlException.Message,
                       time);
```


# Running Locally

The code in the `ConsoleApp17` can be run against a Dockerised MySql database _(so no hosted database needed)_. Make sure you have Docker installed on your machine. Then,

1. On the command line, `cd` into the `docker` folder.

2. Run `docker-compose up`. This will pull down the official MySql 5.7 Docker image from Docker Hub, start the container and appy the initial set of migrations (in the `database` folder) to create a simple database schema.

3. Once the container is up and running, simply hit `F5` or do `dotnet run` on the `ConsoleApp17` to run the code and test the resilience features.

Simplest way to test the resilience bits is to just stop the container by issuing a `ctrl+c` command on the command line where the container is running. This will cause error `1042` i.e. `Unable to connec to host` and trigger spaced retries. By default it will try 5 times to reconnect, while its doing that, re-run `docker-compose up` to bring the database back "online". You will see the inserts resume where they left off.

*NB*: Its still upto you to ensure that retrying writes, doesn't corrupt the data. Ideally retries should only be applied if you know that you can make the underlying operations idempotent. Reads are a perfect candidate for retries.


# Contributions

If this package or code helps you, please send in a PR if you want to add enhancements or bug fixes to it!