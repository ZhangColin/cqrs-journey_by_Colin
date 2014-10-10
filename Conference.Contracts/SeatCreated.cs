using System;
using Infrastructure.Messaging;

namespace Conference.Contracts {
    /// <summary>
    /// 座位创建事件
    /// </summary>
    public class SeatCreated: IEvent {
        public Guid SourceId { get; set; }

        /// <summary>
        /// 会议Id
        /// </summary>
        public Guid ConferenceId { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 价格
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }
    }
}