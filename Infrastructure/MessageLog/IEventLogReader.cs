using System.Collections.Generic;
using Infrastructure.Messaging;

namespace Infrastructure.MessageLog {
    /// <summary>
    /// 事件日志读取器
    /// </summary>
    public interface IEventLogReader {
        IEnumerable<IEvent> Query(QueryCriteria criteria);
    }
}