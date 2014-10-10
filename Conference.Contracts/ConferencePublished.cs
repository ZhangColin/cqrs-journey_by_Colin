using System;
using Infrastructure.Messaging;

namespace Conference.Contracts {
    /// <summary>
    /// 会议发布事件
    /// </summary>
    public class ConferencePublished : IEvent{
        public Guid SourceId { get; set; }
    }
}