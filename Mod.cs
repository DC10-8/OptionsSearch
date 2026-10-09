using System;
using ColossalFramework;
using ICities;

namespace OptionsSearch
{
    /// <summary>MOD設定(このMOD自身のオプション)。</summary>
    internal static class Config
    {
        public const string FileName = "OptionsSearch";

        static Config()
        {
            if (GameSettings.FindSettingsFileByName(FileName) == null)
            {
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile { fileName = FileName } });
            }
        }

        public static readonly SavedBool IncludeTooltips = new SavedBool("includeTooltips", FileName, true, true);
        public static readonly SavedBool IncludeDropdownItems = new SavedBool("includeDropdownItems", FileName, true, true);
        public static readonly SavedInt MaxResults = new SavedInt("maxResults", FileName, 60, true);
        // 0 = 自動(ゲームの言語), 1 = English, 2 = 日本語
        public static readonly SavedInt Language = new SavedInt("language", FileName, 0, true);
    }

    public class OptionsSearchMod : IUserMod
    {
        public string Name
        {
            get { return "Options Search"; }
        }

        public string Description
        {
            get { return Str.ModDescription; }
        }

        public void OnEnabled()
        {
            OptionsSearchHost.Create();
        }

        public void OnDisabled()
        {
            OptionsSearchHost.Remove();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Options Search");
            group.AddDropdown(Str.SettingLanguage, Str.LanguageChoices, Config.Language.value, delegate (int i) { Config.Language.value = i; });
            group.AddCheckbox(Str.SettingTooltips, Config.IncludeTooltips.value, delegate (bool b) { Config.IncludeTooltips.value = b; });
            group.AddCheckbox(Str.SettingDropdown, Config.IncludeDropdownItems.value, delegate (bool b) { Config.IncludeDropdownItems.value = b; });
            group.AddSlider(Str.SettingMaxResults, 10f, 200f, 10f, Config.MaxResults.value, delegate (float v) { Config.MaxResults.value = (int)v; });
        }
    }
}
