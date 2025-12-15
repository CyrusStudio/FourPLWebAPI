using SAP.Middleware.Connector;

namespace FourPLWebAPI.Infrastructure;

/// <summary>
/// SAP Destination Configuration for NCo 3.1
/// 支援透過 SapEnvironment 設定切換 DEV/QAS/PRD
/// </summary>
public class SapDestinationConfig(IConfiguration configuration) : IDestinationConfiguration
{
    // 介面要求的事件，但因 ChangeEventsSupported() 返回 false 所以不會使用
#pragma warning disable CS0067
    public event RfcDestinationManager.ConfigurationChangeHandler? ConfigurationChanged;
#pragma warning restore CS0067

    public RfcConfigParameters GetParameters(string destinationName)
    {
        var parameters = new RfcConfigParameters();

        // 只處理我們定義的 Destination
        if (destinationName == "FourPL_SAP")
        {
            // 取得目標環境 (DEV/QAS/PRD)
            var sapEnv = configuration.GetValue<string>("SapEnvironment") ?? "DEV";

            // 從 Sap:{環境} 區段讀取設定
            var sapSection = configuration.GetSection($"Sap:{sapEnv}");

            // 若找不到該環境設定，嘗試使用舊格式 (直接從 Sap 讀取)
            if (!sapSection.Exists())
            {
                sapSection = configuration.GetSection("Sap");
            }

            parameters[RfcConfigParameters.AppServerHost] = sapSection["AppServerHost"];
            parameters[RfcConfigParameters.SystemNumber] = sapSection["SystemNumber"];
            parameters[RfcConfigParameters.SystemID] = sapSection["SystemId"];
            parameters[RfcConfigParameters.User] = sapSection["User"];
            parameters[RfcConfigParameters.Password] = sapSection["Password"];
            parameters[RfcConfigParameters.Client] = sapSection["Client"];
            parameters[RfcConfigParameters.Language] = sapSection["Language"];

            // Connection Pool 設定
            parameters[RfcConfigParameters.PoolSize] = "5";
        }

        return parameters;
    }

    public bool ChangeEventsSupported()
    {
        return false;
    }
}
