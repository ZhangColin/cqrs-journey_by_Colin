using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Infrastructure.Messaging;
using Infrastructure.Processes;
using Infrastructure.Serialization;

namespace Infrastructure.Sql.Processes {
    public class SqlProcessManagerDataContext<T>: IProcessManagerDataContext<T>
        where T : class, IProcessManager {
        private readonly ICommandBus _commandBus;
        private readonly ITextSerializer _serializer;
        private readonly DbContext _context;

        public SqlProcessManagerDataContext(Func<DbContext> contextFactory, ICommandBus commandBus, ITextSerializer serializer) {
            this._commandBus = commandBus;
            this._serializer = serializer;
            this._context = contextFactory.Invoke();
        }

        public T Find(Guid id) {
            return Find(processManager => processManager.Id == id, true);
        }

        public T Find(Expression<Func<T, bool>> predicate, bool includeCompleted = false) {
            T processManager = null;
            if(!includeCompleted) {
                processManager = this._context.Set<T>().Where(predicate.And(x=>x.Completed==false)).FirstOrDefault();
            }

            if(processManager==null) {
                processManager = this._context.Set<T>().Where(predicate).FirstOrDefault();
            }

            if(processManager!=null) {
                UndispatchedMessages undispatchedMessages = this._context.Set<UndispatchedMessages>().Find(processManager.Id);
                try {
                    this.DispatchMessages(undispatchedMessages);
                }
                catch(Exception ex) {
                    Trace.TraceWarning(
                        "Concurrency exception while marking commands as dispatched for process manager with ID {0} in Find method.",
                        processManager.Id);

                    this._context.Entry(undispatchedMessages).Reload();
                    undispatchedMessages = this._context.Set<UndispatchedMessages>().Find(processManager.Id);

                    this.DispatchMessages(undispatchedMessages);
                }

                if(!processManager.Completed||includeCompleted) {
                    return processManager;
                }
            }

            return null;
        }

        private void DispatchMessages(UndispatchedMessages undispatched, List<Envelope<ICommand>> deserializedCommands=null) {
            if(undispatched!=null) {
                if(deserializedCommands==null) {
                    deserializedCommands =
                        this._serializer.Deserialize<IEnumerable<Envelope<ICommand>>>(undispatched.Commands).ToList();
                }

                var originalCommandsCount = deserializedCommands.Count;
                try {
                    while(deserializedCommands.Count>0) {
                        this._commandBus.Send(deserializedCommands.First());
                        deserializedCommands.RemoveAt(0);
                    }
                }
                catch(Exception) {
                    if(originalCommandsCount!=deserializedCommands.Count)
                    {
                        undispatched.Commands = this._serializer.Serialize(deserializedCommands);
                        try {
                            this._context.SaveChanges();
                        }
                        catch(DbUpdateConcurrencyException) {
                            
                        }
                    }
                    throw;
                }

                this._context.Set<UndispatchedMessages>().Remove(undispatched);
                this._context.SaveChanges();
            }
        }

        public void Save(T processManager) {
            DbEntityEntry<T> entry = this._context.Entry(processManager);

            if(entry.State==EntityState.Detached) {
                this._context.Set<T>().Add(processManager);
            }

            List<Envelope<ICommand>> commands = processManager.Commands.ToList();
            UndispatchedMessages undispatched = null;

            if(commands.Count>0) {
                undispatched = new UndispatchedMessages(processManager.Id) {
                    Commands = this._serializer.Serialize(commands)
                };
                this._context.Set<UndispatchedMessages>().Add(undispatched);
            }

            try {
                this._context.SaveChanges();
            }
            catch(DbUpdateConcurrencyException e) {
                throw new ConcurrencyException(e.Message, e);
            }

            try {
                this.DispatchMessages(undispatched, commands);
            }
            catch(DbUpdateConcurrencyException) {
                Trace.TraceWarning("Ignoring concurrency exception while marking commands as dispatched for process manager with ID {0} in Save method.", processManager.Id);
            }
        }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SqlProcessManagerDataContext() {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing) {
            if(disposing) {
                this._context.Dispose();
            }
        }
    }
}