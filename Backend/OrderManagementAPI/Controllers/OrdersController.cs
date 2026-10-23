
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagementAPI.Data;
using OrderManagementAPI.DTOs;
using OrderManagementAPI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace OrderManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ApplicationDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<ActionResult<OrderResponseDto>> CreateOrder(
            CreateOrderDto request)
        {
            var customerEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var customerName = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrWhiteSpace(customerEmail) ||
                string.IsNullOrWhiteSpace(customerName))
            {
                return Unauthorized(new
                {
                    message = "Customer information is missing from the token."
                });
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new
                {
                    message = "An order must contain at least one product."
                });
            }

            if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
            {
                return BadRequest(new
                {
                    message = "Delivery address is required."
                });
            }

            if (request.Items.Any(item =>
                item.ProductId <= 0 || item.Quantity <= 0))
            {
                return BadRequest(new
                {
                    message = "Product ID and quantity must be greater than zero."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var productIds = request.Items
                    .Select(item => item.ProductId)
                    .Distinct()
                    .ToList();

                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id) && p.IsActive)
                    .ToListAsync();

                if (products.Count != productIds.Count)
                {
                    return BadRequest(new
                    {
                        message = "One or more products do not exist or are inactive."
                    });
                }

                foreach (var item in request.Items)
                {
                    var product = products.First(
                        p => p.Id == item.ProductId);

                    if (product.StockQuantity < item.Quantity)
                    {
                        return BadRequest(new
                        {
                            message = $"Insufficient stock for {product.Name}."
                        });
                    }
                }

                var order = new Order
                {
                    OrderNumber = $"ORD-{Guid.NewGuid():N}",
                    CustomerName = customerName.Trim(),
                    CustomerEmail = customerEmail.Trim(),
                    DeliveryAddress = request.DeliveryAddress.Trim(),
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    TotalAmount = 0
                };

                foreach (var item in request.Items)
                {
                    var product = products.First(
                        p => p.Id == item.ProductId);

                    var subtotal = product.Price * item.Quantity;

                    order.TotalAmount += subtotal;

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        SubTotal = subtotal
                    });

                    product.StockQuantity -= item.Quantity;
                }
                
                _context.Orders.Add(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                   "Order {OrderId} with order number {OrderNumber} was created successfully.",
                    order.Id, order.OrderNumber); 
                var response = new OrderResponseDto
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    CustomerName = order.CustomerName,
                    CustomerEmail = order.CustomerEmail,
                    DeliveryAddress = order.DeliveryAddress,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status
                };

                return CreatedAtAction(
                    nameof(GetOrder),
                    new { id = order.Id },
                    response);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllOrders(
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

            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .ThenInclude(item => item.Product);

            var totalRecords = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = orders.Select(order => new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                DeliveryAddress = order.DeliveryAddress,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,

                Items = order.OrderItems.Select(item =>
                    new OrderItemResponseDto
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Product?.Name ?? "Unknown product",
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        SubTotal = item.SubTotal
                    }).ToList()
            }).ToList();

            return Ok(new
            {
                pageNumber,
                pageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(
                    totalRecords / (double)pageSize),
                data = response
            });
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderResponseDto>> GetOrder(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(new
                {
                    message = "Order not found."
                });
            }

            var response = new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                DeliveryAddress = order.DeliveryAddress,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,

                Items = order.OrderItems.Select(item => new OrderItemResponseDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? "Unknown product",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = item.SubTotal
                }).ToList()
            };

            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(
            int id, UpdateOrderStatusDto request)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }

            var allowedStatuses = new[] { "Pending", "Confirmed", "Cancelled" };

            if (!allowedStatuses.Contains(request.Status))
            {
                return BadRequest(new
                {
                    message = "Invalid status. Use Pending, Confirmed, or Cancelled."
                });
            }

            if (order.Status == "Cancelled")
            {
                return BadRequest(new
                {
                    message = "A cancelled order cannot be updated."
                });
            }

            if (request.Status == "Cancelled")
            {
                return BadRequest(new
                {
                    message = "Use the cancel endpoint to cancel an order."
                });
            }

            order.Status = request.Status;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order status updated successfully.",
                order.Id,
                order.Status
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found." });
                }

                if (order.Status == "Cancelled")
                {
                    return BadRequest(new
                    {
                        message = "Order is already cancelled."
                    });
                }

                if (order.Status != "Pending")
                {
                    return BadRequest(new
                    {
                        message = "Only pending orders can be cancelled."
                    });
                }

                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }

                order.Status = "Cancelled";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderId} was cancelled successfully.",order.Id);
                return Ok(new
                {
                    message = "Order cancelled and product stock restored successfully.",
                    order.Id,
                    order.Status
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        //[HttpGet("customer/{email}")]
        //public async Task<ActionResult<IEnumerable<OrderResponseDto>>>GetOrdersByCustomerEmail(string email)
        //{
        //    if (string.IsNullOrWhiteSpace(email))
        //    {
        //        return BadRequest(new
        //        {
        //            message = "Customer email is required."
        //        });
        //    }

        //    var orders = await _context.Orders
        //        .AsNoTracking()
        //        .Where(o => o.CustomerEmail == email.Trim())
        //        .Include(o => o.OrderItems)
        //        .ThenInclude(item => item.Product)
        //        .OrderByDescending(o => o.OrderDate)
        //        .ToListAsync();

        //    var response = orders.Select(order => new OrderResponseDto
        //    {
        //        Id = order.Id,
        //        OrderNumber = order.OrderNumber,
        //        CustomerName = order.CustomerName,
        //        CustomerEmail = order.CustomerEmail,
        //        DeliveryAddress = order.DeliveryAddress,
        //        OrderDate = order.OrderDate,
        //        TotalAmount = order.TotalAmount,
        //        Status = order.Status,

        //        Items = order.OrderItems.Select(item =>
        //            new OrderItemResponseDto
        //            {
        //                ProductId = item.ProductId,
        //                ProductName = item.Product?.Name ?? "Unknown product",
        //                Quantity = item.Quantity,
        //                UnitPrice = item.UnitPrice,
        //                SubTotal = item.SubTotal
        //            }).ToList()
        //    }).ToList();

        //    return Ok(response);
        //}

        [Authorize(Roles = "Customer")]
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new
                {
                    message = "Email claim not found in token."
                });
            }

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerEmail == email)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("summary")]
        public async Task<IActionResult> GetOrderSummary()
        {
            var totalOrders = await _context.Orders.CountAsync();

            var totalSales = await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Pending");

            var confirmedOrders = await _context.Orders
                .CountAsync(o => o.Status == "Confirmed");

            var cancelledOrders = await _context.Orders
                .CountAsync(o => o.Status == "Cancelled");

            return Ok(new
            {
                totalOrders,
                totalSales,
                pendingOrders,
                confirmedOrders,
                cancelledOrders
            });
        }

    }
}
