﻿using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Core
{
    public class DataSourceTest : TestBase
    {
        public static BasicData BasicData = new BasicData(s_DataSources.Values);

        public DataSourceTest(ITestOutputHelper output) : base(output)
        {
        }

        [Theory, MemberData(nameof(BasicData))]
        public void TestConnection(string dataSourceName, DataSourceType mode)
        {
            var dataSource = DataSource(dataSourceName, mode);
            try
            {
                dataSource.TestConnection();
            }
            finally
            {
                Release(dataSource);
            }
        }

        [Theory, MemberData(nameof(BasicData))]
        public async Task TestConnectionAsync(string dataSourceName, DataSourceType mode)
        {
            var dataSource = DataSource(dataSourceName, mode);
            try
            {
                await dataSource.TestConnectionAsync();
            }
            finally
            {
                Release(dataSource);
            }
        }
    }
}
