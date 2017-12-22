﻿using System;

namespace Tortuga.Chain
{

    /// <summary>
    /// Controls what happens when performing a insert across a set of records
    /// </summary>
    [Flags]
    public enum InsertBatchOptions
    {

        /// <summary>
        /// Use the default behavior.
        /// </summary>
        None = 0,


        /*
         * Task-86: Add support for Identity Insert in SQL Server
        /// <summary>
        /// Override the identity/auto-number column.
        /// </summary>
        /// <remarks>This may require elevated privileges.</remarks>
        IdentityOverride = 2
        */

    }
}
