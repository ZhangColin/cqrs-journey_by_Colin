using System;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Sql.Messaging.Implementation {
    public class MessageReceiver: IMessageReceiver, IDisposable {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly string _connectionName;
        private readonly TimeSpan _pollDelay;
        private readonly string _readQuery;
        private readonly string _deleteQuery;
        private readonly object _lockObject = new object();
        private CancellationTokenSource _cancellationSource;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public MessageReceiver(IDbConnectionFactory connectionFactory, string connectionName, string tableName)
            : this(connectionFactory, connectionName, tableName, TimeSpan.FromMilliseconds(100)) {}

        public MessageReceiver(IDbConnectionFactory connectionFactory, string connectionName, string tableName, TimeSpan pollDelay) {
            this._connectionFactory = connectionFactory;
            this._connectionName = connectionName;
            this._pollDelay = pollDelay;

            this._readQuery =
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"SELECT TOP (1) 
                    {0}.[Id] AS [Id], 
                    {0}.[Body] AS [Body], 
                    {0}.[DeliveryDate] AS [DeliveryDate],
                    {0}.[CorrelationId] AS [CorrelationId]
                    FROM {0} WITH (UPDLOCK, READPAST)
                    WHERE ({0}.[DeliveryDate] IS NULL) OR ({0}.[DeliveryDate] <= @CurrentDate)
                    ORDER BY {0}.[Id] ASC",
                    tableName);
            this._deleteQuery =
                string.Format(
                   CultureInfo.InvariantCulture,
                   "DELETE FROM {0} WHERE Id = @Id",
                   tableName);
        }

        public void Start() {
            lock(this._lockObject) {
                if(this._cancellationSource == null) {
                    this._cancellationSource = new CancellationTokenSource();
                    Task.Factory.StartNew(() => this.ReceiveMessages(this._cancellationSource.Token),
                        this._cancellationSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                }
            }
        }

        public void Stop() {
            lock(this._lockObject) {
                using(this._cancellationSource) {
                    if(this._cancellationSource!=null) {
                        this._cancellationSource.Cancel();
                        this._cancellationSource = null;
                    }
                }
            }
        }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            this.Stop();
        }

        ~MessageReceiver() {
            this.Dispose(false);
        }

        private void ReceiveMessages(CancellationToken cancellationToken) {
            while(!cancellationToken.IsCancellationRequested) {
                if(!this.ReceiveMessage()) {
                    Thread.Sleep(this._pollDelay);
                }
            }
        }

        [SuppressMessage("Microsoft.Security",
            "CA2100:Review SQL queries for security vulnerabilities",
            Justification = "Does not contain user input.")]
        protected bool ReceiveMessage() {
            using(var connection = this._connectionFactory.CreateConnection(this._connectionName)) {
                DateTime currentDate = this.GetCurrentDate();

                connection.Open();

                using(var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted)) {
                    try {
                        long messageId = -1;
                        Message message = null;

                        using(var command = connection.CreateCommand()) {
                            command.Transaction = transaction;
                            command.CommandType = CommandType.Text;
                            command.CommandText = this._readQuery;

                            ((SqlCommand)command).Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value = currentDate;

                            using(var reader = command.ExecuteReader()) {
                                if(!reader.Read()) {
                                    return false;
                                }

                                var body = (string)reader["Body"];
                                var deliveryDateValue = reader["DeliveryDate"];
                                var deliveryDate = deliveryDateValue == DBNull.Value
                                    ? (DateTime?)null : new DateTime?((DateTime)deliveryDateValue);
                                var correlationIdValue = reader["CorrelationId"];
                                var correlationId =
                                    (string)(correlationIdValue == DBNull.Value ? null : correlationIdValue);

                                message = new Message(body, deliveryDate, correlationId);
                                messageId = (long)reader["Id"];
                            }
                        }

                        if(this.MessageReceived!=null) {
                            this.MessageReceived(this, new MessageReceivedEventArgs(message));
                        }

                        using(var command = connection.CreateCommand()) {
                            command.Transaction = transaction;
                            command.CommandType = CommandType.Text;
                            command.CommandText = this._deleteQuery;
                            ((SqlCommand)command).Parameters.Add("@Id", SqlDbType.BigInt).Value = messageId;

                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch(Exception) {
                        try {
                            transaction.Rollback();
                        }
                        catch {
                            
                        }
                        throw;
                    }
                }
            }

            return true;
        }

        protected virtual DateTime GetCurrentDate() {
            return DateTime.UtcNow;
        }
    }
}