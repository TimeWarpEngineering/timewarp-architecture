# Mock Access Token Provider

## Purpose

The primary goal of `MockAccessTokenProvider` is to facilitate development and testing of components that depend on access token functionality, without the need for actual token issuing services. This can be especially useful in the following scenarios:

- **Local Testing:** Enables testing functionality that depends on access tokens without needing live authentication infrastructure.
- **Integration Testing:** Simplifies the setup of integration tests by providing consistent, predictable tokens.
- **Demonstration Purposes:** Allows developers to demonstrate features in environments where the use of real tokens isn't possible (airplane mode).

## How It Works

`MockAccessTokenProvider` implements the `IAccessTokenProvider` interface and provides methods to request an access token. The `GenerateAccessToken` private method creates a dummy token, setting its value and expiration time. This method is used internally to handle both token request methods (`RequestAccessToken` with options and without).

## Usage

To use the `MockAccessTokenProvider` in your application during the development phase, it's essential to configure it as the service responsible for providing access tokens.

### Integration in Application Configuration

1. **Dependency Injection Configuration:**

- In the application startup configuration, register `MockAccessTokenProvider` as the implementation for `IAccessTokenProvider`:

/`
   #if DEBUG
   builder.Services.AddScoped<IAccessTokenProvider, MockAccessTokenProvider>();
   #endif
   /`

This setup ensures that `MockAccessTokenProvider` is used only during development or debugging phases, controlled by the `DEBUG` compilation symbol.
