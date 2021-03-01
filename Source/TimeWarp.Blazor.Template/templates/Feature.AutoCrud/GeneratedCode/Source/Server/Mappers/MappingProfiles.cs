namespace __RootNamespace__.Mapper
{
  using AutoMapper;
  using __RootNamespace__.Features.__FeatureName__s;
  using __RootNamespace__.Models;

  public class MappingProfiles : Profile
  {
    public MappingProfiles()
    {
      CreateMap<__FeatureName__CreateRequest, __FeatureName__Entity>(); // means you want to map from User to UserDTO
      CreateMap<__FeatureName__Entity, __FeatureName__Dto>();
    }
  }
}