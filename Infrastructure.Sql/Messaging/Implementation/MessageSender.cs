using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Transactions;

namespace Infrastructure.Sql.Messaging.Implementation {
    public class MessageSender: IMessageSender {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly string _connectionName;
        private readonly string _insertQuery;

        public MessageSender(IDbConnectionFactory connectionFactory, string connectionName, string tableName) {
            this._connectionFactory = connectionFactory;
            this._connectionName = connectionName;
            this._insertQuery = string.Format(
                "INSERT INTO {0} (Body, DeliveryDate, CorrelationId) VALUES (@Body, @DeliveryDate, @CorrelationId)",
                tableName);
        }

        public void Send(Message message) {
            using(var connection = this._connectionFactory.CreateConnection(_connectionName)) {
                connection.Open();
                InsertMessage(message, connection);
            }
        }

        public void Send(IEnumerable<Message> messages) {
            using(var scope = new TransactionScope(TransactionScopeOption.Required)) {
                using(var connection = this._connectionFactory.CreateConnection(this._connectionName)) {
                    connection.Open();
                    foreach(Message message in messages) {
                        this.InsertMessage(message, connection);
                    }
                }

                scope.Complete();
            }
        }

        [SuppressMessage("Microsoft.Security",
            "CA2100:Review SQL queries for security vulnerabilities",
            Justification = "Does not contain user input.")]
        private void InsertMessage(Message message, DbConnection connection) {
            using(var command = (SqlCommand)connection.CreateCommand()) {
                command.CommandText = this._insertQuery;
                command.CommandType = CommandType.Text;
                
                command.Parameters.Add("@Body", SqlDbType.NVarChar).Value = message.Body;
                if(message.DeliveryDate.HasValue) {
                    command.Parameters.Add("@DeliveryDate", SqlDbType.DateTime).Value = message.DeliveryDate;
                }
                else {
                    command.Parameters.Add("@DeliveryDate", SqlDbType.DateTime).Value = DBNull.Value;
                }

                if (!string.IsNullOrEmpty(message.CorrelationId)) {
                    command.Parameters.Add("@CorrelationId", SqlDbType.NVarChar).Value = message.CorrelationId;
                }
                else {
                    command.Parameters.Add("@CorrelationId", SqlDbType.NVarChar).Value = DBNull.Value;
                }

                command.ExecuteNonQuery();
            }
        }

    }
}