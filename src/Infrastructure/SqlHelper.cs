using System.Data;
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
    private readonly ILogger<SqlHelper> _logger;

    /// <summary>
    /// 建構函式
    /// </summary>
    /// <param name="configuration">設定檔</param>
    /// <param name="logger">日誌記錄器</param>
    public SqlHelper(IConfiguration configuration, ILogger<SqlHelper> logger)
    {
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
}
