using Registration.ReadModel;

namespace Conference.Web.Public.Models {
    public class OrderItemViewModel {
        public SeatType SeatType { get; set; }
        public DraftOrderItem OrderItem { get; set; }
        public bool PartiallyFulfilled { get; set; }
        public int AvailableQuantityForOrder { get; set; }
        public int MaxSelectionQuantity { get; set; }
    }
}