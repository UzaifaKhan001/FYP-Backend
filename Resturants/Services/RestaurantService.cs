using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Restaurants.Models;

namespace Restaurants.Services
{
    public class RestaurantService
    {
        private readonly string _connectionString;

        public RestaurantService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Get Business Types (Using ADO.NET)
        public List<BusinessType> GetBusinessTypes()
        {
            var businessTypes = new List<BusinessType>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT Id, Name FROM BusinessTypes", conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            businessTypes.Add(new BusinessType
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return businessTypes;
        }

        // ✅ Add Business Type (Using ADO.NET)
        public bool AddBusinessType(BusinessType businessType)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO BusinessTypes (Name) VALUES (@Name)", conn))
                {
                    cmd.Parameters.AddWithValue("@Name", businessType.Name);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        // Validate Address (Mock Example)
        public bool ValidateAddress(string address)
        {
            return !string.IsNullOrWhiteSpace(address);
        }

        // Add Restaurant (Using ADO.NET)
        public bool AddRestaurant(Restaurant restaurant)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO Restaurants (StoreAddress, FloorSuite, StoreName, BrandName, BusinessTypeId, FirstName, LastName, Email, PhoneNumber, AgreedToPrivacy) 
                    VALUES (@StoreAddress, @FloorSuite, @StoreName, @BrandName, @BusinessTypeId, @FirstName, @LastName, @Email, @PhoneNumber, @AgreedToPrivacy)", conn))
                {
                    cmd.Parameters.AddWithValue("@StoreAddress", restaurant.StoreAddress);
                    cmd.Parameters.AddWithValue("@FloorSuite", (object)restaurant.FloorSuite ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@StoreName", restaurant.StoreName);
                    cmd.Parameters.AddWithValue("@BrandName", restaurant.BrandName);
                    cmd.Parameters.AddWithValue("@BusinessTypeId", restaurant.BusinessTypeId);
                    cmd.Parameters.AddWithValue("@FirstName", restaurant.FirstName);
                    cmd.Parameters.AddWithValue("@LastName", restaurant.LastName);
                    cmd.Parameters.AddWithValue("@Email", restaurant.Email);
                    cmd.Parameters.AddWithValue("@PhoneNumber", restaurant.PhoneNumber);
                    cmd.Parameters.AddWithValue("@AgreedToPrivacy", restaurant.AgreedToPrivacy);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
    }
}
