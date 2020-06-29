# music-sorter
Provides a Tinderesque interface to sorting massive loads of music, quickly.

This app was developed in order to figure out a quick and idiot-proof workflow to sort thousands of songs quickly, spending under 30 seconds per track, with minimal controls. You can use this laying down in bed as long as you can feel your way to the left/right arrow keys!

You select a "Source" folder of tracks (currently, only MP3s are supported).
The app will then preview tracks, one at a time, for you, by playing back short sections of the beginning, middle, and end of the track.

You can press the "Left" arrow key to reject the song (moving it into a folder of rejected tracks)
You can press the "Right" arrow key to accept a song (moving it into a folder of accepted tracks)

There is a configuration file that has several options, and more are planned. You can change keybindings (reference the Unity Keycodes to figure out what to change the KeyCodeString to https://docs.unity3d.com/ScriptReference/KeyCode.html ) and define new folders to sort songs into. You can also add/change the UI sound for your folders!

```
{
  "DefaultSourceDirectory": "testDir",
  "SkipBrowserDialogOnOpen": false,
  "RandomizeOrder": false,
  "FolderHotkeyMapping": [
    {
      "FolderPath": "Approved",
      "KeyCodeString": "RightArrow",
      "PathRelative": true,
      "UiSoundPath": "ApproveSound.wav"
    },
    {
      "FolderPath": "Rejected",
      "KeyCodeString": "LeftArrow",
      "PathRelative": true,
      "UiSoundPath": "RejectSound.wav"
    }
  ],
  "SkipSongKeyCodeString": "DownArrow",
  "LaunchSongKeyCodeString": "UpArrow",
  "HoldSongKeyCodeString": "Space"
}
```



![alt text](https://github.com/ricobalakit/music-sorter/blob/master/graphic1.png?raw=true)

![alt text](https://github.com/ricobalakit/music-sorter/blob/master/graphic2.png?raw=true)
