// MCP直接接続数の上限を無制限に上書きするエディタスクリプト
// Unityアカウントのプランに関係なく、ローカルでMCP接続を許可します
using Unity.AI.MCP.Editor;
using UnityEditor;

[InitializeOnLoad]
static class McpConnectionOverride
{
    static McpConnectionOverride()
    {
        // PolicyChangedを購読して、AcpEntitlementWiringが後から上書きしても再適用する
        UnityMCPBridge.MaxDirectConnectionsPolicyChanged += ForceUnlimited;
        ForceUnlimited();
    }

    static void ForceUnlimited()
    {
        // MaxDirect=-1(無制限)に設定。既に-1なら SetPolicy内でno-opになるので無限ループしない
        UnityMCPBridge.MaxDirectConnectionsResolver = () => -1;
        UnityEngine.Debug.Log("[McpConnectionOverride] MCP直接接続制限を無制限に上書き仕った。");
    }

    [MenuItem("Tools/MCP/Force Unlimited Connections")]
    static void ForceUnlimitedManual()
    {
        ForceUnlimited();
        UnityEngine.Debug.Log("[McpConnectionOverride] 手動で無制限に設定仕った。");
    }
}
