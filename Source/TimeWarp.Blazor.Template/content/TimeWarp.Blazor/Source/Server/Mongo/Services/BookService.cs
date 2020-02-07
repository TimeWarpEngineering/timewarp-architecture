namespace BooksApi.Services
{
  using BooksApi.Models;
  using MongoDB.Driver;
  using System.Collections.Generic;
  using System.Linq;
  using TimeWarp.Blazor.Models;

  public class BookService
  {
    private readonly IMongoCollection<Book> _books;

    public BookService(IBookstoreDatabaseSettings aBookstoreDatabaseSettings)
    {
      var client = new MongoClient(aBookstoreDatabaseSettings.ConnectionString);
      IMongoDatabase database = client.GetDatabase(aBookstoreDatabaseSettings.DatabaseName);

      _books = database.GetCollection<Book>(aBookstoreDatabaseSettings.BooksCollectionName);
    }

    public List<Book> Get() =>
        _books.Find(book => true).ToList();

    public Book Get(string id) =>
        _books.Find<Book>(book => book.Id == id).FirstOrDefault();

    public Book Create(Book book)
    {
      _books.InsertOne(book);
      return book;
    }

    public void Update(string id, Book bookIn) =>
        _books.ReplaceOne(book => book.Id == id, bookIn);

    public void Remove(Book bookIn) =>
        _books.DeleteOne(book => book.Id == bookIn.Id);

    public void Remove(string id) =>
        _books.DeleteOne(book => book.Id == id);
  }
}