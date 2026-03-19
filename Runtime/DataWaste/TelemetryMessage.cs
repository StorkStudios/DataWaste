using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StorkStudios.DataWaste
{
    /// <summary>
    /// Helper class used for structuring the telemetry data.
    /// </summary>
    /// <remarks>
    /// Each message has a type that is a regular field in the message. The type can be used by <see cref="ITelemetryPlugin"/> to modify messages.
    /// Common message types are listed in <see cref="TelemetryMessageType"/>.
    /// </remarks>
    public class TelemetryMessage : IData
    {
        private Dictionary<string, object> data;

        public object Data => data;
        public string MessageType => data["type"] as string;

        public TelemetryMessage(string type)
        {
            data = new Dictionary<string, object> { { "type", type } };
        }

        public TelemetryMessage AddProperty<T>(string key, T value)
        {
            if (data.ContainsKey(key))
            {
                Debug.LogWarning($"Tried to add duplicate key to telemetry message. Current value: {data[key]}, new value: {value}");
            }
            data[key] = value == null ? "" : value;
            return this;
        }

        public TelemetryMessage AddDictionaryUnpacked<T>(string key, Dictionary<string, T> dict)
        {
            if (dict != null && dict.Count != 0)
            {
                foreach (var (k, v) in dict)
                {
                    AddProperty($"{key}.{k}", v);
                }
                return this;
            }
            AddProperty(key, "");
            return this;
        }
    }
}