# TimeWarp Architecture: Blazor Component Naming and Organization

## Introduction

This document outlines the naming conventions and organizational structure for Blazor components within the TimeWarp Architecture. The goal is to maintain clarity, consistency, and reusability across the codebase by categorizing components into specific folders based on their functionality.

## Component Organization

### Components

The `Components` folder contains non-business domain components. These components are reusable across various features and are not tied to any specific business logic.

#### Elements

The `Elements` folder contains atomic UI elements. These are basic building blocks used across the application. TimeWarp Architecture utilizes FluentUI components for the majority of its Elements. This folder will contain custom components that are not available in FluentUI or are specific to the application's design system. 

**Examples:**
- `SimpleAlert.razor`
- `Avatar.razor`

#### Base

The `Base` folder contains base components and abstractions that can be inherited or implemented by other components.

- **Abstractions**: Interfaces and abstract classes or components.
  - `IAttributeComponent.cs`
  - `IParentComponent.razor`
- **Base Components**: Basic components that provide foundational functionality.
  - `DisplayComponent.razor`
  - `ParentComponent.razor`

#### Editors

The `Editors` folder contains components specialized for editing specific value types. Value types are simple or complex data types that are immutable and have no identity. They represent concepts that are decoupled from specific domain logic, allowing reuse across different domains.

**Examples:**
- `DateEditor.razor`
- `NumericEditor.razor`
- `PhoneEditor.razor`
- `StringEditor.razor`
- `AddressEditor.razor`

#### Forms

The `Forms` folder contains components related to forms, such as containers and layout components for forms.

**Examples:**
- `FormContainer.razor`

#### Layouts

The `Layouts` folder contains layout components used to define the structure of different pages in the application.

**Examples:**
- `MainLayout.razor`
- `AltLayout.razor`

#### Composites

The `Composites` folder contains composite components that combine multiple elements or provide more complex functionality.

**Examples:**
- `AuthorizedFluentNavLink.razor`
- `MarkdownSection.razor`
- `TableOfContents.razor`
- `MultiSplitter.razor`
- `Wizard.razor`

#### Pages

The `Pages` folder contains non-domain page components used to layout pages. These components often have many render fragments and are used to implement feature-specific pages.

### Feature Folders

Each feature in the application has its own folder containing domain-related components. These components are specific to the business logic and functionality of the respective feature.

**Examples:**
- `UserManagement/Components/UserProfileComponent.razor`
- `Sales/Components/SalesChart.razor`
- `Inventory/Components/InventoryTable.razor`

## Component Naming Conventions

### Page Components

These components represent full pages in the application. They are the top-level components rendered for a specific route.

**Naming Convention:**
- Suffix: `Page`
- Example: `DashboardPage.razor`

### Form Components

These components encapsulate forms used for user input. They handle data entry and validation.

**Naming Convention:**
- Suffix: `Form`
- Example: `LoginForm.razor`

### Table Components

These components display data in a tabular format. They handle data presentation and interaction within a table.

**Naming Convention:**
- Suffix: `Table`
- Example: `UserTable.razor`

### Chart Components

These components are used to display data visualizations such as charts and graphs.

**Naming Convention:**
- Suffix: `Chart`
- Example: `SalesChart.razor`

### Dialog Components

These components represent modal dialogs used for user interactions that require a response.

**Naming Convention:**
- Suffix: `Dialog`
- Example: `ConfirmDialog.razor`

### Widget Components

These components are small, self-contained, and focused on a specific piece of functionality or UI element. They are often used to create a modular and reusable interface.

**Naming Convention:**
- Suffix: `Widget`
- Example: `WeatherWidget.razor`

## Example Directory Structure

```
Web.Spa/
├── Components/
│   ├── Elements/
│   │   ├── Button.razor
│   │   ├── InputField.razor
│   │   ├── LoadingSpinner.razor
│   ├── Base/
│   │   ├── Abstractions/
│   │   │   ├── IAttributeComponent.cs
│   │   │   ├── IParentComponent.razor
│   │   │   └── ...
│   │   ├── DisplayComponent.razor
│   │   ├── ParentComponent.razor
│   │   └── ...
│   ├── Editors/
│   │   ├── DateEditor.razor
│   │   ├── NumericEditor.razor
│   │   ├── PhoneEditor.razor
│   │   ├── StringEditor.razor
│   │   ├── AddressEditor.razor
│   │   └── ...
│   ├── Forms/
│   │   ├── FormContainer.razor
│   │   └── ...
│   ├── Layouts/
│   │   ├── MainLayout.razor
│   │   ├── AltLayout.razor
│   │   └── ...
│   ├── Composites/
│   │   ├── AuthorizedFluentNavLink.razor
│   │   └── ...
│   ├── Pages/
│   │   ├── TimeWarpPage/
│   │   │   ├── TimeWarpPage.razor
│   │   │   ├── Footer.razor
│   │   │   ├── Header.razor
│   │   │   ├── Navigation.razor
│   │   │   ├── Banner.razor
│   │   │   └── ...
│   │   ├── AlternatePage/
│   │   │   ├── AlternatePage.razor
│   │   │   ├── Footer.razor
│   │   │   ├── Header.razor
│   │   │   ├── Navigation.razor
│   │   │   ├── Banner.razor
│   │   │   └── ...
│   │   └── ...
│   ├── Routes.razor
│   └── ...
├── Features/
│   ├── UserManagement/
│   │   ├── Components/
│   │   │   ├── UserProfileComponent.razor
│   │   │   ├── UserTable.razor
│   │   │   ├── UserForm.razor
│   │   │   └── ...
│   │   └── ...
│   ├── Sales/
│   │   ├── Components/
│   │   │   ├── SalesTable.razor
│   │   │   ├── SalesChart.razor
│   │   │   └── ...
│   │   └── ...
│   ├── Inventory/
│   │   ├── Components/
|   │   │   ├── InventoryTable.razor
│   │   │   ├── InventoryForm.razor
│   │   │   └── ...
│   │   └── ...
│   └── ...
```

## Conclusion

Adhering to these naming conventions and organizational structures will help maintain a clear, consistent, and maintainable codebase. This structure distinguishes between atomic components that can be reused across different parts of the application and domain-specific components that are tied to particular features.
