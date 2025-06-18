namespace __RootNamespace__.Features.__FeatureName__s
{
    using System;
    public class __FeatureName__Dto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }

        public __FeatureName__Dto(){ }
        public __FeatureName__Dto
        (
            string aDescription,
            string aName,
            decimal aPrice
        )
        {
            Description = aDescription;
            Name = aName;
            Price = aPrice;
        }
    }
}