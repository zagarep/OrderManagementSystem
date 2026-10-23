
using System.ComponentModel.DataAnnotations;

namespace OrderManagementAPI.DTOs
{
    public class OrderItemDto
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "Product ID must be greater than zero.")]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Quantity must be greater than zero.")]
        public int Quantity { get; set; }
    }
}
