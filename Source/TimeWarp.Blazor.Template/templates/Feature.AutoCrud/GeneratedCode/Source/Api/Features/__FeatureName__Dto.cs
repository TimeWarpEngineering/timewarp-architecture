namespace __RootspaceName__.Features.__FeatureName__s
{
    public class __FeatureName__Dto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        #pragma warning disable CA1056 // Uri properties should not be strings
        public string PictureUri { get; set; }
        #pragma warning restore CA1056 // Uri properties should not be strings

        public __FeatureName__Dto
        (
            string aDescription,
            string aName,
            decimal aPrice,
            string aPictureUri
        )
        {
            Description = aDescription;
            Name = aName;
            Price = aPrice;
            PictureUri = aPictureUri;
        }
    }
}