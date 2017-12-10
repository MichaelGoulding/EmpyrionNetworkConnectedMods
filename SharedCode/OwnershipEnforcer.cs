using System;
using System.Collections.Generic;
using System.Timers;
using EmpyrionModApi;

namespace EmpyrionModApi
{
    class OwnershipEnforcer
    {
        public OwnershipEnforcer(Faction currentOwners, BoundingBox boundingBox)
        {
            _currentOwners = currentOwners;

            boundingBox.PlayerEnteredArea += OnPlayerEnteredArea;
            boundingBox.PlayerLeftArea += OnPlayerLeftArea;
            
        }

        public void OnCurrentOwnersChanged(Faction newCurrentOwners, BoundingBox boundingBox)
        {
            System.Diagnostics.Debug.Assert(_currentOwners != newCurrentOwners);

            foreach( Player player in boundingBox.PlayersInArea)
            {
                bool belongsHere = (player.MemberOfFaction == _currentOwners);
                if (belongsHere)
                {
                    StopTimerForPlayer(player);
                }
                else if(!_punishTimers.ContainsKey(player))
                {
                    StartTimerForPlayer(player);
                }
            }
        }

        private void OnPunishTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // TODO: destory ship?
            throw new NotImplementedException();
        }

        private void OnPlayerEnteredArea(Player player, BoundingBox.AreaEventArgs e)
        {
            if( player.MemberOfFaction != _currentOwners)
            {
                StartTimerForPlayer(player);
            }
        }

        private void OnPlayerLeftArea(Player player, BoundingBox.AreaEventArgs e)
        {
            StopTimerForPlayer(player);
        }

        private void StartTimerForPlayer(Player player)
        {
            var punishTimer = new Timer(30 * 1000); // TODO: read config for time
            punishTimer.Elapsed += OnPunishTimerElapsed;
            _punishTimers[player] = punishTimer;
            // TODO: warn player
        }

        private void StopTimerForPlayer(Player player)
        {
            if (_punishTimers.TryGetValue(player, out Timer timer))
            {
                timer.Stop();
                _punishTimers.Remove(player);
            }
        }

        Faction                     _currentOwners;
        Dictionary<Player, Timer>   _punishTimers = new Dictionary<Player, System.Timers.Timer>();
    }

    // onPlayerEnteredPlayfield

}
