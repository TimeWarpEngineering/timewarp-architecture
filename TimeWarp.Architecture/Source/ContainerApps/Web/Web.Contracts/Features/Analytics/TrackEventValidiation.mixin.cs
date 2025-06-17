{{
    $isCreate = string.contains moxy.Class.Name "Create";
    $isUpdate = string.contains moxy.Class.Name "Update";
    $isClientSide = string.contains moxy.Class.Name "EditModel";
    $isServerSide = !$isClientSide
}}
namespace TimeWarp.Architecture.Features.Analytics;
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            {{ if !$isCreate }}
            RuleFor(p => p.Id).NotEmpty();
            {{ end }}

            RuleFor(p => p.Name)
                .NotEmpty()
                .MaximumLength(75)
                {{ if !$isServerSide }}
                    {{ if !$isCreate }}
                        .MustAsync(async (name, ct) => 
                            await repository.FirstOrDefaultAsync(new BrandByNameSpec(name), ct) is null)
                            .WithMessage((_, name) => T["Brand {0} already Exists.", name]);
                    {{ else }}
                        .MustAsync(async (brand, name, ct) =>
                                await repository.FirstOrDefaultAsync(new BrandByNameSpec(name), ct)
                                    is not Brand existingBrand || existingBrand.Id == brand.Id)
                            .WithMessage((_, name) => T["Brand {0} already Exists.", name]);                
                    {{ end }}
                {{ end }}
    }
