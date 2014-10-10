using System;

namespace Registration.ReadModel {
    public interface IOrderDao {
        DraftOrder FindDraftOrder(Guid orderId);
        Guid? LocateOrder(string email, string accessCode);
        PricedOrder FindPricedOrder(Guid orderId);
        OrderSeats FindOrderSeats(Guid assignmentsId);
    }
}