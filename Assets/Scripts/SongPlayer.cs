﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SongPlayer : MonoBehaviour
{

    #region Private Fields

    [SerializeField]
    private int m_partsToListenTo = 5;

    [SerializeField]
    private float m_partPlaybackTimeSeconds = 5f;

    [SerializeField]
    private float m_partPlaybackFadeTimeSeconds = 0.5f; // RPB: Must be less than half of playbacktime

    [SerializeField]
    private AudioSource m_audioSource;

    private Coroutine m_songPlayCoroutine;

    public bool IsPlaying
    {
        get { return m_audioSource.isPlaying; }
    }

    public float PartPlaybackTimeSeconds
    {
        set { m_partPlaybackTimeSeconds = value; }
    }

    #endregion

    #region Public Methods

    public void PlaySongParts(AudioClip songClip)
    {
        if (m_songPlayCoroutine != null)
        {
            StopPlayback();
        }

        m_songPlayCoroutine = StartCoroutine(PlaySongPartsCoroutine(songClip));
    }

    public void StopPlayback()
    {
        m_audioSource.Pause();
        StopCoroutine(m_songPlayCoroutine);
        AudioClip.Destroy(m_audioSource.clip); // RPB: IMPORTANT! If we don't have this, the app runs out of memory and crashes!!!
    }

    #endregion

    #region Private Methods

    private IEnumerator PlaySongPartsCoroutine(AudioClip songClip)
    {
        var lengthPerPart = songClip.length / m_partsToListenTo;

        m_audioSource.clip = songClip;

        yield return new WaitUntil(() => m_audioSource.clip.loadState == AudioDataLoadState.Loaded);

        while (true)
        {
            for (int i = 0; i < m_partsToListenTo; i++)
            {
                m_audioSource.volume = 0f;
                var playheadTime = i * lengthPerPart;
                m_audioSource.time = playheadTime;
                m_audioSource.Play();

                var crossfadeStartTime = Time.time;

                while (Time.time < crossfadeStartTime + m_partPlaybackFadeTimeSeconds)
                {
                    var progressTime = (Time.time - crossfadeStartTime) / m_partPlaybackFadeTimeSeconds;
                    m_audioSource.volume = Mathf.Lerp(0f, 1f, progressTime);
                    yield return null;
                }

                yield return new WaitForSeconds(m_partPlaybackTimeSeconds - 2 * m_partPlaybackFadeTimeSeconds);

                crossfadeStartTime = Time.time;

                while (Time.time < crossfadeStartTime + m_partPlaybackFadeTimeSeconds)
                {
                    var progressTime = (Time.time - crossfadeStartTime) / m_partPlaybackFadeTimeSeconds;
                    m_audioSource.volume = Mathf.Lerp(1f, 0f, progressTime);
                    yield return null;
                }

                yield return null;
            }
        }
    }

    #endregion

}
