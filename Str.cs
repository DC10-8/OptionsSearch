using System;
using ColossalFramework.Globalization;

namespace OptionsSearch
{
    /// <summary>
    /// UI文字列。英語版/日本語版を用意し、設定(Language)で
    /// 0=自動(ゲームの言語に追従) / 1=English / 2=日本語 を切り替える。
    /// </summary>
    internal static class Str
    {
        public const int LangAuto = 0;
        public const int LangEnglish = 1;
        public const int LangJapanese = 2;

        /// <summary>現在日本語表示か。</summary>
        public static bool IsJa()
        {
            int setting = Config.Language.value;
            if (setting == LangEnglish) return false;
            if (setting == LangJapanese) return true;
            try
            {
                return LocaleManager.cultureInfo.TwoLetterISOLanguageName == "ja";
            }
            catch (Exception)
            {
                return false;
            }
        }

        static string T(string en, string ja)
        {
            return IsJa() ? ja : en;
        }

        // ---- MOD情報 ----
        public static string ModDescription
        {
            get
            {
                return T("Adds a search box to the Options screen that searches the base game and every mod's settings.",
                         "オプション画面に検索ボックスを追加し、ゲーム本体と全MODの設定項目を横断検索します。");
            }
        }

        // ---- 検索UI ----
        public static string Placeholder
        {
            get { return T("Search options (Ctrl+F)", "オプションを検索 (Ctrl+F)"); }
        }

        public static string NoResults
        {
            get { return T("No matching options", "該当する項目がありません"); }
        }

        public static string Results(int shown, int total)
        {
            if (IsJa())
            {
                return shown < total ? (total + " 件中 " + shown + " 件を表示") : (total + " 件");
            }
            return shown < total
                ? ("Showing " + shown + " of " + total)
                : (total + " result" + (total == 1 ? "" : "s"));
        }

        public static string PageTag
        {
            get { return T("[Mod/Category]", "[MOD/カテゴリ]"); }
        }

        // ---- このMOD自身の設定画面 ----
        public static string SettingLanguage
        {
            get { return T("Language", "言語 / Language"); }
        }

        public static string[] LanguageChoices
        {
            get
            {
                return new string[]
                {
                    T("Auto (follow game language)", "自動 (ゲームの言語に合わせる)"),
                    "English",
                    "日本語"
                };
            }
        }

        public static string SettingTooltips
        {
            get { return T("Search tooltips too", "ツールチップも検索対象にする"); }
        }

        public static string SettingDropdown
        {
            get { return T("Search dropdown choices too", "ドロップダウンの選択肢も検索対象にする"); }
        }

        public static string SettingMaxResults
        {
            get { return T("Max results", "最大表示件数"); }
        }
    }
}
