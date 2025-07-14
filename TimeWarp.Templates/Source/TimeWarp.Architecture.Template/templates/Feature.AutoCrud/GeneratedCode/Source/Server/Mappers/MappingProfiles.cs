namespace __RootNamespace__.Mapper
{
  using AutoMapper;
  using __RootNamespace__.Features.__FeatureName__s;
  using __RootNamespace__.Models;

  public class MappingProfiles : Profile
  {
    public MappingProfiles()
    {
      //Map from UpsertRequest to Entity
      CreateMap<Upsert__FeatureName__Request, __FeatureName__Entity>();
      CreateMap<__FeatureName__Entity, __FeatureName__Dto>();
    }
  }
}