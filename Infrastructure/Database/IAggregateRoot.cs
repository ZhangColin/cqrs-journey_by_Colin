using System;

namespace Infrastructure.Database {
    /// <summary>
    /// 聚合根接口
    /// </summary>
    public interface IAggregateRoot {
        Guid Id { get; }
    }
}