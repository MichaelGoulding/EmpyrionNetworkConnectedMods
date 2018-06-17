using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eleon.Modding;

namespace EmpyrionModApi
{
    public enum ChatType
    {
        ToAll = 3,
        ToFaction = 5,
        ToPlayer = 8,
        PlayerToServer = 9
    }

    public struct PlayerDeathInfo
    {
        public Player killer;
    }

    public interface IGameServerConnection : IDisposable
    {
        event Action<Player> Event_Player_Connected;
        event Action<Player> Event_Player_Disconnected;
        event Action<ChatType, string, Player> Event_ChatMessage;
        event Action<FactionChangeInfo> Event_Faction_Changed;
        event Action<Playfield, Player> Event_Player_ChangedPlayfield;
        event Action<Player, PlayerDeathInfo> Event_PlayerDied;
        event Action<Playfield> Event_Playfield_Loaded;

        void AddBoundingBox(BoundingBox boundingBox);
        void AddVersionString(string versionString);
        void Connect();
        void DebugOutput(string format, params object[] args);

        Dictionary<int, Player> GetOnlinePlayers();
        Player GetOnlinePlayerByName(string playerName);
        Playfield GetPlayfield(string playfieldName);
        Faction GetFaction(int factionId);
        Task RefreshFactionList();
        Structure GetStructure(int structureId);
        Task RequestEntitySpawn(EntitySpawnInfo entitySpawnInfo);
        Task RequestPlayfieldLoad(PlayfieldLoad playfieldLoad);
        Task RequestConsoleCommand(string commandString);
        Task SendAlarmMessageToAll(string format, params object[] args);
        Task SendAlertMessageToAll(string format, params object[] args);
        Task SendAttentionMessageToAll(string format, params object[] args);
        Task SendChatMessageToAll(string format, params object[] args);
        Task SendMessageToAll(MessagePriority priority, float time, string format, params object[] args);
        Task SendRequest(CmdId cmdID, object data);
        Task<T> SendRequest<T>(CmdId cmdID, object data);
    }
}