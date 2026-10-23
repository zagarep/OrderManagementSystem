
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagementAPI.Data;
using OrderManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace OrderManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }



        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int? categoryId,
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1)
            {
                return BadRequest(new
                {
                    message = "Page number must be at least 1."
                });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new
                {
                    message = "Page size must be between 1 and 100."
                });
            }

            if (minPrice.HasValue && minPrice.Value < 0 ||
                maxPrice.HasValue && maxPrice.Value < 0)
            {
                return BadRequest(new
                {
                    message = "Prices cannot be negative."
                });
            }

            if (minPrice.HasValue && maxPrice.HasValue &&
                minPrice.Value > maxPrice.Value)
            {
                return BadRequest(new
                {
                    message = "Minimum price cannot exceed maximum price."
                });
            }

            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                if (categoryId.Value <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Category ID must be greater than zero."
                    });
                }

                query = query.Where(
                    p => p.CategoryId == categoryId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(
                    p => p.IsActive == isActive.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(
                    p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(
                    p => p.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search.Trim()));
            }
            var totalRecords = await query.CountAsync();

            var products = await query
                .OrderBy(p => p.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                pageNumber,
                pageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(
                    totalRecords / (double)pageSize),
                data = products
            });
        }


        // GET: api/Products/1
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            return Ok(product);
        }

        // POST: api/Products
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == product.CategoryId && c.IsActive);

            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = "The selected category does not exist or is inactive."
                });
            }

            if (product.Price <= 0)
            {
                return BadRequest(new
                {
                    message = "Price must be greater than zero."
                });
            }

            if (product.StockQuantity < 0)
            {
                return BadRequest(new
                {
                    message = "Stock quantity cannot be negative."
                });
            }

            product.Id = 0;
            product.CreatedOn = DateTime.UtcNow;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetProduct),
                new { id = product.Id },
                product);
        }

        // PUT: api/Products/1
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(
            int id,
            Product updatedProduct)
        {
            if (id != updatedProduct.Id)
            {
                return BadRequest(new
                {
                    message = "Product ID in the URL does not match the request body."
                });
            }

            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == updatedProduct.CategoryId && c.IsActive);

            if (!categoryExists)
            {
                return BadRequest(new { message = "Invalid or inactive category." });
            }

            if (updatedProduct.Price <= 0 ||
                updatedProduct.StockQuantity < 0)
            {
                return BadRequest(new
                {
                    message = "Price must be positive and stock cannot be negative."
                });
            }

            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Price = updatedProduct.Price;
            product.StockQuantity = updatedProduct.StockQuantity;
            product.ImageUrl = updatedProduct.ImageUrl;
            product.CategoryId = updatedProduct.CategoryId;
            product.IsActive = updatedProduct.IsActive;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Products/1
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            product.IsActive = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
