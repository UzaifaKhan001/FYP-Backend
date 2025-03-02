using Microsoft.AspNetCore.Mvc;
using Restaurants.Services;
using Restaurants.Models;
using System;
using System.Collections.Generic;

namespace Restaurants.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestaurantController : ControllerBase
    {
        private readonly RestaurantService _restaurantService;

        public RestaurantController(RestaurantService restaurantService)
        {
            _restaurantService = restaurantService;
        }



        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Auth service is running." });
        }

        // GET: api/restaurant/business-types
        [HttpGet("business-types")]
        public IActionResult GetBusinessTypes()
        {
            try
            {
                var businessTypes = _restaurantService.GetBusinessTypes();
                return Ok(businessTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        // ✅ POST: api/restaurant/add-business-type
        [HttpPost("add-business-type")]
        public IActionResult AddBusinessType([FromBody] BusinessType businessType)
        {
            if (businessType == null || string.IsNullOrWhiteSpace(businessType.Name))
                return BadRequest("Business type name is required.");

            try
            {
                bool success = _restaurantService.AddBusinessType(businessType);
                if (success)
                    return Ok("Business type added successfully.");
                return StatusCode(500, "Failed to add business type.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // POST: api/restaurant/validate-address
        [HttpPost("validate-address")]
        public IActionResult ValidateAddress([FromBody] AddressValidationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StoreAddress))
                return BadRequest("Address is required.");

            bool isValid = _restaurantService.ValidateAddress(request.StoreAddress);
            return isValid ? Ok("Valid address.") : BadRequest("Invalid address.");
        }

        // POST: api/restaurant/add
        [HttpPost("add")]
        public IActionResult AddRestaurant([FromBody] Restaurant restaurant)
        {
            if (restaurant == null)
                return BadRequest(new { error = "Invalid restaurant data. Request body cannot be empty." });

            if (string.IsNullOrWhiteSpace(restaurant.StoreAddress))
                return BadRequest(new { error = "Store address is required." });

            if (string.IsNullOrWhiteSpace(restaurant.StoreName))
                return BadRequest(new { error = "Store name is required." });

            if (restaurant.BusinessTypeId <= 0)
                return BadRequest(new { error = "Valid BusinessTypeId is required." });

            try
            {
                bool success = _restaurantService.AddRestaurant(restaurant);
                if (success)
                    return Ok(new { message = "Restaurant added successfully." });

                return StatusCode(500, new { error = "Failed to add restaurant." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

        public class AddressValidationRequest
    {
        public string StoreAddress { get; set; }
    }
}
