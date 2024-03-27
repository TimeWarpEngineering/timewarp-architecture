# CreateCommandMixin

This mixin builds partial declarations for the `Contract` pieces of a `Feature`.
This will ensure that we have consistent naming and structure for our `Features`;
We place it on a DTO class 
## Usage

```csharp
[CreateCommand]
public class TodoItemDto
{}
```

## Generated code
```csharp
namespace TimeWarp.Architecture.Features.TodoItems
{
public partial class CreateTodoItemCommand : BaseRequest, IRequest<CreateTodoItemResponse>{};
public partial class CreateTodoItemResponse : BaseResponse;
public partial class CreateTodoItemCommandValidator: AbstractValidator<CreateTodoItemCommand>{};
}
```
