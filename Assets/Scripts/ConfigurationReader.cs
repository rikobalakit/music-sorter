using System;
using System.IO;
using UnityEngine;

public class ConfigurationReader : MonoBehaviour
{
    public SongSortConfiguration GetConfiguration()
    {
        using (StreamReader r = new StreamReader(Application.streamingAssetsPath + "/config.json"))
        {
            string json = r.ReadToEnd();
            return JsonUtility.FromJson<SongSortConfiguration>(json);
        }
    }
}

[Serializable]
public class SongSortConfiguration
{
    public string DefaultSourceDirectory = "testDir";
    public bool SkipBrowserDialogOnOpen = false;
    public bool RandomizeOrder = false;
    public bool OverrideAcceptDirectory = false;
    public bool OverrideRejectDirectory = false;
    public string OverrideAcceptDirectoryPath = "testDir";
    public string OverrideRejectDirectoryPath = "testDir";
}