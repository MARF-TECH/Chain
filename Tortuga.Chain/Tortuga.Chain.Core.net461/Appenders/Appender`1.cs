using System;
using System.Threading;
using System.Threading.Tasks;
using Tortuga.Chain.Core;
using Tortuga.Chain.DataSources;
namespace Tortuga.Chain.Appenders
{
    /// <summary>
    /// An appender modifies the execution chain of an operation, usually by performing an action just before or after the database call.
    /// </summary>
    /// <typeparam name="TResultType">The operation's result type.</typeparam>
    public abstract class Appender<TResultType> : ILink<TResultType>
    {
        private readonly ILink<TResultType> m_PreviousLink;

        /// <summary>
        /// Initializes a new instance of the <see cref="Appender{TResultType}"/> class.
        /// </summary>
        /// <param name="previousLink">The previous link.</param>
        protected Appender(ILink<TResultType> previousLink)
        {
            if (previousLink == null)
                throw new ArgumentNullException("previousLink", "previousLink is null.");

            m_PreviousLink = previousLink;
            m_PreviousLink.ExecutionTokenPrepared += (s, e) => ExecutionTokenPrepared?.Invoke(this, e);

        }



        /// <summary>
        /// Gets the data source that is associated with this materilizer or appender.
        /// </summary>
        /// <value>The data source.</value>
        public DataSource DataSource
        {
            get { return PreviousLink.DataSource; }
        }

        /// <summary>
        /// Gets the previous link in the operation chain.
        /// </summary>
        public ILink<TResultType> PreviousLink
        {
            get { return m_PreviousLink; }
        }

        /// <summary>
        /// Execute the operation synchronously.
        /// </summary>
        /// <param name="state">User defined state, usually used for logging.</param>
        public abstract TResultType Execute(object state = null);

        /// <summary>
        /// Execute the operation asynchronously.
        /// </summary>
        /// <param name="state">User defined state, usually used for logging.</param>
        /// <returns></returns>
        public Task<TResultType> ExecuteAsync(object state = null)
        {
            return ExecuteAsync(CancellationToken.None, state);
        }

        /// <summary>
        /// Execute the operation asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="state">User defined state, usually used for logging.</param>
        /// <returns></returns>
        public abstract Task<TResultType> ExecuteAsync(CancellationToken cancellationToken, object state = null);

        /// <summary>
        /// Occurs when an execution token has been prepared.
        /// </summary>
        /// <remarks>This is mostly used by appenders to override command behavior.</remarks>
        public event EventHandler<ExecutionTokenPreparedEventArgs> ExecutionTokenPrepared;
    }
}