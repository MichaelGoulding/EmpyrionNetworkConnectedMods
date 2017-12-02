using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eleon.Modding;

namespace SharedCode
{
    public interface IGameServerConnection : IDisposable
    {
        event Action<string, Player> Event_ChatMessage;
        event Action<FactionChangeInfo> Event_Faction_Changed;
        event Action<Playfield, Player> Event_Player_ChangedPlayfield;
        event Action<Playfield> Event_Playfield_Loaded;

        void AddBoundingBox(BoundingBox boundingBox);
        void AddVersionString(string versionString);
        void Connect();
        void DebugOutput(string format, params object[] args);
        Dictionary<int, Player> GetOnlinePlayers();
        Playfield GetPlayfield(string playfieldName);
        Task<Structure> GetShipPilotedBy(Player oldPlayerInfo);
        Task RequestEntitySpawn(EntitySpawnInfo entitySpawnInfo);
        Task RequestEntitySpawn(PlayfieldLoad playfieldLoad);
        Task RequestEntitySpawn(string commandString);
        Task SendAlarmMessageToAll(string format, params object[] args);
        Task SendAlertMessageToAll(string format, params object[] args);
        Task SendAttentionMessageToAll(string format, params object[] args);
        void SendChatMessageToAll(string format, params object[] args);
        Task SendMessageToAll(MessagePriority priority, float time, string format, params object[] args);
        Task SendRequest(CmdId cmdID, object data);
        Task<T> SendRequest<T>(CmdId cmdID, object data);
    }
}