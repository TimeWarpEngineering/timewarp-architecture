namespace TimeWarp.Architecture.Features;

/// <summary>
/// Represents a generic response structure for list-based data. This class encapsulates
/// a collection of items of a specified type and the total count of items available
/// in the data source that the list was constructed from.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class ListResponse<T>
{
    /// <summary>
    /// Provides an empty <see cref="ListResponse{T}"/> with a total count of 0 and an empty array of items.
    /// This static member is useful when a method needs to return an empty result without creating a new instance.
    /// </summary>
    public static readonly ListResponse<T> Empty = new();

    /// <summary>
    /// Gets the total number of items available. This count reflects the total number of items that match
    /// the criteria from the data source, not just the number of items in the <see cref="Items"/> array.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets or sets an array of items of type <typeparamref name="T"/>. These items represent a subset of data
    /// typically corresponding to a specific page or request. The array is initialized to be empty, ensuring that
    /// it is never null.
    /// </summary>
    public T[] Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Private constructor used to create the empty instance of the <see cref="ListResponse{T}"/>.
    /// This constructor is not intended to be called directly from external code.
    /// </summary>
    private ListResponse() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListResponse{T}"/> class with the specified total item count
    /// and the array of items.
    /// </summary>
    /// <param name="totalCount">The total number of items available in the data source.</param>
    /// <param name="items">The array of items of type <typeparamref name="T"/>. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided array of items is null.</exception>
    public ListResponse(int totalCount, T[] items)
    {
        TotalCount = totalCount;
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }
}
