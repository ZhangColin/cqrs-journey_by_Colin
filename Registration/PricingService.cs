using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Registration.Contracts;
using Registration.ReadModel;

namespace Registration {
    public class PricingService: IPricingService {
        private readonly IConferenceDao _conferenceDao;

        public PricingService(IConferenceDao conferenceDao) {
            if(conferenceDao==null) {
                throw new ArgumentNullException("conferenceDao");
            }
            this._conferenceDao = conferenceDao;
        }

        public OrderTotal CalculateTotal(Guid conferenceId, ICollection<SeatQuantity> seatItems) {
            var seatTypes = this._conferenceDao.GetPublishedSeatTypes(conferenceId);
            var lineItems = new List<OrderLine>();

            foreach(var item in seatItems) {
                var seatType = seatTypes.FirstOrDefault(x => x.Id == item.SeatType);

                if(seatType==null) {
                    throw new InvalidDataException(
                        string.Format("Invalid seat type ID '{0}' for conference with ID '{1}'", item.SeatType,
                            conferenceId));
                }

                lineItems.Add(new SeatOrderLine() {
                    SeatType = item.SeatType, Quantity = item.Quantity, UnitPrice = seatType.Price,
                    LineTotal = Math.Round(seatType.Price * item.Quantity, 2)
                });
            }

            return new OrderTotal() {
                Total = lineItems.Sum(x=>x.LineTotal),
                Lines = lineItems
            };
        }
    }
}