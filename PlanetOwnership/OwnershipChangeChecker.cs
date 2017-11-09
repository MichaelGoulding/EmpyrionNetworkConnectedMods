using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace PlanetOwnership
{
    class OwnershipChangeChecker
    {
        public delegate void OwnershipChangeEvent(Faction newOwningFaction, EventArgs e);
        public event OwnershipChangeEvent OwnershipChanged;

        public OwnershipChangeChecker(Faction currentOwners, BoundingBox boundingBox)
        {
            _currentOwners = currentOwners;

            // subscribe for bounding box notifications
            boundingBox.PlayerEnteredArea += OnPlayerEnteredArea;
            boundingBox.PlayerLeftArea += OnPlayerLeftArea;
            _captureTimer.Elapsed += OnCaptureTimerElapsed;
        }

        void OnCaptureTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_capturingFaction != null)
            {
                Console.Out.WriteLine("OwnershipChanged! - old: {0}, new: {1}", _currentOwners, _capturingFaction);
                if (OwnershipChanged != null)
                {
                    OwnershipChanged.Invoke(_capturingFaction, new EventArgs());
                }
                _currentOwners = _capturingFaction;
                _capturingFaction = null;
                _captureTimer.Stop();
            }
        }

        void OnPlayerEnteredArea(Player player, BoundingBox.AreaEventArgs e)
        {
            Console.Out.WriteLine("OwnershipChangeChecker.OnPlayerEnteredArea");
            EvalateIfCapturing(e.AreaBeingWatched.PlayersInArea);
        }

        void OnPlayerLeftArea(Player player, BoundingBox.AreaEventArgs e)
        {
            Console.Out.WriteLine("OwnershipChangeChecker.OnPlayerLeftArea");
            EvalateIfCapturing(e.AreaBeingWatched.PlayersInArea);

            // TODO: tell player who left what happened
            Console.Out.WriteLine("tell player they stopped capturing.");
        }

        void EvalateIfCapturing(List<Player> playersInArea)
        {
            var capturingFactionsPresent = playersInArea.Where(x => x.MemberOfFaction != _currentOwners).Select(x => x.MemberOfFaction).Distinct();

            // if number of players from same faction > 1, and no other players in area.
            if ((capturingFactionsPresent.Count() == 1) && (playersInArea.Count > 0))
            {
                // TODO: tell faction what is happening
                Console.Out.WriteLine("Started capturing.");

                // start capturing
                _capturingFaction = capturingFactionsPresent.Single();
                _captureTimer.Start();
            }
            else if (_capturingFaction != null)
            {
                // TODO: tell everyone what is happening
                Console.Out.WriteLine("Stopped capturing.");

                // stop capturing
                _capturingFaction = null;
                _captureTimer.Stop();
            }
        }

        Faction         _currentOwners;
        Faction         _capturingFaction   = null;
        Timer           _captureTimer       = new Timer(30 * 1000); // TODO: read config for time
    }

    // onPlayerEnteredPlayfield

}
