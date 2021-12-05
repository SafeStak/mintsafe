using Azure.Data.Tables;
using Microsoft.Extensions.Azure;

namespace Mintsafe.DataAccess.Extensions
{
    public static class AzureClientFactoryBuilderExtensions
    {
        public static void AddTableClient(this AzureClientFactoryBuilder factoryBuilder, string connectionString,
            string tableName)
        {
            factoryBuilder.AddClient<TableClient, TableClientOptions>((provider, credential, options) =>
            {
                var tableClient = new TableClient(connectionString, tableName);
                tableClient.CreateIfNotExists();
                return tableClient;
            }).WithName(tableName);
        }
    }
}
