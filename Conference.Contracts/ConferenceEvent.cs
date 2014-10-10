using System;
using Infrastructure.Messaging;

namespace Conference.Contracts {
    /// <summary>
    /// 会议事件
    /// </summary>
    public class ConferenceEvent : IEvent {
        /// <summary>
        /// 事件Id
        /// </summary>
        public Guid SourceId { get; set; }

        /// <summary>
        /// 会议名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 会议描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 会议位置
        /// </summary>
        public string Location { get; set; }
        public string Slug { get; set; }

        /// <summary>
        /// 标语
        /// </summary>
        public string Tagline { get; set; }

        /// <summary>
        /// 推特搜索
        /// </summary>
        public string TwitterSearch { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// 会议发起人
        /// </summary>
        public Owner Owner { get; set; }
    }
}
