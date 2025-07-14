using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public class Telemetry : Singleton<Telemetry>
{
    private enum ServerStatus
    {
        Unknown,
        Online,
        Offline
    }

    [Header("Config")]
    [SerializeField]
    [Tooltip("This should be disabled during development, to limit load on the telemetry server")]
    private bool enableTelemetry = false;
    [SerializeField]
    private string telemetryServer = "";

    [Header("Debug")]
    [SerializeField]
    private bool debugInfo = false;

    [SerializeField]
    [ReadOnly]
    private ServerStatus serverStatus = ServerStatus.Unknown;

    protected override void Awake()
    {
        if (!enableTelemetry)
        {
            return;
        }

        string playerId = GetPlayerId();
#if UNITY_EDITOR
        Log.Warning("Telemetry is enabled - it should be only enabled in production builds!");
        playerId = "editor";
#else
        Log.Debug($"Telemetry running. PlayerId: {playerId}");
#endif
        DataWaste.InitInstance(new Uri(telemetryServer), playerId);

        Init();

        base.Awake();
    }

    private void Start()
    {
        if (!enableTelemetry)
        {
            return;
        }
        SendAppliactionStartMessage();
        SendSystemInfoMessage();
    }

    private void OnApplicationQuit()
    {
        if (!enableTelemetry || serverStatus == ServerStatus.Offline)
        {
            return;
        }

        SendTelemetryMessage(new TelemetryMessage("applicationQuit"));
        DataWaste.Instance.FlushAndFinish();
    }

    public void SendTelemetryMessage(TelemetryMessage message)
    {
        //In a short window after startup we are sending messages to the server with unknown status
        //If server is running it will get the message, if not we will handle the error in DataWaste
        //When server status is determined as offline, we stop
        if (!enableTelemetry || serverStatus == ServerStatus.Offline)
        {
            return;
        }
        DataWaste.Instance.SendData(message);
    }

    public Task<string> GetServerStatus()
    {
        return DataWaste.Instance.GetServerStatus();
    }

    private void Init()
    {
        Task<string> task = DataWaste.Instance.GetServerStatus();
        task.ContinueWith(t =>
        {
            if (t.Result == "OK")
            {
                serverStatus = ServerStatus.Online;
                if (debugInfo)
                {
                    Log.Info("Telemetry server is running");
                }

                CheckNewGameVersion();
            }
            else
            {
                serverStatus = ServerStatus.Offline;
                if (debugInfo)
                {
                    Log.Warning("Telemetry server is unavailable");
                }
            }
        });
    }

    private void CheckNewGameVersion()
    {
        Task<string> task = DataWaste.Instance.GetNewestGameVersion();
        task.ContinueWith(t =>
        {
            GameVersionData gameVersion = JsonConvert.DeserializeObject<GameVersionData>(t.Result);
            if (gameVersion.VersionIndex > GameVersion.Instance.VersionIndex)
            {
                Log.Info($"New game version is available - {gameVersion.VersionName}");
                GameVersion.Instance.NewestAvailableVersion = gameVersion;
            }
        });
    }

    private void SendAppliactionStartMessage()
    {
        TelemetryMessage message = new TelemetryMessage("applicationStart");
        message.AddProperty("version", GameVersion.Instance.VersionText);
        SendTelemetryMessage(message);
    }

    private void SendSystemInfoMessage()
    {
        TelemetryMessage message = new TelemetryMessage("systemInfo");
        Type systemInfoType = typeof(SystemInfo);
        foreach (PropertyInfo info in systemInfoType.GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            message.AddProperty(info.Name, info.GetValue(systemInfoType, null).ToString());
        }
        SendTelemetryMessage(message);
    }

    private string GetPlayerId()
    {
        if (PlayerPrefs.HasKey("playerId"))
        {
            return PlayerPrefs.GetString("playerId");
        }
        else
        {
            string id = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("playerId", id);
            return id;
        }
    }
}
