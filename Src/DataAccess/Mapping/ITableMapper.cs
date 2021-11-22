using Azure.Data.Tables;

namespace Mintsafe.DataAccess.Mapping
{
    public interface ITableMapper<T>
    {
        public T FromTableEntity(TableEntity tableEntity);
        public TableEntity ToTableEntity(T t);
    }
}
