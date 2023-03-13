namespace TimeWarp.Architecture.Features.TodoItems;

public partial class CreateTodoItemCommandValidator
{
  public CreateTodoItemCommandValidator()
  {
    RuleFor(command => command.Title).NotEmpty();
  }
}
