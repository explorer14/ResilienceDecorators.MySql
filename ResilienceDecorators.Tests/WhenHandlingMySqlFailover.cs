using FluentAssertions;
using MySql.Data.MySqlClient;
using ResilienceDecorators.MySql;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ResilienceDecorators.Tests
{
    public class WhenHandlingMySqlFailover
    {
        private readonly ITestOutputHelper testOut;

        public WhenHandlingMySqlFailover(ITestOutputHelper testOut)
        {
            this.testOut = testOut;
        }

        [Fact]
        public async Task ShouldWriteWithRetriesAsynclyForTheNumberOfTimesConfigured()
        {
            int retries = 3;
            var facade = new MockDbInteractorFacade(
                new ResilienceSettings(
                    retries, 1),
                Log);
            await facade.DoWriteAsync();

            facade.Retries.Should().Be(retries);
        }

        [Fact]
        public void ShouldWriteWithRetriesSynclyForTheNumberOfTimesConfigured()
        {
            int retries = 3;
            var facade = new MockDbInteractorFacade(
                new ResilienceSettings(
                    retries, 1),
                Log);
            facade.DoWriteSync();

            facade.Retries.Should().Be(retries);
        }

        [Fact]
        public void ShouldReadSingleWithRetriesSynclyForTheNumberOfTimesConfigured()
        {
            int retries = 3;
            var facade = new MockDbInteractorFacade(
                new ResilienceSettings(
                    retries, 1),
                Log);
            var item = facade.GetOneSync();

            facade.Retries.Should().Be(retries);
            item.Should().NotBeNull();
        }

        [Fact]
        public async Task ShouldReadSingleWithRetriesAsynclyForTheNumberOfTimesConfigured()
        {
            int retries = 3;
            var facade = new MockDbInteractorFacade(
                new ResilienceSettings(
                    retries, 1),
                Log);
            var item = await facade.GetOneAsync();

            facade.Retries.Should().Be(retries);
            item.Should().NotBeNull();
        }

        [Fact]
        public void ShouldReadMultipleWithRetriesSynclyForTheNumberOfTimesConfigured()
        {
            int retries = 3;
            var facade = new MockDbInteractorFacade(
                new ResilienceSettings(
                    retries, 1),
                Log);
            var items = facade.GetMultipleSync();

            facade.Retries.Should().Be(retries);
            items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ShouldReadMultipleWithRetriesAsynclyForTheNumberOfTimesConfigured()
        {
            int retries = 3;
            var facade = new MockDbInteractorFacade(
                new ResilienceSettings(
                    retries, 1),
                Log);
            var items = await facade.GetMultipleAsync();

            facade.Retries.Should().Be(retries);
            items.Should().NotBeEmpty();
        }

        private void Log(MySqlException ex, TimeSpan nextRetryIn)
        {
            testOut.WriteLine(
                $"Failure {ex.Message}, retrying in {nextRetryIn.ToString()}...");
        }
    }
}