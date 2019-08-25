using System;
using System.Collections.Generic;
using System.Linq;
using Tortuga.Chain;
using Tortuga.Chain.DataSources;
using Xunit;

namespace Tests
{
    public class TablesAndViewLimitData<TEnum> : TheoryData<string, DataSourceType, string, LimitOptions>
    {
        public TablesAndViewLimitData(IEnumerable<DataSource> dataSources)
        {
            foreach (var ds in dataSources)
            {
                ds.DatabaseMetadata.Preload();
                foreach (var table in ds.DatabaseMetadata.GetTablesAndViews().Where(t => t.IsTable))
                {
                    foreach (var value in Enum.GetValues(typeof(TEnum)))
                    {
                        var castValue = (LimitOptions)value;
                        Add(ds.Name, DataSourceType.Normal, table.Name, castValue);
                        Add(ds.Name, DataSourceType.Open, table.Name, castValue);
                        Add(ds.Name, DataSourceType.Transactional, table.Name, castValue);
                        Add(ds.Name, DataSourceType.Strict, table.Name, castValue);
                    }
                }
            }
        }
    }
}
