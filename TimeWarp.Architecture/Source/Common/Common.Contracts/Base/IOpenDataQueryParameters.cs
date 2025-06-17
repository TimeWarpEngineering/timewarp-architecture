namespace TimeWarp.Architecture.Features;

/// <summary>
/// Defines the set of query parameters for handling pagination and filtering in OData requests.
/// This interface allows for basic data querying capabilities such as specifying the number of records to return,
/// skipping a number of records, applying a filter condition, sorting the results, and optionally returning the total count of matching entries.
/// </summary>
public interface IOpenDataQueryParameters
{
  /// <summary>
  /// Gets or sets the number of records to return in the query result. Corresponds to the OData $top query option.
  /// </summary>
  /// <value>The maximum number of records to return.</value>
  int? Top { get; set; }

  /// <summary>
  /// Gets or sets the number of records to skip before returning results. Used for paging through large data sets.
  /// Corresponds to the OData $skip query option.
  /// </summary>
  /// <value>The number of records to skip.</value>
  int? Skip { get; set; }

  /// <summary>
  /// Gets or sets the filter expression used to filter results. Corresponds to the OData $filter query option.
  /// This should be a valid OData filter expression.
  /// </summary>
  /// <value>The filter expression.</value>
  /// <example>
  /// $filter=Price lt 20
  /// $filter=contains(ProductName, 'chair')
  /// </example>
  string? Filter { get; set; }

  /// <summary>
  /// Gets or sets the property or properties to sort the results by, including direction (ascending or descending).
  /// Corresponds to the OData $orderby query option.
  /// </summary>
  /// <value>A string specifying the properties and direction for sorting.</value>
  string? OrderBy { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether the total count of records that match the filter criteria should be returned
  /// along with the data results. Corresponds to the OData $count query option.
  /// </summary>
  /// <value><c>true</c> if the total count should be returned; otherwise, <c>false</c>.</value>
  bool ReturnTotalCount { get; set; }
}
