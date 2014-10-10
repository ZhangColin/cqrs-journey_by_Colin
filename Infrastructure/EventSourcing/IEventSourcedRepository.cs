using System;

namespace Infrastructure.EventSourcing {
    /// <summary>
    /// 事件溯源仓储接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEventSourcedRepository<T> where T: IEventSourced {
        T Find(Guid id);
        T Get(Guid id);
        void Save(T eventSourced, string correlationId);
    }
}