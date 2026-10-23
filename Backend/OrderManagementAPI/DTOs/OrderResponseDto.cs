namespace OrderManagementAPI.DTOs
{
    //This DTO defines the order information the API will return after successfully creating an order.
    public class OrderResponseDto
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string DeliveryAddress { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public List<OrderItemResponseDto> Items { get; set; }
           = new List<OrderItemResponseDto>();
    }
}
