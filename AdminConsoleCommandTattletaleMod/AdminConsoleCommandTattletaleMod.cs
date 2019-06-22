using EmpyrionModApi;
using EmpyrionModApi.ExtensionMethods;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AdminConsoleCommandTattletaleMod
{
    [System.ComponentModel.Composition.Export(typeof(IGameMod))]
    public class AdminConsoleCommandTattletaleMod : IGameMod
    {
        static readonly string k_versionString = EmpyrionModApi.Helpers.GetVersionString(typeof(AdminConsoleCommandTattletaleMod));

        private TraceSource _traceSource = new TraceSource("AdminConsoleCommandTattletaleMod");

        public void Start(IGameServerConnection gameServerConnection)
        {
            _traceSource.TraceInformation("Starting up...");

            _gameServerConnection = gameServerConnection;

            _gameServerConnection.AddVersionString(k_versionString);
            _gameServerConnection.Event_ConsoleCommand += OnEvent_ConsoleCommand;
        }

        public void Stop()
        {
            _traceSource.TraceInformation("Stopping...");
            _traceSource.Flush();
            _traceSource.Close();
        }

        private async void OnEvent_ConsoleCommand(Player player, string command, bool allowed)
        {
            if (player.IsPrivileged)
            {
                await _gameServerConnection.SendChatMessageToAll("{0} used Console command: {1}. Admin Status = {2}.", player, command, allowed);
            }
        }

        private IGameServerConnection _gameServerConnection;
    }
}
