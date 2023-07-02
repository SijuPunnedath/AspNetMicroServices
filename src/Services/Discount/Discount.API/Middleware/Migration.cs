using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Runtime.CompilerServices;

namespace Discount.API.Middleware
{
    public class Migration : IMiddleware
    {
        private IConfiguration _configuration;
        private ILogger _logger;
        int retryForAvailability = 0;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            int? retry = 0;
            await MirateDatabase(retry);
            await next(context);

        }

        private async Task MirateDatabase(int?  retry=0)
        {
            int retryForAvailability = retry.Value;

              try
                {
                    _logger.LogInformation("Migrating postresql database.");

                    using var connection = new NpgsqlConnection
                        (_configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
                    connection.Open();

                    using var command = new NpgsqlCommand
                    {
                        Connection = connection
                    };

                    command.CommandText = "DROP TABLE IF EXISTS Coupon";
                    command.ExecuteNonQuery();

                    command.CommandText = @"CREATE TABLE Coupon(Id SERIAL PRIMARY KEY, 
                                                                ProductName VARCHAR(24) NOT NULL,
                                                                Description TEXT,
                                                                Amount INT)";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Coupon(ProductName, Description, Amount) VALUES('IPhone X', 'IPhone Discount', 150);";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO Coupon(ProductName, Description, Amount) VALUES('Samsung 10', 'Samsung Discount', 100);";
                    command.ExecuteNonQuery();

                    _logger.LogInformation("Migrated postresql database.");
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "An error occurred while migrating the postresql database");

                    if (retryForAvailability < 50)
                    {
                        retryForAvailability++;
                        System.Threading.Thread.Sleep(2000);
                       await MirateDatabase(retryForAvailability);
                    }
                }
           
        }

        
    }
}
