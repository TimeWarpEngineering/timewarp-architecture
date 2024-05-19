# Mock Authentication State Provider

## Overview

`MockAuthenticationStateProvider` is a custom implementation of `AuthenticationStateProvider` designed to facilitate development and testing in environments where actual authentication services, such as Azure AD B2C, are not available or practical. This provider simulates an authenticated user state, allowing developers to test components and behaviors that require user authentication without the need to connect to external authentication services.

## Purpose

The primary purpose of this class is to provide a seamless way to develop and test authentication-dependent features in the application without the overhead or dependencies of actual authentication mechanisms. It is especially useful during:

- **Offline Development:** When developing without an internet connection or access to Azure AD B2C services.
- **Service Outages:** When external authentication services are down.
- **Rapid Prototyping:** When quickly iterating over UI components and business logic that depend on user roles and permissions.

## How It Works

`MockAuthenticationStateProvider` overrides the `GetAuthenticationStateAsync` method to return a predefined `AuthenticationState` object that includes a `ClaimsPrincipal` with predefined claims. These claims can be adjusted as necessary to reflect different testing scenarios.

## Usage

To integrate the `MockAuthenticationStateProvider` in the development build of the application, ensure the compilation symbol `MOCK_AUTHENTICATION` is defined in your build configurations. This is controlled via conditional compilation flags within the project settings or directly in the `.csproj` file.

### Enabling Mock Authentication

1. **Visual Studio Configuration:**
  - Right-click on the project in Solution Explorer and select **Properties**.
  - Navigate to the **Build** tab.
  - In the **Conditional compilation symbols** field, add `MOCK_AUTHENTICATION`.

2. **.csproj File Configuration:**
  - Edit your `.csproj` file to include the following conditional property group:
    ```xml
    <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
      <DefineConstants>$(DefineConstants);MOCK_AUTHENTICATION</DefineConstants>
    </PropertyGroup>
    ```

### Sample Configuration in `Program.cs`

```csharp
#if MOCK_AUTHENTICATION
builder.Services.AddScoped<AuthenticationStateProvider, MockAuthenticationStateProvider>();
#endif
```
