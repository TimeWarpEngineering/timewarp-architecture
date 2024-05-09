# TimeWarp Architecture: Blazor Component Naming and Organization

## Introduction

This document outlines the naming conventions and organizational structure for Blazor components within the TimeWarp Architecture. The goal is to maintain clarity, consistency, and reusability across the codebase, distinguishing between atomic, non-business domain components and domain-related components.

## Component Organization

### Root Folder: Components

This folder contains atomic, non-business domain components. These components are reusable across various features and are not tied to any specific business logic.

**Examples:**
- `Button.razor`
- `InputField.razor`
- `LoadingSpinner.razor`
- `DateEditor.razor`
- `NumericEditor.razor`
- `PhoneEditor.razor`
- `StringEditor.razor`

### Feature Folders

Each feature in the application has its own folder containing domain-related components. These components are specific to the business logic and functionality of the respective feature.

**Examples:**
- `UserManagement/UserProfileComponent.razor`
- `Sales/SalesChart.razor`
- `Inventory/InventoryTable.razor`

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

### Editor Components

These components are specialized for editing specific types of values. They handle the user input for different data types.

**Naming Convention:**
- Suffix: `Editor`
- Example: `DateEditor.razor`, `NumericEditor.razor`, `PhoneEditor.razor`, `StringEditor.razor`

## Example Directory Structure

\```
src/
├── Components/
│   ├── Button.razor
│   ├── InputField.razor
