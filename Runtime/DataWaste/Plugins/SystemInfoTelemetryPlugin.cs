using StorkStudios.DataWaste;
using System.Reflection;
using UnityEngine;
using System;

namespace StorkStudios.DataWaste
{
    [CreateAssetMenu(fileName = "SystemInfoTelemetryPlugin", menuName = "StorkStudios/DataWaste/Telemetry plugins/SystemInfoTelemetryPlugin")]
    public class SystemInfoTelemetryPlugin : ScriptableObject, ITelemetryPlugin
    {
        public void OnTelemetryInitialized()
        {
            SendSystemInfoMessage();
        }

        public void OnBeforeTelemetryDestroyed() { }

        public void OnBeforeMessageSent(TelemetryMessage message) { }

        private void SendSystemInfoMessage()
        {
            TelemetryMessage message = new(TelemetryMessageType.SystemInfo);
            Type systemInfoType = typeof(SystemInfo);
            foreach (PropertyInfo info in systemInfoType.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                message.AddProperty(info.Name, info.GetValue(systemInfoType, null).ToString());
            }
            Telemetry.Instance.SendTelemetryMessage(message);
        }
    }
}