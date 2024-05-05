[![](https://img.shields.io/badge/Nuget-v4.0.0-green?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/ResilienceDecorators.MySql/)

# What's new in v4.0.0

Core library updated to support .NET 6.0 and dependencies upgraded. 

# What's new in v3.0.0

All the **ResilientXXX** classes have been removed, this will be a breaking change for clients that are still using v2.x. Please switch over to v3.0!

# What's new in v2.2.0

No new features, really! Except I've chosen to **deprecate all the ResilientXXX classes** because the `RetryWrapper` approach is more reliable (covers all database interactions) and simpler to use. The next major version v3.0.0 will NOT have these classes, if you are using these, please switch over to using `RetryWrapper`. 

# Introduction 

A package for C#/.NET Core applications to provide failover recovery for MySql database interactions. Uses the [MySql.Data official .NET Core driver](https://www.nuget.org/packages/MySql.Data/) and [Polly](https://github.com/App-vNext/Polly) retry policies internally.

[Accompanying post](https://amanagrawal.blog/2020/01/19/recovering-from-aurora-database-failovers-and-mysql-connection-pooling/) that goes into details of failovers and connection management in the .NET Core MySql driver.

# Getting Started

## Inherit from the RetryWrapper class to execute your data access code resiliently

Say you have a repository interface in your project like so:

```
public interface IOrderRepository
{    
    Task<bool> CheckOrderExistsAsync(int orderId);
}
```

The implementation of this interface which provides these business oriented data access methods, can be made resilient by inheriting your repository class with the `RetryWrapper` class like so:

```
public class OrderRepository : RetryWrapper, IOrderRepository
{
    private readonly string connectionString;
    private readonly ResilienceSettings customResilienceSettings;

    public OrderRepository(ResilienceSettings customResilienceSettings)
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        connectionString = "Server=127.0.0.1;Port=3306;Database=OrdersDB;Uid=root;Ssl Mode=Required;Pwd=<>;Pooling=True;ConnectionLifetime=60";
        this.customResilienceSettings = customResilienceSettings;
    }

    public async Task<bool> CheckOrderExistsAsync(int orderId)
    {
        return await ExecuteWithAsyncRetries(async () =>
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM Orders WHERE OrderId = {orderId};";

                var reader = await command.ExecuteReaderAsync();
                Log.Logger.Information("Data was returned {val}", reader.HasRows);

                return reader.HasRows;
            }
        },
        customResilienceSettings,
        LogRetry);
    }

    private void LogRetry(MySqlException mySqlException, TimeSpan time) =>
        Log.Logger.Warning("Failed with {message}. Retrying in {@ts}...",
                           mySqlException.Message,
                           time);
}
```

The caller of these repository methods doesn't need to know that the calls will be retried:

```
// passing null will simply result in default failover resilience settings being applied
var store = new OrderRepository(null);

await store.CheckOrderExistsAsync(2);
```

## If you just want the underlying Polly resilience policies without inheriting from the helper class, you can do that too:

```
MySqlFailoverRetryPolicies
    .DefaultSyncPolicy(
        customResilienceSettings, // your own overrides for the resilience settings
        onRetry) // your own custom on retry action
    .Execute(action);
```

# Running Locally

The code in the [`ConsoleApp17`](ConsoleApp17/Program.cs) can be run against a Dockerised MySql database _(so no hosted database needed)_. Make sure you have Docker installed on your machine. Then,

1. On the command line, `cd` into the `docker` folder.

2. Run `docker-compose up`. This will pull down the official MySql 5.7 Docker image from Docker Hub, start the container and appy the initial set of migrations (in the `database` folder) to create a simple database schema.

3. Once the container is up and running, simply hit `F5` or do `dotnet run` on the `ConsoleApp17` to run the code and test the resilience features.

Simplest way to test the resilience bits is to just stop the container by issuing a `ctrl+c` command on the command line where the container is running. This will cause error `1042` i.e. `Unable to connec to host` and trigger spaced retries. By default it will try 5 times to reconnect, while its doing that, re-run `docker-compose up` to bring the database back "online". You will see the inserts resume where they left off.

*NB*: Its still upto you to ensure that retrying writes, doesn't corrupt the data. Ideally retries should only be applied if you know that you can make the underlying operations idempotent. Reads are a perfect candidate for retries.

# Contributions

If this package or code helps you, please send in a PR if you want to add enhancements or bug fixes to it!