﻿using System;
using System.Diagnostics.CodeAnalysis;
using Tortuga.Chain.Metadata;

namespace Tortuga.Chain
{
    /// <summary>
    /// Sort expressions are used for From and FromFunction command builders.
    /// </summary>
    /// <remarks>You can implicitly convert strings into sort expressions.</remarks>
    public class SortExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SortExpression"/> class.
        /// </summary>
        /// <param name="columnName">Name of the column to be sorted by.</param>
        /// <param name="descending">if set to <c>true</c> [descending].</param>
        /// <exception cref="ArgumentException"></exception>
        public SortExpression(string columnName, SortDirection descending)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException($"{nameof(columnName)} is null or empty.", nameof(columnName));

            ColumnName = columnName;
            Direction = descending;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortExpression"/> class.
        /// </summary>
        /// <param name="columnName">Name of the column to be sorted by.</param>
        /// <exception cref="ArgumentException"></exception>
        public SortExpression(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException($"{nameof(columnName)} is null or empty.", nameof(columnName));

            ColumnName = columnName;
        }

        /// <summary>
        /// Gets the name of the column to be sorted by.
        /// </summary>
        /// <value>
        /// The name of the column.
        /// </value>
        public string ColumnName { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="SortExpression"/> is descending.
        /// </summary>
        /// <value>
        ///   <c>true</c> if descending; otherwise, <c>false</c>.
        /// </value>
        public SortDirection Direction { get; }

        internal ISqlBuilderEntryDetails? Column { get; set; }

        /// <summary>
        /// Perform an implicit conversion from <see cref="string"/> to <see cref="SortExpression"/> with Ascending as the sort direction.
        /// </summary>
        /// <param name="columnName">The columnName</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
        public static implicit operator SortExpression(string columnName) => new SortExpression(columnName);
    }
}
