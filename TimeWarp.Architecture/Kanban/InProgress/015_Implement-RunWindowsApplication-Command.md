# 015_Implement-RunWindowsApplication-Command.md

## Description

Create a Windows-specific RunWindowsApplication command to handle Windows-specific process execution features, using [SupportedOSPlatform] attribute to clearly indicate platform support.

## Requirements

1. Create standalone RunWindowsApplication command with:
   - Window style (Normal, Minimized, etc.)
   - Working directory support
   - Window handle capture
   - After application launch behavior options
   - Timeout settings
2. Implement [SupportedOSPlatform("windows")] attribute
3. Return both ProcessId and WindowHandle in response

## Checklist

### Design
- [ ] Create RunWindowsApplication command model
- [ ] Define Windows-specific enums (WindowStyle, AfterLaunchBehavior)
- [ ] Design response model with WindowHandle support

### Implementation
- [ ] Implement RunWindowsApplication handler
- [ ] Add Windows-specific process configuration
- [ ] Add platform validation
- [ ] Test window handle capture

### Documentation
- [ ] Document Windows-specific features and requirements

## Notes

Proposed structure:
```csharp
[SupportedOSPlatform("windows")]
public static class RunWindowsApplication
{
    public sealed class Command : IRequest<OneOf<Response, ValidationResult, Exception>>
    {
        public string ApplicationPath { get; set; }
        public string? Arguments { get; set; }
        public WindowStyle WindowStyle { get; set; }
        public string? WorkingDirectory { get; set; }
        public TimeSpan? Timeout { get; set; }
        public AfterLaunchBehavior AfterLaunch { get; set; }
    }

    public sealed class Response
    {
        public int ProcessId { get; init; }
        public IntPtr WindowHandle { get; init; }
    }
}
