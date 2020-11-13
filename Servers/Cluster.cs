﻿using System.Collections.Generic;
using System.Threading;
using TEKLauncher.Net;
using static System.Threading.ThreadPool;
using static TEKLauncher.Net.ARKdictedData;

namespace TEKLauncher.Servers
{
    internal class Cluster
    {
        internal bool IsPvE;
        internal int PlayersLimit;
        internal string Discord, Hoster, Name;
        internal Dictionary<string, string> Info;
        internal Dictionary<string, Dictionary<ulong, string>> Mods;
        internal Server[] Servers;
        private void RefreshServers(object State)
        {
            foreach (Server Server in Servers)
                Server.Refresh();
        }
        private void RequestArkoudaQuery(object State) => new ArkoudaQuery().Request();
        internal void Refresh()
        {
            foreach (Server Server in Servers)
            {
                Server.IsLoaded = false;
                Server.PlayersOnline = 0;
            }
            QueueUserWorkItem(Name == "Arkouda" ? RequestArkoudaQuery : Name == "ARKdicted" ? (WaitCallback)LoadServers : RefreshServers);
        }
    }
}