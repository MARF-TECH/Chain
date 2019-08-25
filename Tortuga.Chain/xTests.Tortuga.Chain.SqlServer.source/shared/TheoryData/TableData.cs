﻿using System.Collections.Generic;
using System.Linq;
using Tortuga.Chain.DataSources;
using Xunit;

namespace Tests
{
    public class TableData : TheoryData<string, DataSourceType, string>
    {
        public TableData(IEnumerable<DataSource> dataSources)
        {
            foreach (var ds in dataSources)
            {
                ds.DatabaseMetadata.Preload();
                foreach (var table in ds.DatabaseMetadata.GetTablesAndViews().Where(t => t.IsTable))
                {
                    Add(ds.Name, DataSourceType.Normal, table.Name);
                    Add(ds.Name, DataSourceType.Open, table.Name);
                    Add(ds.Name, DataSourceType.Transactional, table.Name);
                    Add(ds.Name, DataSourceType.Strict, table.Name);
                }
            }
        }

        public TableData(IEnumerable<DataSource> dataSources, DataSourceType dataSourceType)
        {
            foreach (var ds in dataSources)
            {
                ds.DatabaseMetadata.Preload();
                foreach (var table in ds.DatabaseMetadata.GetTablesAndViews().Where(t => t.IsTable))
                {
                    Add(ds.Name, dataSourceType, table.Name);
                }
            }
        }
    }
}
