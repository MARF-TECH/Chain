﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tortuga.Chain.CommandBuilders;
using Tortuga.Chain.Core;
using Tortuga.Chain.Materializers;
using Tortuga.Chain.Metadata;

#if !SqlDependency_Missing

using Tortuga.Chain.SqlServer.Materializers;

#endif

#if SQL_SERVER_SDS

using System.Data.SqlClient;

#elif SQL_SERVER_MDS

using Microsoft.Data.SqlClient;

#endif

namespace Tortuga.Chain.SqlServer.CommandBuilders
{
    /// <summary>
    /// SqlServerTableOrView supports queries against tables and views.
    /// </summary>
    internal sealed partial class SqlServerTableOrView<TObject> : TableDbCommandBuilder<SqlCommand, SqlParameter, SqlServerLimitOption, TObject>, ISupportsApproximateCount
        where TObject : class
    {
        readonly TableOrViewMetadata<SqlServerObjectName, SqlDbType> m_Table;
        object? m_FilterValue;
        string? m_WhereClause;
        object? m_ArgumentValue;
        IEnumerable<SortExpression> m_SortExpressions = Enumerable.Empty<SortExpression>();
        SqlServerLimitOption m_LimitOptions;
        int? m_Skip;
        int? m_Take;
        int? m_Seed;
        string? m_SelectClause;
        FilterOptions m_FilterOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerTableOrView{TObject}" /> class.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        /// <param name="tableOrViewName">Name of the table or view.</param>
        /// <param name="filterValue">The filter value.</param>
        /// <param name="filterOptions">The filter options.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public SqlServerTableOrView(SqlServerDataSourceBase dataSource, SqlServerObjectName tableOrViewName, object filterValue, FilterOptions filterOptions = FilterOptions.None) : base(dataSource)
        {
            if (tableOrViewName == SqlServerObjectName.Empty)
                throw new ArgumentException($"{nameof(tableOrViewName)} is empty", nameof(tableOrViewName));

            m_FilterValue = filterValue;
            m_FilterOptions = filterOptions;
            m_Table = DataSource.DatabaseMetadata.GetTableOrView(tableOrViewName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerTableOrView{TObject}"/> class.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        /// <param name="tableOrViewName">Name of the table or view.</param>
        /// <param name="whereClause">The where clause.</param>
        /// <param name="argumentValue">The argument value.</param>
        public SqlServerTableOrView(SqlServerDataSourceBase dataSource, SqlServerObjectName tableOrViewName, string? whereClause, object? argumentValue) : base(dataSource)
        {
            if (tableOrViewName == SqlServerObjectName.Empty)
                throw new ArgumentException($"{nameof(tableOrViewName)} is empty", nameof(tableOrViewName));

            m_ArgumentValue = argumentValue;
            m_WhereClause = whereClause;
            m_Table = DataSource.DatabaseMetadata.GetTableOrView(tableOrViewName);
        }

        /// <summary>
        /// Prepares the command for execution by generating any necessary SQL.
        /// </summary>
        /// <param name="materializer">The materializer.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public override CommandExecutionToken<SqlCommand, SqlParameter> Prepare(Materializer<SqlCommand, SqlParameter> materializer)
        {
            if (materializer == null)
                throw new ArgumentNullException(nameof(materializer), $"{nameof(materializer)} is null.");

            var sqlBuilder = m_Table.CreateSqlBuilder(StrictMode);
            sqlBuilder.ApplyRulesForSelect(DataSource);

            if (m_SelectClause == null)
                sqlBuilder.ApplyDesiredColumns(materializer.DesiredColumns());

            //Support check
            if (!Enum.IsDefined(typeof(SqlServerLimitOption), m_LimitOptions))
                throw new NotSupportedException($"SQL Server does not support limit option {(LimitOptions)m_LimitOptions}");

            //Validation
            if (m_Skip < 0)
                throw new InvalidOperationException($"Cannot skip {m_Skip} rows");

            if (m_Skip > 0 && !m_SortExpressions.Any())
                throw new InvalidOperationException($"Cannot perform a Skip operation with out a sort expression.");

            if (m_Skip > 0 && m_LimitOptions != SqlServerLimitOption.Rows)
                throw new InvalidOperationException($"Cannot perform a Skip operation with limit option {m_LimitOptions}");

            if (m_Take <= 0)
                throw new InvalidOperationException($"Cannot take {m_Take} rows");

            if ((m_LimitOptions == SqlServerLimitOption.TableSampleSystemRows || m_LimitOptions == SqlServerLimitOption.TableSampleSystemPercentage) && m_SortExpressions.Any())
                throw new InvalidOperationException($"Cannot perform random sampling when sorting.");

            if ((m_LimitOptions == SqlServerLimitOption.RowsWithTies || m_LimitOptions == SqlServerLimitOption.PercentageWithTies) && !m_SortExpressions.Any())
                throw new InvalidOperationException($"Cannot perform a WITH TIES operation without sorting.");

            //SQL Generation
            List<SqlParameter> parameters;
            var sql = new StringBuilder();

            string? topClause = null;
            switch (m_LimitOptions)
            {
                case SqlServerLimitOption.Rows:
                    if (!m_SortExpressions.Any())
                        topClause = $"TOP (@fetch_row_count_expression) ";
                    break;

                case SqlServerLimitOption.Percentage:
                    topClause = $"TOP (@fetch_row_count_expression) PERCENT ";
                    break;

                case SqlServerLimitOption.PercentageWithTies:
                    topClause = $"TOP (@fetch_row_count_expression) PERCENT WITH TIES ";
                    break;

                case SqlServerLimitOption.RowsWithTies:
                    topClause = $"TOP (@fetch_row_count_expression) WITH TIES ";
                    break;
            }

            if (m_SelectClause != null)
                sql.Append($"SELECT {topClause} {m_SelectClause} ");
            else
                sqlBuilder.BuildSelectClause(sql, "SELECT " + topClause, null, null);

            sql.Append(" FROM " + m_Table.Name.ToQuotedString());

            switch (m_LimitOptions)
            {
                case SqlServerLimitOption.TableSampleSystemRows:
                    sql.Append($" TABLESAMPLE SYSTEM ({m_Take} ROWS) ");
                    if (m_Seed.HasValue)
                        sql.Append($"REPEATABLE ({m_Seed}) ");
                    break;

                case SqlServerLimitOption.TableSampleSystemPercentage:
                    sql.Append($" TABLESAMPLE SYSTEM ({m_Take} PERCENT) ");
                    if (m_Seed.HasValue)
                        sql.Append($"REPEATABLE ({m_Seed}) ");
                    break;
            }

            if (m_FilterValue != null)
            {
                sql.Append(" WHERE (" + sqlBuilder.ApplyFilterValue(m_FilterValue, m_FilterOptions) + ")");
                sqlBuilder.BuildSoftDeleteClause(sql, " AND (", DataSource, ") ");

                parameters = sqlBuilder.GetParameters();
            }
            else if (!string.IsNullOrWhiteSpace(m_WhereClause))
            {
                sql.Append(" WHERE (" + m_WhereClause + ")");
                sqlBuilder.BuildSoftDeleteClause(sql, " AND (", DataSource, ") ");

                parameters = SqlBuilder.GetParameters<SqlParameter>(m_ArgumentValue);
                parameters.AddRange(sqlBuilder.GetParameters());
            }
            else
            {
                sqlBuilder.BuildSoftDeleteClause(sql, " WHERE ", DataSource, null);
                parameters = sqlBuilder.GetParameters();
            }
            sqlBuilder.BuildOrderByClause(sql, " ORDER BY ", m_SortExpressions, null);

            switch (m_LimitOptions)
            {
                case SqlServerLimitOption.Rows:

                    if (m_SortExpressions.Any())
                    {
                        sql.Append(" OFFSET @offset_row_count_expression ROWS ");
                        parameters.Add(new SqlParameter("@offset_row_count_expression", m_Skip ?? 0));

                        if (m_Take.HasValue)
                        {
                            sql.Append(" FETCH NEXT @fetch_row_count_expression ROWS ONLY");
                            parameters.Add(new SqlParameter("@fetch_row_count_expression", m_Take));
                        }
                    }
                    else
                    {
                        parameters.Add(new SqlParameter("@fetch_row_count_expression", m_Take));
                    }
                    break;

                case SqlServerLimitOption.Percentage:
                case SqlServerLimitOption.PercentageWithTies:
                case SqlServerLimitOption.RowsWithTies:
                    parameters.Add(new SqlParameter("@fetch_row_count_expression", m_Take));

                    break;
            }

            sql.Append(";");

            return new SqlServerCommandExecutionToken(DataSource, "Query " + m_Table.Name, sql.ToString(), parameters);
        }

        /// <summary>
        /// Adds sorting to the command builder.
        /// </summary>
        /// <param name="sortExpressions">The sort expressions.</param>
        /// <returns></returns>
        protected override TableDbCommandBuilder<SqlCommand, SqlParameter, SqlServerLimitOption> OnWithSorting(IEnumerable<SortExpression> sortExpressions)
        {
            if (sortExpressions == null)
                throw new ArgumentNullException(nameof(sortExpressions), $"{nameof(sortExpressions)} is null.");

            m_SortExpressions = sortExpressions;
            return this;
        }

        /// <summary>
        /// Adds limits to the command builder.
        /// </summary>
        /// <param name="skip">The number of rows to skip.</param>
        /// <param name="take">Number of rows to take.</param>
        /// <param name="limitOptions">The limit options.</param>
        /// <param name="seed">The seed for repeatable reads. Only applies to random sampling</param>
        /// <returns></returns>
        protected override TableDbCommandBuilder<SqlCommand, SqlParameter, SqlServerLimitOption> OnWithLimits(int? skip, int? take, SqlServerLimitOption limitOptions, int? seed)
        {
            m_Seed = seed;
            m_Skip = skip;
            m_Take = take;
            m_LimitOptions = limitOptions;
            return this;
        }

        /// <summary>
        /// Adds limits to the command builder.
        /// </summary>
        /// <param name="skip">The number of rows to skip.</param>
        /// <param name="take">Number of rows to take.</param>
        /// <param name="limitOptions">The limit options.</param>
        /// <param name="seed">The seed for repeatable reads. Only applies to random sampling</param>
        /// <returns></returns>
        protected override TableDbCommandBuilder<SqlCommand, SqlParameter, SqlServerLimitOption> OnWithLimits(int? skip, int? take, LimitOptions limitOptions, int? seed)
        {
            m_Seed = seed;
            m_Skip = skip;
            m_Take = take;
            m_LimitOptions = (SqlServerLimitOption)limitOptions;
            return this;
        }

        /// <summary>
        /// Adds (or replaces) the filter on this command builder.
        /// </summary>
        /// <param name="filterValue">The filter value.</param>
        /// <param name="filterOptions">The filter options.</param>
        /// <returns>TableDbCommandBuilder&lt;SqlCommand, SqlParameter, SqlServerLimitOption&gt;.</returns>
        protected override TableDbCommandBuilder<SqlCommand, SqlParameter, SqlServerLimitOption> OnWithFilter(object filterValue, FilterOptions filterOptions = FilterOptions.None)
        {
            m_FilterValue = filterValue;
            m_WhereClause = null;
            m_ArgumentValue = null;
            m_FilterOptions = filterOptions;
            return this;
        }

        /// <summary>
        /// Adds (or replaces) the filter on this command builder.
        /// </summary>
        /// <param name="whereClause">The where clause.</param>
        /// <param name="argumentValue">The argument value.</param>
        /// <returns></returns>
        protected override TableDbCommandBuilder<SqlCommand, SqlParameter, SqlServerLimitOption> OnWithFilter(string whereClause, object? argumentValue)
        {
            m_FilterValue = null;
            m_WhereClause = whereClause;
            m_ArgumentValue = argumentValue;
            return this;
        }

        /// <summary>
        /// Returns the row count using a <c>SELECT COUNT_BIG(*)</c> style query.
        /// </summary>
        /// <returns></returns>
        public override ILink<long> AsCount()
        {
            m_SelectClause = "COUNT_BIG(*)";
            return ToInt64();
        }

        /// <summary>
        /// Returns the row count for a given column. <c>SELECT COUNT_BIG(columnName)</c>
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="distinct">if set to <c>true</c> use <c>SELECT COUNT_BIG(DISTINCT columnName)</c>.</param>
        /// <returns></returns>
        public override ILink<long> AsCount(string columnName, bool distinct = false)
        {
            var column = m_Table.Columns[columnName];
            if (distinct)
                m_SelectClause = $"COUNT_BIG(DISTINCT {column.QuotedSqlName})";
            else
                m_SelectClause = $"COUNT_BIG({column.QuotedSqlName})";

            return ToInt64();
        }

        /// <summary>
        /// Returns the column associated with the column name.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <returns></returns>
        /// <remarks>
        /// If the column name was not found, this will return null
        /// </remarks>
        public override ColumnMetadata? TryGetColumn(string columnName)
        {
            return m_Table.Columns.TryGetColumn(columnName);
        }

        /// <summary>
        /// Gets the data source.
        /// </summary>
        /// <value>The data source.</value>
        public new SqlServerDataSourceBase DataSource
        {
            get { return (SqlServerDataSourceBase)base.DataSource; }
        }

        /// <summary>
        /// Returns a list of columns known to be non-nullable.
        /// </summary>
        /// <returns>
        /// If the command builder doesn't know which columns are non-nullable, an empty list will be returned.
        /// </returns>
        /// <remarks>
        /// This is used by materializers to skip IsNull checks.
        /// </remarks>
        public override IReadOnlyList<ColumnMetadata> TryGetNonNullableColumns() => m_Table.NonNullableColumns;

        /// <summary>
        /// Return the approximate distinct count using the APPROX_COUNT_DISTINCT function.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        public ILink<long> AsCountApproximate(string columnName)
        {
            var column = m_Table.Columns[columnName];
            m_SelectClause = $"APPROX_COUNT_DISTINCT({column.QuotedSqlName})";

            return ToInt64();
        }

        /// <summary>
        /// Return the approximate row count using the APPROX_COUNT_DISTINCT function.
        /// </summary>
        /// <remarks>This is only available on tables with a single primary key.</remarks>
        public ILink<long> AsCountApproximate()
        {
            var primaryKeys = m_Table.PrimaryKeyColumns;
            if (primaryKeys.Count != 1)
                throw new MappingException($"{nameof(AsCountApproximate)}() operation isn't allowed on {m_Table.Name} because it doesn't have a single primary key. Please provide a column name.");

            m_SelectClause = $"APPROX_COUNT_DISTINCT({primaryKeys.Single().QuotedSqlName})";

            return ToInt64();
        }
    }

#if !SqlDependency_Missing

    partial class SqlServerTableOrView<TObject> : ISupportsChangeListener
    {
        /// <summary>
        /// Waits for change in the data that is returned by this operation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="state">User defined state, usually used for logging.</param>
        /// <returns>Task that can be waited for.</returns>
        /// <remarks>This requires the use of SQL Dependency</remarks>
        public Task WaitForChange(CancellationToken cancellationToken, object? state = null)
        {
            return WaitForChangeMaterializer.GenerateTask(this, cancellationToken, state);
        }

        SqlServerCommandExecutionToken ISupportsChangeListener.Prepare(Materializer<SqlCommand, SqlParameter> materializer)
        {
            return (SqlServerCommandExecutionToken)Prepare(materializer);
        }
    }

#endif
}
