using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using SFB;
using TagLib;
using System;
using System.Linq;
using System.Text;
using UnityEngine.Networking;

public class SongSortInterface : MonoBehaviour
{

    #region Private Classes

    private class FolderHotkeyMappingInternal
    {

        #region Public Fields

        public string AbsoluteFolderPath = "";
        public KeyCode KeyCodeEnum = KeyCode.Joystick8Button9;
        public string UiSoundPath = "";
        public AudioClip UiSoundClip = null; // RPB: has to be set by a monobehavior or something else capable of creating an AudioClip from a .wav file

        #endregion

        #region Public Constructors

        public FolderHotkeyMappingInternal(FolderHotkeyMapping inputMapping, string sourceFolderPath)
        {
            if (inputMapping.PathRelative)
            {
                AbsoluteFolderPath = $"{sourceFolderPath}/{inputMapping.FolderPath}";
            }
            else
            {
                AbsoluteFolderPath = inputMapping.FolderPath;
            }

            KeyCodeEnum = (KeyCode)System.Enum.Parse(typeof(KeyCode), inputMapping.KeyCodeString);

            UiSoundPath = Application.streamingAssetsPath + "/" + inputMapping.UiSoundPath;
        }

        #endregion

    }

    #endregion

    #region Private Fields

    // RPB: Player Configurations

    [SerializeField]
    private AudioSource m_mainSound = null;

    [SerializeField]
    private int m_partsToListenTo = 6;

    [SerializeField]
    private float m_partPlaybackFadeTimeSeconds = 0.5f;


    // RPB: UI

    [SerializeField]
    private AudioSource m_uiSound = null;

    [SerializeField]
    private Text m_nowPlayingText = null;

    [SerializeField]
    private Text m_statusText = null;

    [SerializeField]
    private Text m_progressText = null;

    [SerializeField]
    private Text m_statsText = null;

    [SerializeField]
    private RawImage m_albumArtDisplay = null;

    [SerializeField]
    private Texture m_defaultImage = null;

    [SerializeField]
    private Slider m_timeSlider = null;

    [SerializeField]
    private GameObject m_instructionsLayer = null;

    [SerializeField]
    private GameObject m_statisticsLayer = null;

    [SerializeField]
    private AudioClip m_skipSongSound = null;

    [SerializeField]
    private AudioClip m_defaultUiSound = null;

    // RPB: Components
    private SongPlayer m_songPlayer = null;
    private AudioImporter m_importer = null;
    private ConfigurationReader m_configurationReader = null;

    // RPB: Mappings
    private List<FolderHotkeyMappingInternal> m_keyFolderMappings = new List<FolderHotkeyMappingInternal>();
    private KeyCode m_skipSongKeyCode = KeyCode.DownArrow;
    private KeyCode m_launchPlayerKeyCode = KeyCode.UpArrow;
    private KeyCode m_holdSongKeycode = KeyCode.Space;
    private KeyCode m_undoKeycode = KeyCode.Backspace;

    // RPB: Paths
    private string m_songFolderSourceSongs = null;

    // RPB: State info
    private string m_currentFilePath = null;
    private string m_currentTitle = null;
    private string m_currentArtist = null;
    private bool m_fullPlayOn = false;
    private bool m_isImporting = false;
    private Queue<string> m_songFilesToProcess = new Queue<string>();
    private string m_lastMoveOriginalPath = null;
    private string m_lastMoveNewPath = null;

    // RPB: Helpers
    private StringBuilder m_stringBuilder = new StringBuilder();

    #endregion

    #region Private Methods

    private void Start()
    {
        ReadConfiguration();
        InitializeComponents();
        InitializeNextSong();
    }

    private void InitializeComponents()
    {
        m_songPlayer = gameObject.AddComponent<SongPlayer>();
        m_songPlayer.InitializeSettings(m_mainSound, m_partsToListenTo, m_partPlaybackFadeTimeSeconds, m_timeSlider.value, m_timeSlider.maxValue, m_holdSongKeycode);

        m_importer = gameObject.AddComponent<NAudioImporter>();

        m_timeSlider.onValueChanged.AddListener(delegate { TimeSliderChanged(); });

        m_uiSound.loop = false;

        m_statusText.text = "[ SYSTEM ] Press (F1) for controls";
    }

