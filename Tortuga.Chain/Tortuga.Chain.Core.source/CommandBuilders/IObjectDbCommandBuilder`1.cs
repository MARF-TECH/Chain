namespace Tortuga.Chain.CommandBuilders
{
    /// <summary>
    /// This represents command builders that operate on single object parameters: Insert, Update, Upsert, Delete
    /// </summary>
    /// <typeparam name="TArgument">The type of the argument.</typeparam>
    public interface IObjectDbCommandBuilder<TArgument> : ISingleRowDbCommandBuilder
    {
        /// <summary>
        /// Gets the argument value passed to the command builder.
        /// </summary>
        /// <value>The argument value.</value>
        TArgument ArgumentValue { get; }

        /// <summary>
        /// After executing the operation, refreshes the properties on the argumentValue by reading the updated values from the database.
        /// </summary>
        /// <returns></returns>
        ILink<TArgument> WithRefresh();



        /// <summary>
        /// Materializes the result as a new instance of the same type as the argumentValue
        /// </summary>
        /// <param name="rowOptions">The row options.</param>
        /// <returns></returns>
        /// <remarks>To update the argumentValue itselt, use WithRefresh() instead.</remarks>
        ILink<TArgument> ToObject(RowOptions rowOptions = RowOptions.None);
    }
}
