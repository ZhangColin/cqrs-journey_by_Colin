using System;
using Infrastructure.Messaging;

namespace Conference.Contracts {
    /// <summary>
    /// 取消会议事件
    /// </summary>
    public class ConferenceUnpublished : IEvent {
        public Guid SourceId { get; set; }
    }
}