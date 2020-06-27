using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using SFB;
using TagLib;
using System;
using System.Linq;

public class SongSortInterface : MonoBehaviour
{
    [SerializeField]
    private SongPlayer m_songPlayer;

    [SerializeField]
    private AudioImporter m_importer;

    [SerializeField]
    private AudioSource m_approvalSound;

    [SerializeField]
    private AudioSource m_rejectionSound;

    [SerializeField]
    private AudioSource m_skipSound;

    [SerializeField]
    private Text m_nowPlayingText;

    [SerializeField]
    private Text m_statusText;

    [SerializeField]
    private Text m_progressText;

    [SerializeField]
    private RawImage m_albumArtDisplay;

    [SerializeField]
    private Texture m_defaultImage;

    [SerializeField]
    private Slider m_timeSlider;

    [SerializeField]
    private GameObject m_instructionsLayer;

    private string m_currentFilePath;

    private string m_songFolderApprovedSongs;
    private string m_songFolderRejectedSongs;
    private string m_songFolderSourceSongs;

    private string m_currentTitle;
    private string m_currentArtist;

    private bool m_isImporting = false;
    private Queue<string> m_songFiles = new Queue<string>();

    private int m_startingNumberOfSongs = 0;

    private void Start()
    {
        var configurationReader = gameObject.AddComponent<ConfigurationReader>();
        var configuration = configurationReader.GetConfiguration();

        if (configuration.SkipBrowserDialogOnOpen)
        {
            m_songFolderSourceSongs = configuration.DefaultSourceDirectory;
        }
        else
        {
            var paths = StandaloneFileBrowser.OpenFolderPanel("Select Source Folder", "", false);
            m_songFolderSourceSongs = paths[0];
        }

        if (configuration.OverrideAcceptDirectory)
        {
            m_songFolderApprovedSongs = configuration.OverrideAcceptDirectoryPath;
        }
        else
        {
            m_songFolderApprovedSongs = $"{m_songFolderSourceSongs}\\Approved";
        }

        if (configuration.OverrideRejectDirectory)
        {
            m_songFolderRejectedSongs = configuration.OverrideRejectDirectoryPath;
        }
        else
        {
            m_songFolderRejectedSongs = $"{m_songFolderSourceSongs}\\Rejected";
        }

        Directory.CreateDirectory(m_songFolderApprovedSongs);
        Directory.CreateDirectory(m_songFolderRejectedSongs);

        var allFiles = Directory.GetFiles(m_songFolderSourceSongs);

        if (configuration.RandomizeOrder)
        {
            System.Random rnd = new System.Random();
            allFiles = allFiles.OrderBy(x => rnd.Next()).ToArray();
        }

        m_songFiles = new Queue<string>();

        for (int i = 0; i < allFiles.Length; i++)
        {
            if (Path.GetExtension(allFiles[i]) == ".mp3")
            {
                m_songFiles.Enqueue(allFiles[i]);
            }
        }

        m_approvalSound.loop = false;
        m_rejectionSound.loop = false;

        m_startingNumberOfSongs = m_songFiles.Count;

        m_statusText.text = "[ SYSTEM ] Ready! Press (F1) for controls";

        m_timeSlider.onValueChanged.AddListener(delegate { TimeSliderChanged(); });

        InitializeNextSong();
    }

    private void TimeSliderChanged()
    {
        m_statusText.text = $"[ SYSTEM ] Section Playback Time: {m_timeSlider.value}s";
        m_songPlayer.PartPlaybackTimeSeconds = m_timeSlider.value;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            PlayFullSong();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            HoldSong();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            RejectSong();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ApproveSong();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        m_instructionsLayer.SetActive(Input.GetKey(KeyCode.F1));
    }

    private void InitializeSongList()
    {
        // RPB: Get all the files in the Source directory and put it into a list... or queue?
    }

    private void InitializeNextSong()
    {
        // RPB: Get the next song in list path, but check it still exists.
        // RPB: Also make sure to take care of the case where a song doesn't 
        if (m_songFiles.Count == 0)
        {
            m_statusText.text = "[ SYSTEM ] Finished!";
            return;
        }

        m_currentFilePath = m_songFiles.Dequeue();

        m_progressText.text = $"[ {m_startingNumberOfSongs - m_songFiles.Count} / {m_startingNumberOfSongs} ]";

        InitializeSongFromPath(m_currentFilePath);
    }

    private void InitializeSongFromPath(string path)
    {
        StartCoroutine(Import(path));
    }

    private IEnumerator Import(string path)
    {
        m_isImporting = true;

        while (m_approvalSound.isPlaying || m_rejectionSound.isPlaying || m_skipSound.isPlaying)
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
        m_songPlayer.PlaySongParts(m_importer.audioClip);
    }

    private void ApproveSong()
    {
        if (m_isImporting || !m_songPlayer.IsPlaying)
        {
            m_statusText.text = "[ SYSTEM ] Cannot approve song while it is still loading!";
        }

        m_songPlayer.StopPlayback();
        // RPB: Move file to Approve folder

        StartCoroutine(TryMoveFile(m_currentFilePath, m_songFolderApprovedSongs + "\\" + Path.GetFileName(m_currentFilePath)));

        m_statusText.text = $"[ SYSTEM ] Accepted {Path.GetFileNameWithoutExtension(m_currentFilePath)}";
        // RPB: Play an approval sound
        m_approvalSound.Play();
        InitializeNextSong();
    }

    private void RejectSong()
    {
        if (m_isImporting || !m_songPlayer.IsPlaying)
        {
            m_statusText.text = "[ SYSTEM ] Cannot reject song while it is still loading!";
        }

        m_songPlayer.StopPlayback();
        // RPB: Move file to Reject folder
        m_statusText.text = $"[ SYSTEM ] Rejected {Path.GetFileNameWithoutExtension(m_currentFilePath)}";
        // RPB: Play a rejection sound
        StartCoroutine(TryMoveFile(m_currentFilePath, m_songFolderRejectedSongs + "\\" + Path.GetFileName(m_currentFilePath)));

        m_rejectionSound.Play();
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
        // RPB: Play an approval sound
        m_skipSound.Play();
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
}
