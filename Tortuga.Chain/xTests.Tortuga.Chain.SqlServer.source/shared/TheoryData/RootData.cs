using System.Collections.Generic;
using Tortuga.Chain.DataSources;
using Xunit;

namespace Tests
{
    public class RootData : TheoryData<string>
    {
        public RootData(params DataSource[] dataSources)
        {
            foreach (var ds in dataSources)
            {
                Add(ds.Name);
            }
        }

        public RootData(IEnumerable<DataSource> dataSources)
        {
            foreach (var ds in dataSources)
            {
                Add(ds.Name);
            }
        }
    }
}