    private void ReadConfiguration()
    {
        // TODO-RPB: Can this be broken up into more methods?
        m_configurationReader = gameObject.AddComponent<ConfigurationReader>();
        var configuration = m_configurationReader.GetConfiguration();

        if (configuration == null)
        {
            m_statusText.text = "[ ERROR ] Config file not found... falling back to default values";
            Debug.LogWarning("No configuration file found (unusual), so falling back to default values");
            configuration = new SongSortConfiguration();
        }

        // RPB: This is optional. If empty, will fall back to the default.
        if (!string.IsNullOrEmpty(configuration.LaunchSongKeyCodeString))
        {
            m_launchPlayerKeyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), configuration.LaunchSongKeyCodeString);
        }

        // RPB: This is optional. If empty, will fall back to the default.
        if (!string.IsNullOrEmpty(configuration.LaunchSongKeyCodeString))
        {
            m_skipSongKeyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), configuration.SkipSongKeyCodeString);
        }

        // RPB: This is optional. If empty, will fall back to the default.
        if (!string.IsNullOrEmpty(configuration.LaunchSongKeyCodeString))
        {
            m_holdSongKeycode = (KeyCode)System.Enum.Parse(typeof(KeyCode), configuration.HoldSongKeyCodeString);
        }

        // RPB: This is optional. If empty, will fall back to the default.
        if (!string.IsNullOrEmpty(configuration.UndoKeyCodeString))
        {
            m_undoKeycode = (KeyCode)System.Enum.Parse(typeof(KeyCode), configuration.UndoKeyCodeString);
        }

        if (configuration.SkipBrowserDialogOnOpen)
        {
            m_songFolderSourceSongs = configuration.DefaultSourceDirectory;
        }
        else
        {
            var paths = StandaloneFileBrowser.OpenFolderPanel("Select Source Folder", "", false);

            if (paths == null || paths.Length == 0)
            {
                Application.Quit();
            }

            m_songFolderSourceSongs = paths[0];
        }

        if (configuration.FolderHotkeyMapping == null || configuration.FolderHotkeyMapping.Length == 0)
        {
            Debug.LogWarning("FolderHotkeyMapping not found! Falling back to defaults...");

            var approveMapping = new FolderHotkeyMapping("Approved", true, "RightArrow", "ApproveSound.wav");
            var rejectMapping = new FolderHotkeyMapping("Rejected", true, "LeftArrow", "ApproveSound.wav");
            configuration.FolderHotkeyMapping = new FolderHotkeyMapping[] { approveMapping, rejectMapping };
        }

        // TODO-RPB: This needs to be error checked like crazy! I can imagine this being fragile
        for (int i = 0; i < configuration.FolderHotkeyMapping.Length; i++)
        {
            m_keyFolderMappings.Add(new FolderHotkeyMappingInternal(configuration.FolderHotkeyMapping[i], m_songFolderSourceSongs));
        }

        for (int i = 0; i < m_keyFolderMappings.Count; i++)
        {
            Directory.CreateDirectory(m_keyFolderMappings[i].AbsoluteFolderPath);

            StartCoroutine(SetAudioClip(m_keyFolderMappings[i]));
        }

        var allFiles = Directory.GetFiles(m_songFolderSourceSongs);

        if (configuration.RandomizeOrder)
        {
            System.Random rnd = new System.Random();
            allFiles = allFiles.OrderBy(x => rnd.Next()).ToArray();
        }

        m_songFilesToProcess = new Queue<string>();

        for (int i = 0; i < allFiles.Length; i++)
        {
            if (Path.GetExtension(allFiles[i]) == ".mp3")
            {
                m_songFilesToProcess.Enqueue(allFiles[i]);
            }
        }
    }

    private void TimeSliderChanged()
    {
        m_statusText.text = $"[ SYSTEM ] Section Playback Time: {m_timeSlider.value}s";
        m_songPlayer.PartPlaybackTimeSeconds = m_timeSlider.value;
    }

    private void Update()
    {
        for (int i = 0; i < m_keyFolderMappings.Count; i++)
        {
            if (Input.GetKeyDown(m_keyFolderMappings[i].KeyCodeEnum))
            {
                MoveCurrentSong(m_keyFolderMappings[i]);
            }
        }

        if (Input.GetKeyDown(m_launchPlayerKeyCode))
        {
            PlayFullSong();
        }

        if (Input.GetKeyDown(m_skipSongKeyCode))
        {
            HoldSong();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            m_fullPlayOn = !m_fullPlayOn;
            m_songPlayer.FullPlayOn = m_fullPlayOn;

            if (m_fullPlayOn)
            {
                m_statusText.text = "[ SYSTEM ] Playback mode: Full!";
            }
            else
            {
                m_statusText.text = "[ SYSTEM ] Playback mode: Preview!";
            }
        }

        if(Input.GetKeyDown(m_undoKeycode)) // TODO-RPB: Make this settable
        {
            m_statusText.text = $"[ SYSTEM ] UNDO!!!";
            StartCoroutine(TryMoveFile(m_lastMoveNewPath, m_lastMoveOriginalPath));
        }

        m_instructionsLayer.SetActive(Input.GetKey(KeyCode.F1));

        if (Input.GetKeyDown(KeyCode.F3))
        {
            UpdateStatsText();
        }

        m_statisticsLayer.SetActive(Input.GetKey(KeyCode.F3));

        if (m_songPlayer.IsHolding)
        {
            m_statusText.text = "[ SYSTEM ] Holding current playback section!";
        }
    }

    private void UpdateProgressText()
    {
        DirectoryInfo sourceInfo = new DirectoryInfo(m_songFolderSourceSongs);
        var totalSongsInSource = sourceInfo.GetFiles("*.mp3", SearchOption.TopDirectoryOnly).Length;

        int totalSongsInMappedDirectories = 0;

        for (int i = 0; i < m_keyFolderMappings.Count; i++)
        {
            DirectoryInfo folderInfo = new DirectoryInfo(m_keyFolderMappings[i].AbsoluteFolderPath);
            int filesInFolder = folderInfo.GetFiles("*.mp3", SearchOption.TopDirectoryOnly).Length;
            totalSongsInMappedDirectories += filesInFolder;
        }

        var fractionComplete = (float)totalSongsInMappedDirectories / ((float)totalSongsInSource + totalSongsInMappedDirectories);
        m_progressText.text = $"[ {totalSongsInMappedDirectories}/{totalSongsInSource + totalSongsInMappedDirectories} {fractionComplete:0.00%} ]";
    }

    private void UpdateStatsText()
    {
        m_stringBuilder.Clear();
        m_stringBuilder.AppendLine("Statistics:");

        DirectoryInfo sourceInfo = new DirectoryInfo(m_songFolderSourceSongs);
        var totalSongsInSource = sourceInfo.GetFiles("*.mp3", SearchOption.TopDirectoryOnly).Length;

        int totalSongsInMappedDirectories = 0;

        for (int i = 0; i < m_keyFolderMappings.Count; i++)
        {
            DirectoryInfo folderInfo = new DirectoryInfo(m_keyFolderMappings[i].AbsoluteFolderPath);
            int filesInFolder = folderInfo.GetFiles("*.mp3", SearchOption.TopDirectoryOnly).Length;
            totalSongsInMappedDirectories += filesInFolder;
        }

        for (int i = 0; i < m_keyFolderMappings.Count; i++)
        {
            DirectoryInfo folderInfo = new DirectoryInfo(m_keyFolderMappings[i].AbsoluteFolderPath);
            int filesInFolder = folderInfo.GetFiles("*.mp3", SearchOption.TopDirectoryOnly).Length;
            m_stringBuilder.AppendLine($" - {folderInfo.Name}: {filesInFolder} ({(float)filesInFolder / (float)totalSongsInMappedDirectories:0.00%})");
        }

        var fractionComplete = (float)totalSongsInMappedDirectories / ((float)totalSongsInSource + totalSongsInMappedDirectories);
        m_progressText.text = $"[ {totalSongsInMappedDirectories}/{totalSongsInSource + totalSongsInMappedDirectories} {fractionComplete:0.00%} ]";

        m_stringBuilder.AppendLine($" - Remain: {totalSongsInSource}");
        m_stringBuilder.AppendLine($" - Total: {totalSongsInSource + totalSongsInMappedDirectories}");

        m_statsText.text = m_stringBuilder.ToString();
    }

    private void InitializeNextSong()
    {
        // RPB: Get the next song in list path, but check it still exists.
        // RPB: Also make sure to take care of the case where a song doesn't 
        if (m_songFilesToProcess.Count == 0)
        {
            m_statusText.text = "[ SYSTEM ] Finished!";
            return;
        }

        m_currentFilePath = m_songFilesToProcess.Dequeue();

        UpdateProgressText();

        InitializeSongFromPath(m_currentFilePath);
    }

    private void InitializeSongFromPath(string path)
    {
        StartCoroutine(Import(path));
    }

    private IEnumerator Import(string path)
    {
        m_isImporting = true;

        while (m_uiSound.isPlaying)
        {
            yield return null;
        }

        TagLib.File tagFile = TagLib.File.Create(path);

        m_currentArtist = tagFile.Tag.JoinedPerformers;
        m_currentTitle = tagFile.Tag.Title;

        int foundFirstImageIndex = -1;
        for (int i = 0; i < tagFile.Tag.Pictures.Length; i++)
        {
            if (tagFile.Tag.Pictures[i].MimeType == "image/png" || tagFile.Tag.Pictures[i].MimeType == "image/jpeg")
            {
                foundFirstImageIndex = i;
                break;
            }
        }

        if (foundFirstImageIndex != -1)
        {
            var bin = (byte[])(tagFile.Tag.Pictures[foundFirstImageIndex].Data.Data);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bin);
            tex.Apply();

            m_albumArtDisplay.texture = tex;
        }
        else
        {
            m_albumArtDisplay.texture = m_defaultImage;
        }

        if (m_currentTitle != null && m_currentArtist != null)
        {
            m_nowPlayingText.text = $"{m_currentArtist} - {m_currentTitle}";
        }
        else if (m_currentTitle != null && m_currentArtist == null)
        {
            m_nowPlayingText.text = $"{m_currentTitle}";
        }
        else
        {
            m_nowPlayingText.text = $"{Path.GetFileName(m_currentFilePath)}";
        }

        m_importer.Import(path);

        while (!m_importer.isDone)
            yield return null;

        m_isImporting = false;
        m_songPlayer.PlaySong(m_importer.audioClip);
    }

    private void MoveCurrentSong(FolderHotkeyMappingInternal folderMapping)
    {
        if (m_isImporting || !m_songPlayer.IsPlaying)
        {
            m_statusText.text = "[ SYSTEM ] Cannot move song while it is still loading!";
            return;
        }

        m_songPlayer.StopPlayback();
        StartCoroutine(TryMoveFile(m_currentFilePath, folderMapping.AbsoluteFolderPath + "\\" + Path.GetFileName(m_currentFilePath)));
        m_statusText.text = $"[ SYSTEM ] Moved {Path.GetFileName(m_currentFilePath)} to {new DirectoryInfo(folderMapping.AbsoluteFolderPath).Name}";
        Debug.Log($"Moved {Path.GetFileName(m_currentFilePath)} to {new DirectoryInfo(folderMapping.AbsoluteFolderPath).Name}");

        if (folderMapping.UiSoundClip)
        {
            m_uiSound.clip = folderMapping.UiSoundClip;
        }
        else
        {
            m_uiSound.clip = m_defaultUiSound;
        }

        m_uiSound.Play();
        InitializeNextSong();
    }

    private void HoldSong()
    {
        if (m_isImporting || !m_songPlayer.IsPlaying)
        {
            m_statusText.text = "[ SYSTEM ] Cannot skip song while it is still loading!";
        }

        m_songPlayer.StopPlayback();
        m_statusText.text = $"[ SYSTEM ] Skipped {Path.GetFileNameWithoutExtension(m_currentFilePath)}";

        m_uiSound.clip = m_skipSongSound;
        m_uiSound.Play();
        InitializeNextSong();
    }

    private void ReplayPreview()
    {
        m_songPlayer.StopPlayback();
        StartCoroutine(Import(m_currentFilePath));
    }

    private void PlayFullSong()
    {
        m_songPlayer.StopPlayback();
        System.Diagnostics.Process.Start(m_currentFilePath);
    }

    private IEnumerator TryMoveFile(string oldPath, string newPath)
    {
        m_lastMoveOriginalPath = oldPath;
        m_lastMoveNewPath = newPath;
        Debug.Log($"trying to move file {oldPath} to {newPath}");
        bool wasSuccessful = false;

        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(i * 5);

            bool successUnlessError = true;

            try
            {
                System.IO.File.Move(oldPath, newPath);
            }
            catch (Exception e)
            {
                successUnlessError = false;
            }

            if (successUnlessError)
            {
                wasSuccessful = true;

                break;
            }
        }

        if (!wasSuccessful)
        {
            Debug.LogError($"Failed to move {oldPath}");
            m_statusText.text = $"Failed to move {oldPath}";
        }
    }

    private IEnumerator SetAudioClip(FolderHotkeyMappingInternal mapping)
    {
        mapping.UiSoundClip = m_defaultUiSound;

        if (!System.IO.File.Exists(mapping.UiSoundPath))
        {
            Debug.LogError($"Attempted to load nonexistent sound file: {mapping.UiSoundPath}");
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(mapping.UiSoundPath, AudioType.WAV))
        {

            yield return www.Send();

            if (www.isNetworkError)
            {
                Debug.LogError(www.error);
                Debug.LogError($"Attempted to load invalid/corrupt sound file: {mapping.UiSoundPath}");
            }
            else
            {
                mapping.UiSoundClip = DownloadHandlerAudioClip.GetContent(www);
            }
        }
    }

    #endregion

}
