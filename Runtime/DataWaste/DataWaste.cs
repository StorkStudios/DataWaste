using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class DataWaste
{
	private static DataWaste instance;

	public static DataWaste Instance => instance;

	private readonly DataWasteWorker worker;

	private DataWaste(Uri uri, string playerId)
	{
		worker = new DataWasteWorker(uri, playerId);
	}

	public static void InitInstance(Uri uri, string playerId)
	{
		instance = new DataWaste(uri, playerId);
	}

	public Task<string> GetServerStatus()
	{
		return worker.GetServerStatus();
	}

	public Task<string> GetNewestGameVersion()
	{
		return worker.GetNewestVersion();
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
