﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TableDataAttribute : DataAttribute
    {
        public TableDataAttribute(DataSourceGroup dataSourceGroup) : base(dataSourceGroup)
        {
        }

        public override IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            foreach (var ds in DataSources)
            {
                ds.DatabaseMetadata.Preload();

                foreach (var table in ds.DatabaseMetadata.GetTablesAndViews().Where(t => t.IsTable))
                    foreach (var dst in DataSourceTypeList)
                        yield return new object[] { ds.Name, dst, table.Name };
            }
        }
    }
}