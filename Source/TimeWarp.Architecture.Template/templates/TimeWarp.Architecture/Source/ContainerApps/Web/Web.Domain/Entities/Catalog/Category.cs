//namespace TimeWarp.Architecture.Entities.Catalog;

//using FluentValidation;

///// <summary>
///// Represents a category
///// </summary>
//public partial class Category : BaseEntity, IInvariants //, ILocalizedEntity, ISlugSupported, IAclSupported, IStoreMappingSupported, IDiscountSupported<DiscountCategoryMapping>, ISoftDeletedEntity
//{
//  /// <summary>
//  /// The name of the category
//  /// </summary>
//  public string Name { get; set; }

//  /// <summary>
//  /// The description of the category
//  /// </summary>
//  public string Description { get; set; }

//  /// <summary>
//  /// The category template identifier
//  /// </summary>
//  public int CategoryTemplateId { get; set; }

//  /// <summary>
//  /// The meta keywords
//  /// </summary>
//  public string? MetaKeywords { get; set; }

//  /// <summary>
//  /// The meta description
//  /// </summary>
//  public string MetaDescription { get; set; }

//  /// <summary>
//  /// The meta title
//  /// </summary>
//  public string MetaTitle { get; set; }

//  /// <summary>
//  /// The parent category identifier
//  /// </summary>
//  public int ParentCategoryId { get; set; }

//  /// <summary>
//  /// The picture identifier
//  /// </summary>
//  public int PictureId { get; set; }

//  /// <summary>
//  /// Gets or sets the page size
//  /// </summary>
//  public int PageSize { get; set; }

//  /// <summary>
//  /// Gets or sets a value indicating whether customers can select the page size
//  /// </summary>
//  public bool AllowCustomersToSelectPageSize { get; set; }

//  /// <summary>
//  /// Gets or sets the available customer selectable page size options
//  /// </summary>
//  public string PageSizeOptions { get; set; }

//  /// <summary>
//  /// Gets or sets a value indicating whether to show the category on home page
//  /// </summary>
//  public bool ShowOnHomepage { get; set; }

//  /// <summary>
//  /// Gets or sets a value indicating whether to include this category in the top menu
//  /// </summary>
//  public bool IncludeInTopMenu { get; set; }

//  /// <summary>
//  /// Gets or sets a value indicating whether the entity is subject to ACL
//  /// </summary>
//  public bool SubjectToAcl { get; set; }

//  /// <summary>
//  /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
//  /// </summary>
//  public bool LimitedToStores { get; set; }

//  /// <summary>
//  /// Gets or sets a value indicating whether the entity is published
//  /// </summary>
//  public bool Published { get; set; }

//  /// <summary>
//  /// Gets or sets a value indicating whether the entity has been deleted
//  /// </summary>
//  public bool Deleted { get; set; }

//  /// <summary>
//  /// Gets or sets the display order
//  /// </summary>
//  public int DisplayOrder { get; set; }

//  /// <summary>
//  /// Gets or sets the date and time of instance creation
//  /// </summary>
//  public DateTime CreatedOnUtc { get; set; }

//  /// <summary>
//  /// Gets or sets the date and time of instance update
//  /// </summary>
//  public DateTime UpdatedOnUtc { get; set; }

//  /// <summary>
//  /// Gets or sets a value indicating whether the price range filtering is enabled
//  /// </summary>
//  public bool PriceRangeFiltering { get; set; }

//  /// <summary>
//  /// Gets or sets the "from" price
//  /// </summary>
//  public decimal PriceFrom { get; set; }

//  /// <summary>
//  /// Gets or sets the "to" price
//  /// </summary>
//  public decimal PriceTo { get; set; }

//  /// <summary>
//  /// Gets or sets a value indicating whether the price range should be entered manually
//  /// </summary>
//  public bool ManuallyPriceRange { get; set; }

//  public class Invariants : AbstractValidator<Category>, IInvariants
//  {
//    public Invariants()
//    {
//      RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
//    }
//  }
//}
