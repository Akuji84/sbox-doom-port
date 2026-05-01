using Sandbox;

namespace Sandbox
{
    public sealed class SboxManagedDoomMultiplayerServerBootstrap : Component
    {
        private GameObject spawnedSessionObject;

        protected override void OnStart()
        {
            if ( !Networking.IsHost || ManagedDoom.SboxManagedDoomMultiplayerSessionComponent.Current is not null )
            {
                return;
            }

            spawnedSessionObject = new GameObject();
            spawnedSessionObject.Name = "ManagedDoom Multiplayer Session";
            spawnedSessionObject.NetworkMode = NetworkMode.Object;

            var session = spawnedSessionObject.AddComponent<ManagedDoom.SboxManagedDoomMultiplayerSessionComponent>();
            session.LobbyName = "DOOM PORT";
            spawnedSessionObject.NetworkSpawn();
        }

        protected override void OnDestroy()
        {
            if ( spawnedSessionObject is null )
            {
                return;
            }

            try
            {
                spawnedSessionObject.Destroy();
            }
            catch
            {
            }

            spawnedSessionObject = null;
        }
    }
}
