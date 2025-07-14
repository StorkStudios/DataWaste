using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameVersionData
{
    [JsonProperty]
    public readonly string VersionName;
    [JsonProperty]
    public readonly int VersionIndex;
    [JsonProperty]
    public readonly string DownloadLink;
    [JsonProperty]
    public readonly string AdditionalMessage;
}
