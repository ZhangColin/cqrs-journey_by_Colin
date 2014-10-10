using System;
using System.Collections;
using System.Collections.Generic;
using Infrastructure.EventSourcing;

namespace Registration.Contracts.Events {
    /// <summary>
    /// 预订事件
    /// </summary>
    public class OrderPartiallyReserved: VersionedEvent {
        /// <summary>
        /// 预订到期时间
        /// </summary>
        public DateTime ReservationExpiration { get; set; }

        public IEnumerable<SeatQuantity> Seats { get; set; }
    }
}