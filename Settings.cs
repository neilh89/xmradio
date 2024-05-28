using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMRadio
{
    [Serializable]
    class Settings
    {
        private string username;
        private string password;
        private string colorSetting;
        private string textColorSetting;

        public string User
        {
            get
            {
                return username;
            }
            set
            {
                username = value;
            }
        }

        public string Pass
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }

        public string Color
        {
            get
            {
                return colorSetting;
            }
            set
            {
                colorSetting = value;
            }
        }

        public string TextColor
        {
            get
            {
                return textColorSetting;
            }
            set
            {
                textColorSetting = value;
            }
        }
    }
}
