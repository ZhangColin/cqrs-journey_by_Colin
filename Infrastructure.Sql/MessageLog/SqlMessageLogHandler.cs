using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;

namespace Infrastructure.Sql.MessageLog {
    public class SqlMessageLogHandler: IEventHandler<IEvent>, ICommandHandler<ICommand> {
        private readonly SqlMessageLog _log;

        public SqlMessageLogHandler(SqlMessageLog log) {
            this._log = log;
        }

        public void Handle(IEvent @event) {
            _log.Save(@event);
        }

        public void Handle(ICommand command) {
            _log.Save(command);
        }
    }
}