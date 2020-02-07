using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeWarp.Blazor.Server.Mongo
{
    public interface IMongoRepository
    {
        
    }

  public interface IMongoRepository<T> where T : DocumentBase
  {
    Task<Document> CreateItemAsync(T item);
    Task DeleteItemAsync(Guid id);
    Task<T> GetItemAsync(Guid id);
    Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate);
    Task<Document> UpdateItemAsync(T item);
  }
}
