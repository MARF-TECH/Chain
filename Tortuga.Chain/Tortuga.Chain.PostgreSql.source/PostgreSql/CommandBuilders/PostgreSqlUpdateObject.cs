﻿using Npgsql;
using System;
using System.Text;
using Tortuga.Chain.Core;
using Tortuga.Chain.Materializers;

namespace Tortuga.Chain.PostgreSql.CommandBuilders
{
    /// <summary>
    /// Command object that represents an update operation.
    /// </summary>
    internal sealed class PostgreSqlUpdateObject : PostgreSqlObjectCommand
    {
        private readonly UpdateOptions m_Options;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlUpdateObject"/> class.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="argumentValue">The argument value.</param>
        /// <param name="options">The options.</param>
        public PostgreSqlUpdateObject(PostgreSqlDataSourceBase dataSource, PostgreSqlObjectName tableName, object argumentValue, UpdateOptions options)
            : base(dataSource, tableName, argumentValue)
        {
            m_Options = options;
        }

        /// <summary>
        /// Prepares the command for execution by generating any necessary SQL.
        /// </summary>
        /// <param name="materializer"></param>
        /// <returns><see cref="PostgreSqlExecutionToken" /></returns>
        public override ExecutionToken<NpgsqlCommand, NpgsqlParameter> Prepare(Materializer<NpgsqlCommand, NpgsqlParameter> materializer)
        {
            if (materializer == null)
                throw new ArgumentNullException(nameof(materializer), $"{nameof(materializer)} is null.");

            var sqlBuilder = Metadata.CreateSqlBuilder(StrictMode);
            sqlBuilder.ApplyArgumentValue(DataSource, ArgumentValue, m_Options);
            sqlBuilder.ApplyDesiredColumns(materializer.DesiredColumns());

            var sql = new StringBuilder();
            if (m_Options.HasFlag(UpdateOptions.ReturnOldValues))
            {
                sqlBuilder.BuildSelectByKeyStatement(sql, TableName.ToString(), ";");
                sql.AppendLine();
            }
            sqlBuilder.BuildUpdateByKeyStatement(sql, TableName.ToString(), null);
            if(!m_Options.HasFlag(UpdateOptions.ReturnOldValues))
            {
                sqlBuilder.BuildSelectClause(sql, " RETURNING ", null, null);
            }
            sql.Append(";");

            return new PostgreSqlExecutionToken(DataSource, "Update " + TableName, sql.ToString(), sqlBuilder.GetParameters());
        }
    }
}
