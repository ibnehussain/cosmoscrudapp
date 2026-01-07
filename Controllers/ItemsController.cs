using Microsoft.AspNetCore.Mvc;
using CosmosApp.Services;
using CosmosApp.Models;
using CosmosApp.DTOs;

namespace CosmosApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(ICosmosDbService cosmosDbService, ILogger<ItemsController> logger)
        {
            _cosmosDbService = cosmosDbService;
            _logger = logger;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<ItemResponse>>> GetItemsByUser(string userId)
        {
            try
            {
                var items = await _cosmosDbService.GetItemsByUserAsync(userId);
                var response = items.Select(item => new ItemResponse
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    Name = item.Name,
                    Description = item.Description,
                    Category = item.Category,
                    CreatedAt = item.CreatedAt
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{userId}/{id}")]
        public async Task<ActionResult<ItemResponse>> GetItem(string userId, string id)
        {
            try
            {
                var item = await _cosmosDbService.GetItemAsync(id, userId);
                if (item == null)
                {
                    return NotFound();
                }

                var response = new ItemResponse
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    Name = item.Name,
                    Description = item.Description,
                    Category = item.Category,
                    CreatedAt = item.CreatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving item {ItemId} for user {UserId}", id, userId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ItemResponse>> CreateItem([FromBody] CreateItemRequest request)
        {
            try
            {
                var item = new Item
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    Name = request.Name,
                    Description = request.Description,
                    Category = request.Category,
                    CreatedAt = DateTime.UtcNow
                };

                var createdItem = await _cosmosDbService.CreateItemAsync(item);

                var response = new ItemResponse
                {
                    Id = createdItem.Id,
                    UserId = createdItem.UserId,
                    Name = createdItem.Name,
                    Description = createdItem.Description,
                    Category = createdItem.Category,
                    CreatedAt = createdItem.CreatedAt
                };

                return CreatedAtAction(nameof(GetItem), new { userId = createdItem.UserId, id = createdItem.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item for user {UserId}", request.UserId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{userId}/{id}")]
        public async Task<ActionResult<ItemResponse>> UpdateItem(string userId, string id, [FromBody] UpdateItemRequest request)
        {
            try
            {
                var existingItem = await _cosmosDbService.GetItemAsync(id, userId);
                if (existingItem == null)
                {
                    return NotFound();
                }

                existingItem.Name = request.Name;
                existingItem.Description = request.Description;
                existingItem.Category = request.Category;

                var updatedItem = await _cosmosDbService.UpdateItemAsync(existingItem);

                var response = new ItemResponse
                {
                    Id = updatedItem.Id,
                    UserId = updatedItem.UserId,
                    Name = updatedItem.Name,
                    Description = updatedItem.Description,
                    Category = updatedItem.Category,
                    CreatedAt = updatedItem.CreatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {ItemId} for user {UserId}", id, userId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{userId}/{id}")]
        public async Task<ActionResult> DeleteItem(string userId, string id)
        {
            try
            {
                await _cosmosDbService.DeleteItemAsync(id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {ItemId} for user {UserId}", id, userId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}