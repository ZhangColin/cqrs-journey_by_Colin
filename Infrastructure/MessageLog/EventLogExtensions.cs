using System;
using System.Collections;
using System.Collections.Generic;
using Infrastructure.Messaging;

namespace Infrastructure.MessageLog {
    /// <summary>
    /// 事件日志扩展
    /// </summary>
    public static class EventLogExtensions {
        /// <summary>
        /// 读取所有事件
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public static IEnumerable<IEvent> ReadAll(this IEventLogReader log) {
            return log.Query(new QueryCriteria());
        }

        public static IEventQuery Query(this IEventLogReader log) {
            return new EventQuery(log);
        }

        public interface IEventQuery: IEnumerable<IEvent> {
            IEnumerable<IEvent> Execute();

            IEventQuery WithTypeName(string typeName);

            IEventQuery WithFullName(string fullName);

            IEventQuery FromAssembly(string assemblyName);

            IEventQuery FromNamespace(string @namespace);

            IEventQuery FromSource(string sourceType);

            IEventQuery Until(DateTime endDate);
        }

        /// <summary>
        /// 事件查询
        /// </summary>
        private class EventQuery: IEventQuery, IEnumerable<IEvent> {
            private IEventLogReader _log;
            private QueryCriteria _criteria = new QueryCriteria();

            public EventQuery(IEventLogReader log) {
                this._log = log;
            }

            public IEnumerator<IEvent> GetEnumerator() {
                return this.Execute().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return this.GetEnumerator();
            }

            public IEnumerable<IEvent> Execute() {
                return _log.Query(this._criteria);
            }

            public IEventQuery WithTypeName(string typeName) {
                _criteria.TypeNames.Add(typeName);
                return this;
            }

            public IEventQuery WithFullName(string fullName) {
                _criteria.FullNames.Add(fullName);
                return this;
            }

            public IEventQuery FromAssembly(string assemblyName) {
                _criteria.AssemblyNames.Add(assemblyName);
                return this;
            }

            public IEventQuery FromNamespace(string @namespace) {
                _criteria.Namespaces.Add(@namespace);
                return this;
            }

            public IEventQuery FromSource(string sourceType) {
                _criteria.SourceTypes.Add(sourceType);
                return this;
            }

            public IEventQuery Until(DateTime endDate) {
                _criteria.EndDate = endDate;
                return this;
            }
        }
    }
}