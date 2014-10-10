using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Conference.Common;
using Infrastructure.MessageLog;
using Infrastructure.Messaging;
using Infrastructure.Serialization;

namespace Infrastructure.Sql.MessageLog {
    public class SqlMessageLog: IEventLogReader {
        private readonly string _nameOrConnectionString;
        private readonly ITextSerializer _serializer;
        private readonly IMetadataProvider _metadataProvider;

        public SqlMessageLog(string nameOrConnectionString, ITextSerializer serializer, IMetadataProvider metadataProvider) {
            this._nameOrConnectionString = nameOrConnectionString;
            this._serializer = serializer;
            this._metadataProvider = metadataProvider;
        }

        public void Save(IEvent @event) {
            using(var context = new MessageLogDbContext(this._nameOrConnectionString)) {
                IDictionary<string, string> metadata = this._metadataProvider.GetMetadata(@event);

                context.Set<MessageLogEntity>().Add(new MessageLogEntity() {
                    Id = Guid.NewGuid(),
                    SourceId = @event.SourceId.ToString(),
                    Kind = metadata.TryGetValue(StandardMetadata.Kind),
                    AssemblyName = metadata.TryGetValue(StandardMetadata.AssemblyName),
                    FullName = metadata.TryGetValue(StandardMetadata.FullName),
                    Namespace = metadata.TryGetValue(StandardMetadata.Namespace),
                    TypeName = metadata.TryGetValue(StandardMetadata.TypeName),
                    SourceType = metadata.TryGetValue(StandardMetadata.SourceType),
                    CreationDate = DateTime.UtcNow.ToString("o"),
                    Payload = _serializer.Serialize(@event)
                });

                context.SaveChanges();
            }
        }
        
        public void Save(ICommand command) {
            using(var context = new MessageLogDbContext(this._nameOrConnectionString)) {
                IDictionary<string, string> metadata = this._metadataProvider.GetMetadata(command);

                context.Set<MessageLogEntity>().Add(new MessageLogEntity() {
                    Id = Guid.NewGuid(),
                    SourceId = command.Id.ToString(),
                    Kind = metadata.TryGetValue(StandardMetadata.Kind),
                    AssemblyName = metadata.TryGetValue(StandardMetadata.AssemblyName),
                    FullName = metadata.TryGetValue(StandardMetadata.FullName),
                    Namespace = metadata.TryGetValue(StandardMetadata.Namespace),
                    TypeName = metadata.TryGetValue(StandardMetadata.TypeName),
                    SourceType = metadata.TryGetValue(StandardMetadata.SourceType),
                    CreationDate = DateTime.UtcNow.ToString("o"),
                    Payload = _serializer.Serialize(command)
                });

                context.SaveChanges();
            }
        }

        public IEnumerable<IEvent> Query(QueryCriteria criteria) {
            return new SqlQuery(_nameOrConnectionString, this._serializer, criteria);
        }

        private class SqlQuery: IEnumerable<IEvent> {
            private readonly string _nameOrConnectionString;
            private readonly ITextSerializer _serializer;
            private readonly QueryCriteria _criteria;

            public SqlQuery(string nameOrConnectionString, ITextSerializer serializer, QueryCriteria criteria) {
                this._nameOrConnectionString = nameOrConnectionString;
                this._serializer = serializer;
                this._criteria = criteria;
            }

            public IEnumerator<IEvent> GetEnumerator() {
                return new DisposingEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return this.GetEnumerator();
            }

            private class DisposingEnumerator: IEnumerator<IEvent> {
                private readonly SqlQuery _sqlQuery;
                private MessageLogDbContext _context;
                private IEnumerator<IEvent> _events; 

                public DisposingEnumerator(SqlQuery sqlQuery) {
                    this._sqlQuery = sqlQuery;
                }

                ~DisposingEnumerator() {
                    if(_context!=null) {
                        _context.Dispose();
                    }
                }

                public void Dispose() {
                    if(_context!=null) {
                        _context.Dispose();
                        _context = null;
                        GC.SuppressFinalize(this);
                    }
                    if(_events!=null) {
                        _events.Dispose();
                    }
                }

                public bool MoveNext() {
                    if(_context==null) {
                        _context = new MessageLogDbContext(_sqlQuery._nameOrConnectionString);
                        IQueryable<MessageLogEntity> queryable = _context.Set<MessageLogEntity>().AsQueryable()
                            .Where(x => x.Kind == StandardMetadata.EventKind);
                        Expression<Func<MessageLogEntity, bool>> where = _sqlQuery._criteria.ToExpression();
                        if(where!=null) {
                            queryable = queryable.Where(where);
                        }

                        _events = queryable.AsEnumerable()
                            .Select(x => this._sqlQuery._serializer.Deserialize<IEvent>(x.Payload)).GetEnumerator();
                    }

                    return _events.MoveNext();
                }

                public void Reset() {
                    throw new NotSupportedException();
                }

                public IEvent Current {
                    get { return _events.Current; }
                }

                object IEnumerator.Current {
                    get { return Current; }
                }
            }
        }
    }
}