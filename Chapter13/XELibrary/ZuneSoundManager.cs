using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Media;

namespace XELibrary
{
    public class SoundManager
    {
        public bool RepeatPlayList = true;

        private Song[] playList;
        private int currentSong;

        public void Update()
        {
            if (playList.Length > 0) //are we playing a list?
            {
                //check current cue to see if it is playing
                //if not, go to next cue in list
                if (MediaPlayer.State != MediaState.Playing)
                {
                    currentSong++;

                    if (currentSong == playList.Length)
                    {
                        if (RepeatPlayList)
                            currentSong = 0;
                        else
                            return;
                    }

                    if (MediaPlayer.State != MediaState.Playing)
                        MediaPlayer.Play(playList[currentSong]);

                }
            }
        }

        public void StartPlayList(Song[] playList)
        {
            StartPlayList(playList, 0);
        }

        public void StartPlayList(Song[] playList, int startIndex)
        {
            if (playList.Length == 0)
                return;

            this.playList = playList;

            if (startIndex > playList.Length)
                startIndex = 0;

            StartPlayList(startIndex);
        }

        public void StartPlayList(int startIndex)
        {
            if (playList.Length == 0)
                return;

            currentSong = startIndex;
            MediaPlayer.Play(playList[currentSong]);
            MediaPlayer.IsRepeating = false;
        }

        public void StopPlayList()
        {
            MediaPlayer.Stop();
        }
    }
}
