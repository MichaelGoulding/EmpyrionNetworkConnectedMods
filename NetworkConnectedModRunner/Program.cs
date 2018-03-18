using EmpyrionModApi;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Runtime.InteropServices;
using System.Threading;

namespace NetworkConnectedModRunner
{
    class Program
    {
        private static readonly Mutex Mutex = new Mutex(false, "NetworkConnectedModRunner");

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler exitHandler;

        static ManualResetEvent run = new ManualResetEvent(true);
        private bool _exiting;

        private static System.Diagnostics.TraceSource _traceSource =
                new System.Diagnostics.TraceSource("NetworkConnectedModRunner");

        CompositionContainer _container;

        IGameServerConnection _gameServerConnection;

        #pragma warning disable 0649
        [ImportMany]
        IEnumerable<IGameMod> _gameMods;
        #pragma warning restore 0649

        public void Run()
        {
            var configFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "Settings.yaml";
            var config = Configuration.GetConfiguration<Configuration>(configFilePath);

            var modPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Extensions";

            exitHandler += new EventHandler(ExitHandler);
            SetConsoleCtrlHandler(exitHandler, true);

            using (var catalog = new AggregateCatalog(
                new AssemblyCatalog(typeof(Program).Assembly),
                new DirectoryCatalog(modPath, "*Mod.dll")))
            {
                // iterate over all directories in .\Plugins dir and add all Plugin* dirs to catalogs
                foreach (var path in System.IO.Directory.EnumerateDirectories(modPath, "*", System.IO.SearchOption.TopDirectoryOnly))
                {
                    catalog.Catalogs.Add(new DirectoryCatalog(path, "*Mod.dll"));
                }

                _container = new CompositionContainer(catalog);

                try
                {
                    this._container.ComposeParts(this);

                    using (_gameServerConnection = new GameServerConnection(config))
                    {
                        foreach (var gameMod in _gameMods)
                        {
                            gameMod.Start(_gameServerConnection);
                        }

                        _gameServerConnection.Connect();

                        while (!_exiting)
                        {
                            Thread.Sleep(500);
                        }

                    }
                }
                catch (CompositionException compositionException)
                {
                    _traceSource.TraceEvent(System.Diagnostics.TraceEventType.Error, 1, compositionException.ToString());
                }
            }
        }

        static void Main(string[] args)
        {
            if (!Mutex.WaitOne(TimeSpan.FromSeconds(2), false)) // singleton application already started
            {
                Console.WriteLine("Another instance of NetworkConnectedModRunner is already running... Press ENTER to exit");
                Console.ReadLine();
                return;
            }

            var prog = new Program();

            prog.Run();

            _traceSource.Flush();
            _traceSource.Close();
        }

        private bool ExitHandler(CtrlType sig)
        {
            ExitGameMods();
            Console.WriteLine("Shutting down: " + sig.ToString());
            run.Reset();
            Thread.Sleep(2000);
            return false; // If the function handles the control signal, it should return TRUE. If it returns FALSE, the next handler function in the list of handlers for this process is used (from MSDN).

        }

        private void ExitGameMods()
        {
            Console.WriteLine("Gracefully shutting down mods...");
            foreach (var gameMod in _gameMods)
            {
                gameMod.Stop();
            }
            Console.WriteLine("All mods have been shut down");
            _exiting = true;
        }

    }
}
