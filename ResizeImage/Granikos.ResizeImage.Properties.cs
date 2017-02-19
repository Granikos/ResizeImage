// Copyright (c) 2017 Granikos GmbH & Co. KG
// 
using System.Configuration;
using System.Diagnostics;
using System.Drawing;

namespace Granikos.ResizeImage.Properties
{
    internal sealed class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance;

        public static Settings Default
        {
            get
            {
                return Settings.defaultInstance;
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("jpg,jpeg,bmp,png,gif")]
        public string DefaultImageTypes
        {
            get
            {
                return (string)this["DefaultImageTypes"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("100")]
        public int JpegQuality
        {
            get
            {
                return (int)this["JpegQuality"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("96, 96")]
        public Size OutputSize
        {
            get
            {
                return (Size)this["OutputSize"];
            }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("7")]
        public int TopMargin
        {
            get
            {
                return (int)this["TopMargin"];
            }
        }

        static Settings()
        {
            Settings.defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());
        }

        public Settings()
        {
        }
    }
}
