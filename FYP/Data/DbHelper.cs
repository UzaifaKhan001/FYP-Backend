using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FYP.Data
{
    public class DbHelper
    {
        private readonly string _connectionString;
        private readonly ILogger<DbHelper> _logger;

        public DbHelper(IConfiguration configuration, ILogger<DbHelper> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("Connection string is missing.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SqlConnection GetConnection()
        {
            try
            {
                var connection = new SqlConnection(_connectionString);
                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating database connection.");
                throw;
            }
        }
    }
}
