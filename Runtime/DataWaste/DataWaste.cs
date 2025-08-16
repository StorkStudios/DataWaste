using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StorkStudios.DataWaste
{
    public class DataWaste
    {
        private static DataWaste instance;

        public static DataWaste Instance => instance;

        private readonly DataWasteWorker worker;

        private DataWaste(Uri uri, string playerId, string gameId, float timeout)
        {
            worker = new DataWasteWorker(uri, playerId, gameId, timeout);
        }

        public static void InitInstance(Uri uri, string playerId, string gameId, float timeout)
        {
            instance ??= new DataWaste(uri, playerId, gameId, timeout);
        }

        public Task<string> GetServerStatus()
        {
            return worker.GetServerStatus();
        }

        public Task<string> GetNewestGameVersion()
        {
            return worker.GetData("version");
        }

        public Task<string> GetData(string path)
        {
            return worker.GetData(path);
        }

        public void SendData(IData data)
        {
            worker.SendData(data);
        }

        public void FlushAndFinish()
        {
            //Wait until all data is send
            worker.FlushAndFinish().Wait();
        }
    }
}