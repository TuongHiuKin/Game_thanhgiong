using MCPForUnity.Editor.Services;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class McpAutoReconnect
{
    static McpAutoReconnect()
    {
        EditorApplication.delayCall += ReconnectOnce;
    }

    private static async void ReconnectOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ReconnectOnce;
            return;
        }
        if (!MCPServiceLocator.Server.IsLocalHttpServerReachable())
        {
            MCPServiceLocator.Server.StartLocalHttpServer(quiet: true);
            for (int i = 0; i < 20 && !MCPServiceLocator.Server.IsLocalHttpServerReachable(); i++)
                await Task.Delay(250);
        }

        if (MCPServiceLocator.Bridge.IsRunning) return;

        bool connected = await MCPServiceLocator.Bridge.StartAsync();
        Debug.Log(connected
            ? "[Thanh Giong] MCP for Unity session restored automatically."
            : "[Thanh Giong] MCP auto-reconnect could not start a session; use Window > MCP for Unity.");

        if (!connected)
        {
            await Task.Delay(1500);
            EditorApplication.delayCall += ReconnectOnce;
        }
    }
}
