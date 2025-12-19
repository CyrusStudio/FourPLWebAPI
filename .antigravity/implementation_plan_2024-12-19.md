# Hangfire Job Naming and Logging Improvement

This plan aims to resolve the issue where all Hangfire jobs are named "JobExecutor" and logs are not visible in the Hangfire dashboard.

## Proposed Changes

### Infrastructure & Dependencies

#### [MODIFY] [FourPLWebAPI.csproj](file:///c:/Lotus/FourPLWebAPI/src/FourPLWebAPI.csproj)
- Add `Hangfire.Console` (version 1.4.2 or latest)
- Add `Serilog.Sinks.HangfireConsole`

### Configuration

#### [MODIFY] [Program.cs](file:///c:/Lotus/FourPLWebAPI/src/Program.cs)
- Add `.UseConsole()` to Hangfire configuration.
- Add `.WriteTo.HangfireConsole()` to Serilog configuration.

### Job Infrastructure

#### [MODIFY] [JobExecutor.cs](file:///c:/Lotus/FourPLWebAPI/src/jobs/Infrastructure/JobExecutor.cs)
- Add `[JobDisplayName("{0}")]` attribute to `ExecuteAsync` method.
- Update `ExecuteAsync` signature to include `PerformContext context`.
- Ensure the `context` is passed or available for the Serilog sink.

#### [MODIFY] [ScheduleSyncService.cs](file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/Scheduling/ScheduleSyncService.cs)
- Update `RecurringJob.AddOrUpdate` call to match the new `ExecuteAsync` signature (passing `null` or `JobContext.None` as the context parameter, Hangfire will fill it).

## Verification Plan

### Manual Verification
1. **Job Naming**:
   - Start the application.
   - Go to Hangfire Dashboard -> Recurring Jobs.
   - Verify that the "Job" column shows the actual job type name instead of `JobExecutor.ExecuteAsync`.
2. **Job Logging**:
   - Trigger a job manually from the dashboard.
   - Click on the job execution in the "Succeeded" or "Processing" tab.
   - Verify that the "Console" tab exists and displays the logs (e.g., "開始執行 Job...", "Job 執行完成...").
   - Verify that logs from the actual job implementation (e.g., `SapSoSyncJob`) also appear in the console.
