using Sandbox;

namespace ManagedDoom
{
    public sealed class SboxManagedDoomMultiplayerLobbyListener : Sandbox.Component, Sandbox.Component.INetworkListener
    {
        public void OnActive(Sandbox.Connection channel)
        {
            if (channel is null)
            {
                return;
            }

            SboxManagedDoomMultiplayerPanelService.AddHostedPlayer(channel.DisplayName);
            SboxManagedDoomMultiplayerPanelService.SetHostStatus("LOBBY ACTIVE. WAITING FOR PLAYERS TO JOIN.");
            Log.Info($"[MP-UI] Player joined hosted lobby: {channel.DisplayName}");
        }

        public void OnDisconnected(Sandbox.Connection channel)
        {
            if (channel is null)
            {
                return;
            }

            SboxManagedDoomMultiplayerPanelService.RemoveHostedPlayer(channel.DisplayName);
            SboxManagedDoomMultiplayerPanelService.SetHostStatus("LOBBY ACTIVE. WAITING FOR PLAYERS TO JOIN.");
            Log.Info($"[MP-UI] Player left hosted lobby: {channel.DisplayName}");
        }
    }
}
