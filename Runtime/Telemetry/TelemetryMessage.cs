using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TelemetryMessage : IData
{
    private Dictionary<string, string> data;

    public object Data => data;

    public TelemetryMessage(string type)
    {
        data = new Dictionary<string, string> { { "type", type } };
    }

    public TelemetryMessage AddProperty(string key, string value)
    {
        if (data.ContainsKey(key))
        {
            Log.Warning($"Tried to add duplicate key to telemetry message. Current value: {data[key]}, new value: {value}");
        }
        data[key] = value ?? "";
        return this;
    }

    public TelemetryMessage AddProperty<T>(string key, T value)
    {
        if (data.ContainsKey(key))
        {
            Log.Warning($"Tried to add duplicate key to telemetry message. Current value: {data[key]}, new value: {value}");
        }
        data[key] = value == null ? "" : value.ToString();
        return this;
    }

    public TelemetryMessage AddDictionaryProperty<T>(string key, Dictionary<string, T> dict)
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

    public TelemetryMessage AddEnumerableProperty<T>(string key, IEnumerable<T> enumerable)
    {
        if (enumerable != null)
        {
            AddProperty(key, string.Join(",", enumerable));
        }
        else
        {
            AddProperty(key, "");
        }
        return this;
    }
}
