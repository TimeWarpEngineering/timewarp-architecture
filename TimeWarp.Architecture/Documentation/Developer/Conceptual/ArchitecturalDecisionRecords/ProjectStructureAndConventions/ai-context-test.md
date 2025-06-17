# AI Context Test

## Prompt

```
Given the following context please answer

1. If I wanted you to create a Query to get all Users. What should the file and path be?
2. Can you show me what the content of the class would be?
```

```
put ai-context.yaml contents here.
```

# Expected result

Features/User/Queries/GetAllUsers

```
namespace Web.Contracts.Features.User
{
    public static partial class GetAllUsers
    {
        public class Query : IRequest<OneOf<SuccessResponse, ProblemDetails>>
        {
        }

        public class SuccessResponse
        {
            public IEnumerable<UserDto> Users { get; set; }
        }

        public class UserDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            // Add more properties as required
        }

        public class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                // Add validation rules if required
            }
        }
    }
}
```