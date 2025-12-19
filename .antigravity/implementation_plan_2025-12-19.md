# Simplified Production Logging Fix

## User Review Required
> [!TIP]
> To avoid complex IIS permissions within `inetpub/wwwroot`, we are moving the logs to a dedicated root folder: **`C:\FourPLLogs\`**. This is usually much easier to manage and less prone to inheritance issues.

## Proposed Changes

### Configuration

#### [MODIFY] [Program.cs](file:///c:/Lotus/FourPLWebAPI/src/Program.cs)
- Change Serilog File path to `@"C:\FourPLLogs\fourplwebapi-.log"`.

## Verification Plan
1. **Build**: Run `dotnet publish -c Release`.
2. **Setup**: Create the folder `C:\FourPLLogs` on the server.
3. **Verify**: Check that logs appear in `C:\FourPLLogs` after restarting the application.
