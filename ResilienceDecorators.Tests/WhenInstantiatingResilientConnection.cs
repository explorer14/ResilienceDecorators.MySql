using MySql.Data.MySqlClient;
using ResilienceDecorators.MySql;
using System;
using Xunit;

namespace ResilienceDecorators.Tests
{
    public class WhenInstantiatingResilientConnection
    {
        [Theory]
        [InlineData(" ")]
        [InlineData(null)]
        public void ShouldThrowIfNoConnectionStringProvided(string connectionStringValue)
        {
            Assert.Throws<ArgumentNullException>(() => 
            {
                using(var conn = new ResilientMySqlConnectionBuilder(connectionStringValue).Build()){}
            });
        }

        [Fact]
        public void ShouldThrowIfUnderlyingMySqlConnectionIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>             
            {
                var resilientConnection = new ResilientMySqlConnection(
                    null, 
                    ResilienceSettings.DefaultFailoverResilienceSettings);
            });
        }

        [Fact]
        public void ShouldThrowIfNoResilienceSettingsAreProvided()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var resilientConnection = new ResilientMySqlConnection(
                    new MySqlConnection(),
                    null);
            });
        }
    }
}
