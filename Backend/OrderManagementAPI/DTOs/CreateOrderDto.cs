
//It tells the API which product the customer wants and how many units they want.
//The customer does not provide the product price or subtotal;
//our API will calculate those using database values.


using System.ComponentModel.DataAnnotations;

namespace OrderManagementAPI.DTOs
{
    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Customer name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Customer name must be between 2 and 100 characters.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Delivery address is required.")]
        [StringLength(250,
            ErrorMessage = "Delivery address cannot exceed 250 characters.")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one order item is required.")]
        [MinLength(1,
            ErrorMessage = "An order must contain at least one product.")]
        public List<OrderItemDto> Items { get; set; }
            = new List<OrderItemDto>();
    }
}
