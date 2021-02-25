namespace __RootspaceName__.Mapper
{
  using AutoMapper;
  using __RootNamespace__.Features.__FeatureName__s;
  using __RootspaceName__.Models;

  public class MappingProfiles : Profile
  {
    public MappingProfiles()
    {
      CreateMap<__FeatureName__CreateRequest, __FeatureName__Entity>(); // means you want to map from User to UserDTO
    }
  }
}