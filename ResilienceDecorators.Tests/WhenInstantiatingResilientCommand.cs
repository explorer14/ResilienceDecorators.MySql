using MySql.Data.MySqlClient;
using ResilienceDecorators.MySql;
using System;
using Xunit;

namespace ResilienceDecorators.Tests
{
    public class WhenInstantiatingResilientCommand
    {
        [Fact]
        public void ShouldThrowIfUnderlyingMySqlCommandIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new ResilientMySqlCommand(
                    null,
                    ResilienceSettings.DefaultFailoverResilienceSettings);
            });
        }

        [Fact]
        public void ShouldThrowIfResilienceSettingsAreNotProvided()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new ResilientMySqlCommand(
                    new MySqlCommand(),
                    null);
            });
        }
    }
}