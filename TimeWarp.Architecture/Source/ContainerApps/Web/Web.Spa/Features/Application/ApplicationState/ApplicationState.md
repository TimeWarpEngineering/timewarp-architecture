## TimeWarp.Architecture.Features.Applications.ApplicationState

This class represents the state of the application. It is a sealed partial class that inherits from `State<ApplicationState>` and is decorated with the `[StateAccessMixin]` attribute.

### Properties

- **ActiveModalId**: string?
  - Gets or sets the ID of the active modal. This property is nullable.

- **IsMenuExpanded**: bool
  - Gets or sets a value indicating whether the menu is expanded.

- **Logo**: string?
  - Gets or sets the path to the application logo. This property is nullable.

- **Name**: string
  - Gets or sets the name of the application. This property is non-nullable.

- **Version**: string?
  - Gets the version of the assembly containing this class. This is a read-only property.

### Constructor

- **ApplicationState()**
  - Initializes a new instance of the ApplicationState class.

### Methods

- **Initialize(): void**
  - Overrides the base Initialize method.
  - Sets default values for IsMenuExpanded, Name, and Logo.

### Remarks

This class uses the `StateAccessMixin`, which likely provides additional functionality for state management. The `Initialize` method sets up default values for some of the properties, which can be useful for initializing the application state.

The `Version` property is derived from the assembly version, which can be helpful for displaying or logging the current version of the application.
