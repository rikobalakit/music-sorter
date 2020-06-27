using System;
using System.IO;
using UnityEngine;

public class ConfigurationReader : MonoBehaviour
{

    #region Public Methods

    public SongSortConfiguration GetConfiguration()
    {
        var configFilePath = Application.streamingAssetsPath + "/config.json";

        if (!File.Exists(configFilePath))
        {
            Debug.LogError("Could not find the configuration! Falling back to default settings");
            return null;
        }

        using (StreamReader r = new StreamReader(configFilePath))
        {
            string json = r.ReadToEnd();
            return JsonUtility.FromJson<SongSortConfiguration>(json);
        }
    }

    #endregion

}

[Serializable]
public class SongSortConfiguration
{

    #region Public Fields

    public string DefaultSourceDirectory = "testDir";
    public bool SkipBrowserDialogOnOpen = false;
    public bool RandomizeOrder = false;
    public bool OverrideAcceptDirectory = false;
    public bool OverrideRejectDirectory = false;
    public string OverrideAcceptDirectoryPath = "testDir";
    public string OverrideRejectDirectoryPath = "testDir";

    #endregion

}