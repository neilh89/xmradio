using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMRadio
{
    class XMChan : IComparable
    {
        private int chanNum;
        private string chanName;
        private string artistName;
        private string songTitle;
        private string albumName;

        public XMChan()
        {
            ChanNum = 0;
            ChanName = "";
            ArtistName = "";
            SongTitle = "";
            AlbumName = "";

        }
        public XMChan(int cNum)
        {
            ChanNum = cNum;
            ChanName = "";
            ArtistName = "";
            SongTitle = "";
            AlbumName = "";
        }
        public XMChan(int cNum, String cName, String artName, String sTitle, String albName)
        {
            ChanNum = cNum;
            ChanName = cName;
            ArtistName = artName;
            SongTitle = sTitle;
            AlbumName = albName;
        }
        public int ChanNum
        {
            get
            {
                return chanNum;
            }
            set
            {
                chanNum = value;
            }
        }

        public string ChanName
        {
            get
            {
                return chanName;
            }
            set
            {
                chanName = value;
            }
        }
        public string ArtistName
        {
            get
            {
                return artistName;
            }
            set
            {
                artistName = value;
            }
        }
        public string SongTitle
        {
            get
            {
                return songTitle;
            }
            set
            {
                songTitle = value;
            }
        }
        public string AlbumName
        {
            get
            {
                return albumName;
            }
            set
            {
                albumName = value;
            }
        }

        public int CompareTo(object xc)
        {
            XMChan xmChannel = (XMChan)xc;

            if (ChanNum < xmChannel.ChanNum)
                return -1;
            if (ChanNum > xmChannel.ChanNum)
                return 1;
            else
                return 0;
        }
    }
}
