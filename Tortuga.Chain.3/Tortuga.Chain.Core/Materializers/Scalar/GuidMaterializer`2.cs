using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Tortuga.Chain.CommandBuilders;

namespace Tortuga.Chain.Materializers
{
    /// <summary>
    /// Materializes the result set as a Guid.
    /// </summary>
    /// <typeparam name="TCommand">The type of the t command type.</typeparam>
    /// <typeparam name="TParameter">The type of the t parameter type.</typeparam>
    internal sealed class GuidMaterializer<TCommand, TParameter> : ScalarMaterializer<TCommand, TParameter, Guid> where TCommand : DbCommand
        where TParameter : DbParameter
    {
        /// <summary>Initializes a new instance of the <see cref="Tortuga.Chain.Materializers.GuidMaterializer{TCommand, TParameter}"/> class.</summary>
        /// <param name="commandBuilder">The command builder.</param>
        /// <param name="columnName">Name of the desired column.</param>
        public GuidMaterializer(DbCommandBuilder<TCommand, TParameter> commandBuilder, string? columnName = null)
            : base(commandBuilder, columnName)
        { }

        /// <summary>
        /// Execute the operation synchronously.
        /// </summary>
        /// <returns></returns>
        public override Guid Execute(object? state = null)
        {
            object? temp = null;
            ExecuteCore(cmd => temp = cmd.ExecuteScalar(), state);
            if (temp == null || temp == DBNull.Value)
                throw new MissingDataException("Unexpected null result");

            return (Guid)temp;
        }

        /// <summary>
        /// Execute the operation asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="state">User defined state, usually used for logging.</param>
        /// <returns></returns>
        public override async Task<Guid> ExecuteAsync(CancellationToken cancellationToken, object? state = null)
        {
            object? temp = null;
            await ExecuteCoreAsync(async cmd => temp = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), cancellationToken, state).ConfigureAwait(false);
            if (temp == null || temp == DBNull.Value)
                throw new MissingDataException("Unexpected null result");

            return (Guid)temp;
        }
    }
}
