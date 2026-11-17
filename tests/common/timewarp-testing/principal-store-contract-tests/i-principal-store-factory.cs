#region Purpose
// Shared IPrincipalStore contract case group IPrincipalStoreFactory (task 104-032 dual-fixture); inherited by the
// in-memory and EF concrete fixtures so every backend pins the same semantics.
#endregion

namespace PrincipalStoreContract_;

using TimeWarp.Identity;

/// <summary>Factory for an empty, independent <see cref="IPrincipalStore"/> instance.</summary>
public interface IPrincipalStoreFactory
{
  IPrincipalStore CreateStore();
}
