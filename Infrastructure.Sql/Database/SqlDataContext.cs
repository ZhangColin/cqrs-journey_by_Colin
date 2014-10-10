using System;
using System.Data.Entity;
using Infrastructure.Database;
using Infrastructure.Messaging;

namespace Infrastructure.Sql.Database {
    public class SqlDataContext<T> : IDataContext<T> where T : class, IAggregateRoot {
        private readonly IEventBus _eventBus;
        private readonly DbContext _context;

        public SqlDataContext(Func<DbContext> contextFactory, IEventBus eventBus) {
            this._eventBus = eventBus;
            this._context = contextFactory.Invoke();
        }

        public T Find(Guid id) {
            return this._context.Set<T>().Find(id);
        }

        public void Save(T aggregate) {
            var entry = this._context.Entry(aggregate);
            if(entry.State==EntityState.Detached) {
                this._context.Set<T>().Add(aggregate);
            }

            this._context.SaveChanges();

            var eventPublisher = aggregate as IEventPublisher;
            if (eventPublisher != null) {
                this._eventBus.Publish(eventPublisher.Events);
            }
        }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SqlDataContext() {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing) {
            if(disposing) {
                this._context.Dispose();
            }
        }
    }
}