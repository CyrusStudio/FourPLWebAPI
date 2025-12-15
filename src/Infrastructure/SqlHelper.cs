using System.Data;
using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;

namespace FourPLWebAPI.Infrastructure;

/// <summary>
/// SQL Helper 實作
/// 封裝 Dapper，自動處理 Connection 生命週期
/// </summary>
public class SqlHelper : ISqlHelper
{
    private readonly string _connectionString;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlHelper> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="configuration">設定檔</param>
    /// <param name="logger">日誌記錄器</param>
    public SqlHelper(IConfiguration configuration, ILogger<SqlHelper> logger)
    {
        _configuration = configuration;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("未設定 DefaultConnection 連線字串");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        _logger.LogDebug("執行 SQL 查詢: {Sql}", sql);
        
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        return await connection.QueryAsync<T>(sql, param);
    }

    /// <inheritdoc />
    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null)
    {
        _logger.LogDebug("執行 SQL 查詢 (單一結果): {Sql}", sql);
        
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        _logger.LogDebug("執行 SQL 命令: {Sql}", sql);
        
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        return await connection.ExecuteAsync(sql, param);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ExecuteStoredProcedureAsync<T>(string storedProcedure, object? param = null)
    {
        _logger.LogDebug("執行預存程序: {StoredProcedure}", storedProcedure);
        
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        return await connection.QueryAsync<T>(
            storedProcedure, 
            param, 
            commandType: CommandType.StoredProcedure);
    }

    /// <inheritdoc />
    public async Task<int> BulkInsertAsync<T>(string tableName, IEnumerable<T> data, string? connectionStringName = null)
    {
        var connStr = GetConnectionString(connectionStringName);
        var dataList = data.ToList();
        
        if (dataList.Count == 0)
        {
            _logger.LogDebug("BulkInsert 無資料可寫入");
            return 0;
        }

        _logger.LogDebug("執行 Bulk Insert 至 {TableName}，資料筆數: {Count}", tableName, dataList.Count);
        
        // 建立 DataTable
        var dataTable = CreateDataTable(dataList);
        
        await using var connection = new SqlConnection(connStr);
        await connection.OpenAsync();

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = tableName,
            BulkCopyTimeout = 600
        };

        // 設定欄位對應
        foreach (DataColumn column in dataTable.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        try
        {
            await bulkCopy.WriteToServerAsync(dataTable);
            _logger.LogInformation("Bulk Insert 成功，共 {Count} 筆資料", dataList.Count);
            return dataList.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk Insert 失敗: {TableName}", tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> ExecuteWithConnectionAsync(string sql, object? param, string connectionStringName)
    {
        var connStr = GetConnectionString(connectionStringName);
        _logger.LogDebug("執行 SQL 命令 (使用 {ConnectionName}): {Sql}", connectionStringName, sql);
        
        await using var connection = new SqlConnection(connStr);
        await connection.OpenAsync();
        
        return await connection.ExecuteAsync(sql, param);
    }

    /// <summary>
    /// 取得連線字串
    /// </summary>
    private string GetConnectionString(string? connectionStringName)
    {
        if (string.IsNullOrEmpty(connectionStringName))
        {
            return _connectionString;
        }

        return _configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"未設定 {connectionStringName} 連線字串");
    }

    /// <summary>
    /// 將物件集合轉換為 DataTable
    /// </summary>
    private static DataTable CreateDataTable<T>(List<T> data)
    {
        var dataTable = new DataTable();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // 建立欄位
        foreach (var prop in properties)
        {
            var columnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            dataTable.Columns.Add(prop.Name, columnType);
        }

        // 填入資料
        foreach (var item in data)
        {
            var row = dataTable.NewRow();
            foreach (var prop in properties)
            {
                row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
            }
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }
}
