using System;
using System.Collections.Generic;
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
    public FolderHotkeyMapping[] FolderHotkeyMapping = null;
    public string SkipSongKeyCodeString = "DownArrow";
    public string LaunchSongKeyCodeString = "UpArrow";
    public string HoldSongKeyCodeString = "Space";
    public string UndoKeyCodeString = "Backspace";

    #endregion

}

[Serializable]
public class FolderHotkeyMapping
{

    #region Public Fields

    public string FolderPath = "testDir";
    public string KeyCodeString = "";
    public bool PathRelative = true;
    public string UiSoundPath = "testFile";

    #endregion

    #region Public Constructors

    public FolderHotkeyMapping(string folderPath, bool pathRelative, string keyCodeString, string uiSoundPath)
    {
        FolderPath = folderPath;
        PathRelative = pathRelative;
        KeyCodeString = keyCodeString;
        UiSoundPath = uiSoundPath;
    }

    #endregion
}