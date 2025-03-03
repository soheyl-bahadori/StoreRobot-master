namespace WebHookReceiver.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderId { get; set; }
        public StoreEnum Store { get; set; }
        public string? Dkpc { get; set; }
        public int? Quantity { get; set; }
    }
}
