using System;
using System.Linq.Expressions;

namespace Infrastructure.Processes {
    /// <summary>
    /// 长时任务数据上下文接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IProcessManagerDataContext<T>: IDisposable
        where T : class, IProcessManager {
        T Find(Guid id);
        void Save(T processManager);
        T Find(Expression<Func<T, bool>> predicate, bool includeCompleted = false);
    }
}