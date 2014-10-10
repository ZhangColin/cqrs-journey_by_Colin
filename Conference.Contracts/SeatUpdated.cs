using System;
using Infrastructure.Messaging;

namespace Conference.Contracts {
    /// <summary>
    /// 座位更新事件
    /// </summary>
    public class SeatUpdated : IEvent {
        public Guid SourceId { get; set; }

        public Guid ConferenceId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}