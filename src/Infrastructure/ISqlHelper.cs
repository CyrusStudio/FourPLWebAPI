using System.Data;

namespace FourPLWebAPI.Infrastructure;

/// <summary>
/// SQL 資料庫操作的通用介面
/// 封裝 Dapper，提供參數化查詢與執行功能
/// </summary>
public interface ISqlHelper
{
    /// <summary>
    /// 執行參數化查詢，回傳強型別結果集合
    /// </summary>
    /// <typeparam name="T">結果類型</typeparam>
    /// <param name="sql">SQL 查詢語句</param>
    /// <param name="param">查詢參數 (可選)</param>
    /// <returns>查詢結果集合</returns>
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);

    /// <summary>
    /// 執行參數化查詢，回傳單一結果或預設值
    /// </summary>
    /// <typeparam name="T">結果類型</typeparam>
    /// <param name="sql">SQL 查詢語句</param>
    /// <param name="param">查詢參數 (可選)</param>
    /// <returns>查詢結果或預設值</returns>
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null);

    /// <summary>
    /// 執行 SQL 命令 (INSERT, UPDATE, DELETE)
    /// </summary>
    /// <param name="sql">SQL 命令</param>
    /// <param name="param">命令參數 (可選)</param>
    /// <returns>受影響的資料列數</returns>
    Task<int> ExecuteAsync(string sql, object? param = null);

    /// <summary>
    /// 執行預存程序並回傳結果集合
    /// </summary>
    /// <typeparam name="T">結果類型</typeparam>
    /// <param name="storedProcedure">預存程序名稱</param>
    /// <param name="param">參數 (可選)</param>
    /// <returns>結果集合</returns>
    Task<IEnumerable<T>> ExecuteStoredProcedureAsync<T>(string storedProcedure, object? param = null);
}
