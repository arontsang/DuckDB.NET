﻿using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace DuckDB.NET.Data
{
    public class DuckDbCommand : DbCommand
    {
        private DuckDBConnection connection;

        public DuckDbCommand()
        {
        }

        public DuckDbCommand(string commandText)
        {
            CommandText = commandText;
        }

        public DuckDbCommand(string commandText, DuckDBConnection connection) : this(commandText)
        {
            Connection = connection;
        }

        public override void Cancel()
        {

        }

        public override int ExecuteNonQuery()
        {
            EnsureConnectionOpen();
            
            using var unmanagedString = CommandText.ToUnmanagedString();
            var queryResult = new DuckDBResult();
            try {
                var result = NativeMethods.Query.DuckDBQuery(connection.NativeConnection, unmanagedString, queryResult);

                if (!result.IsSuccess())
                {
                    var errorMessage = NativeMethods.Query.DuckDBResultError(queryResult).ToManagedString(false);
                    throw new DuckDBException(string.IsNullOrEmpty(errorMessage) ? "DuckDBQuery failed" : errorMessage, result);
                }

                return (int)NativeMethods.Query.DuckDBRowsChanged(queryResult);
            }
            finally {
                NativeMethods.Query.DuckDBDestroyResult(queryResult);
            }
        }

        public override object ExecuteScalar()
        {
            EnsureConnectionOpen();
            
            using var reader = ExecuteReader();
            if (!reader.Read())
                return null;
            return reader.GetValue(0);
        }

        public override void Prepare()
        {
            throw new NotImplementedException();
        }

        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }

        protected override DbConnection DbConnection
        {
            get => connection;
            set => connection = (DuckDBConnection)value;
        }

        internal DuckDBNativeConnection DBNativeConnection => connection.NativeConnection;

        protected override DbParameterCollection DbParameterCollection { get; } = new DuckDBDbParameterCollection();
        protected override DbTransaction DbTransaction { get; set; }
        public override bool DesignTimeVisible { get; set; }

        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            EnsureConnectionOpen();
            return new DuckDBDataReader(this, behavior);
        }

        internal void CloseConnection()
        {
            Connection.Close();
        }

        private void EnsureConnectionOpen([CallerMemberName]string operation = "")
        {
            if (connection.State != ConnectionState.Open)
                throw new InvalidOperationException($"{operation} requires an open connection");
        }
    }
}
