using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace OptionsSearch
{
    /// <summary>
    /// オプション画面(OptionsPanel)を監視し、開かれるたびに検索UIの取り付けとインデックス再構築を行う。
    /// メインメニュー⇔ゲーム内でパネルが作り直されても自動で付け直す。
    /// </summary>
    internal sealed class OptionsSearchHost : MonoBehaviour
    {
        const string PanelName = "OptionsPanel";
        const float DebounceSec = 0.15f;

        static GameObject _go;

        UIComponent _panel;
        SearchUI _ui;
        List<Entry> _index;
        bool _wasVisible;
        bool _lastJa;
        bool _indexDirty = true;
        bool _searchPending;
        float _searchAt;
        float _nextFind;

        public static void Create()
        {
            if (_go != null) return;
            _go = new GameObject("OptionsSearchHost");
            _go.AddComponent<OptionsSearchHost>();
            DontDestroyOnLoad(_go);
        }

        public static void Remove()
        {
            if (_go != null)
            {
                Destroy(_go);
                _go = null;
            }
        }

        void OnDestroy()
        {
            if (_ui != null) _ui.Destroy();
            _ui = null;
        }

        void Update()
        {
            // パネルが破棄された(シーン遷移など) → 取り直す
            if (_panel == null)
            {
                if (_ui != null) { _ui = null; }
                _index = null;
                _wasVisible = false;
                if (Time.realtimeSinceStartup < _nextFind) return;
                _nextFind = Time.realtimeSinceStartup + 0.5f;
                _panel = FindOptionsPanel();
                if (_panel == null) return;
                Debug.Log("[OptionsSearch] found options panel: " + _panel.name + " size=" + _panel.size + " visible=" + _panel.isVisible);
            }

            if (_ui == null || !_ui.IsAlive)
            {
                try
                {
                    _ui = new SearchUI(_panel, OnTextChanged, OnPick, OnSubmit);
                    _indexDirty = true;
                    Debug.Log("[OptionsSearch] search UI attached");
                }
                catch (Exception ex)
                {
                    Debug.Log("[OptionsSearch] UI creation failed: " + ex);
                    _panel = null;
                    _nextFind = Time.realtimeSinceStartup + 5f;
                    return;
                }
            }

            // 言語設定が変わったらUIを作り直す(プレースホルダ等を更新)
            bool ja = Str.IsJa();
            if (ja != _lastJa)
            {
                _lastJa = ja;
                if (_ui != null) _ui.Destroy();
                _ui = null;
                return;
            }

            bool visible = _panel.isVisible;
            if (visible && !_wasVisible)
            {
                Debug.Log("[OptionsSearch] options panel opened");
                _indexDirty = true; // 開くたびに再構築(MOD設定の増減に追従)
                _ui.Clear();
            }
            else if (!visible && _wasVisible)
            {
                _ui.Clear();
                _searchPending = false;
            }
            _wasVisible = visible;
            if (!visible) return;

            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.F))
            {
                _ui.Focus();
            }

            if (_searchPending && Time.realtimeSinceStartup >= _searchAt)
            {
                _searchPending = false;
                RunSearch(_ui.Text);
            }
        }

        // ------------------------------------------------------------ callbacks

        void OnTextChanged(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                _searchPending = false;
                if (_ui != null) _ui.HideResults();
                return;
            }
            _searchPending = true;
            _searchAt = Time.realtimeSinceStartup + DebounceSec;
        }

        void OnSubmit()
        {
            if (_ui == null) return;
            int total;
            List<Entry> r = OptionsIndex.Search(EnsureIndex(), _ui.Text, out total);
            if (r.Count > 0) OnPick(r[0]);
        }

        void OnPick(Entry e)
        {
            if (_ui != null) _ui.HideResults();
            StartCoroutine(Jumper.Run(e, _panel));
        }

        void RunSearch(string text)
        {
            if (_ui == null) return;
            int total;
            List<Entry> r = OptionsIndex.Search(EnsureIndex(), text, out total);
            _ui.ShowResults(r, total);
        }

        List<Entry> EnsureIndex()
        {
            if (_index == null || _indexDirty)
            {
                try
                {
                    _index = OptionsIndex.Build(_panel, _ui != null ? _ui.Root : null);
                    // 検索UI自身(結果パネル)は兄弟として作られるので除外する
                    _index.RemoveAll(delegate (Entry e) { return e.Target != null && IsOwnUI(e.Target); });
                    Debug.Log("[OptionsSearch] indexed " + _index.Count + " entries");
                }
                catch (Exception ex)
                {
                    Debug.Log("[OptionsSearch] index build failed: " + ex);
                    _index = new List<Entry>();
                }
                _indexDirty = false;
            }
            return _index;
        }

        static bool IsOwnUI(UIComponent c)
        {
            int guard = 0;
            while (c != null && guard++ < 100)
            {
                if (c.name == "OptionsSearchResults" || c.name == "OptionsSearchBox") return true;
                c = c.parent;
            }
            return false;
        }

        // ----------------------------------------------------------------- find

        static UIComponent FindOptionsPanel()
        {
            try
            {
                UIPanel p = UIView.library.Get<UIPanel>(PanelName);
                if (p != null) return p;
            }
            catch (Exception) { }

            try
            {
                UIView view = UIView.GetAView();
                if (view == null) return null;
                UIPanel[] all = view.GetComponentsInChildren<UIPanel>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].name == PanelName) return all[i];
                }
            }
            catch (Exception) { }
            return null;
        }
    }
}
