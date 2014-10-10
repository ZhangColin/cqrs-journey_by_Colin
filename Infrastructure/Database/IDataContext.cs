using System;

namespace Infrastructure.Database {
    /// <summary>
    /// 数据上下文
    /// </summary>
    /// <typeparam name="T">聚合类型</typeparam>
    public interface IDataContext<T>: IDisposable
        where T : IAggregateRoot {

        T Find(Guid id);
        void Save(T aggregate);
    }
}