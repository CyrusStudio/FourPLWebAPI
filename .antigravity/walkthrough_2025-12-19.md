# Walkthrough - Staging Table Cleanup Fix

I have addressed the issue where dynamic staging tables were being left behind in the database during SAP Master Data synchronization.

## Changes Made

### 1. Robust Table Cleanup
- Modified `SapMasterDataRepository.cs` to wrap the entire staging table processing logic in a `try...finally` block.
- The `DROP TABLE` command is now located in the `finally` block, ensuring it will always be attempted, regardless of the success or failure of the Bulk Insert or MERGE operations.
- Added an inner `try...catch` around the `DROP TABLE` command itself to prevent cleanup errors from overshadowing the original processing error.

### Production Logging (Fixed)
- **Problem**: IIS relative paths and permissions in `wwwroot`.
- **Solution**: Forced log path to **`C:\FourPLLogs\`**.
- **Action Required**: Manually create `C:\FourPLLogs` on the server and grant `Everyone` or `IIS AppPool\FourPLWebAPI` **Modify** permissions to this drive root folder.
- **Status**: Build completed.

## Verification Results

### Release Build
- **Build Status**: Successful.
- **Output Directory**: `./publish/release`
- **Command**: `dotnet publish src/FourPLWebAPI.csproj -c Release -r win-x64 --self-contained true -o ./publish/release`

### Manual Verification Recommended
1. **Existing Tables Cleanup**:
   - Please run the following SQL script to clean up any leaked tables that were created before this fix:
   ```sql
   DECLARE @sql NVARCHAR(MAX) = '';
   SELECT @sql += 'DROP TABLE [' + SCHEMA_NAME(schema_id) + '].[' + name + '];' + CHAR(13)
   FROM sys.tables
   WHERE name LIKE '%_Staging_2025%'; -- 根據您的命名規則匹配

   IF @sql <> ''
   BEGIN
       PRINT '執行刪除指令:';
       PRINT @sql;
       -- EXEC sp_executesql @sql; -- 確認無誤後再取消註解執行
   END
   ELSE
   BEGIN
       PRINT '找不到可清理的資料表。';
   END
   ```

render_diffs(file:///c:/Lotus/FourPLWebAPI/src/Infrastructure/SAP/SapMasterDataRepository.cs)
