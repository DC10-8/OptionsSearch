using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace OptionsSearch
{
    /// <summary>オプション画面に追加する検索ボックスと結果リスト。</summary>
    internal sealed class SearchUI
    {
        // 位置の調整はここ (オプション画面左上からの相対座標)
        const float BoxX = 24f;
        const float BoxY = 12f;
        const float BoxW = 320f;
        const float BoxH = 30f;
        const float ResultsW = 560f;
        const float RowH = 40f;
        const float HeaderH = 26f;
        const int MaxTitleChars = 80;

        sealed class Row
        {
            public UIPanel Panel;
            public UILabel Title;
            public UILabel Crumb;
            public Entry Entry;
        }

        readonly UIComponent _options;
        readonly Action<string> _onChanged;
        readonly Action<Entry> _onPick;
        readonly Action _onSubmit;

        UIPanel _box;
        UITextField _field;
        UILabel _placeholder;
        UIPanel _results;
        UILabel _status;
        readonly List<Row> _slots = new List<Row>();

        List<Entry> _shown = new List<Entry>();
        int _total;
        int _offset;

        public UIComponent Root { get { return _box; } }
        public bool IsAlive { get { return _box != null && _field != null; } }
        public string Text { get { return _field != null ? _field.text : ""; } }

        public SearchUI(UIComponent options, Action<string> onChanged, Action<Entry> onPick, Action onSubmit)
        {
            _options = options;
            _onChanged = onChanged;
            _onPick = onPick;
            _onSubmit = onSubmit;
            Build();
        }

        static UITextureAtlas DefaultAtlas()
        {
            try { return UIView.GetAView().defaultAtlas; } catch (Exception) { return null; }
        }

        void Build()
        {
            UITextureAtlas atlas = DefaultAtlas();

            _box = _options.AddUIComponent<UIPanel>();
            _box.name = "OptionsSearchBox";
            _box.size = new Vector2(BoxW, BoxH);
            _box.relativePosition = new Vector3(BoxX, BoxY);

            _field = _box.AddUIComponent<UITextField>();
            _field.name = "OptionsSearchField";
            if (atlas != null) _field.atlas = atlas;
            _field.size = new Vector2(BoxW, BoxH);
            _field.relativePosition = Vector3.zero;
            _field.normalBgSprite = "TextFieldPanel";
            _field.hoveredBgSprite = "TextFieldPanelHovered";
            _field.focusedBgSprite = "TextFieldPanel";
            _field.selectionSprite = "EmptySprite";
            _field.selectionBackgroundColor = new Color32(0, 172, 234, 255);
            _field.textColor = new Color32(255, 255, 255, 255);
            _field.builtinKeyNavigation = true;
            _field.isInteractive = true;
            _field.enabled = true;
            _field.readOnly = false;
            _field.submitOnFocusLost = false;
            _field.cursorWidth = 1;
            _field.cursorBlinkTime = 0.45f;
            _field.textScale = 0.9f;
            _field.padding = new RectOffset(8, 8, 7, 4);
            _field.horizontalAlignment = UIHorizontalAlignment.Left;
            _field.verticalAlignment = UIVerticalAlignment.Middle;

            _placeholder = _box.AddUIComponent<UILabel>();
            _placeholder.text = Str.Placeholder;
            _placeholder.textScale = 0.85f;
            _placeholder.textColor = new Color32(150, 160, 170, 255);
            _placeholder.isInteractive = false;
            _placeholder.relativePosition = new Vector3(10f, 8f);

            _field.eventTextChanged += delegate (UIComponent c, string value)
            {
                _placeholder.isVisible = string.IsNullOrEmpty(value);
                _onChanged(value);
            };
            _field.eventTextSubmitted += delegate (UIComponent c, string value)
            {
                _onSubmit();
            };
            _field.eventGotFocus += delegate (UIComponent c, UIFocusEventParameter p)
            {
                if (!string.IsNullOrEmpty(_field.text)) _onChanged(_field.text);
            };
            _field.eventKeyDown += delegate (UIComponent c, UIKeyEventParameter p)
            {
                if (p.keycode == KeyCode.Escape && !string.IsNullOrEmpty(_field.text))
                {
                    Clear();
                    p.Use(); // オプション画面自体は閉じない
                }
            };

            // ---- 結果パネル (不透明な背景 + 固定数の行スロット。自前でスクロール管理) ----
            float resH = Mathf.Min(460f, Mathf.Max(200f, _options.height - (BoxY + BoxH + 30f)));
            int slotCount = Mathf.Max(3, (int)((resH - HeaderH - 6f) / RowH));
            resH = HeaderH + 6f + slotCount * RowH;

            _results = _options.AddUIComponent<UIPanel>();
            _results.name = "OptionsSearchResults";
            if (atlas != null) _results.atlas = atlas;
            _results.backgroundSprite = "GenericPanel";
            _results.color = new Color32(24, 32, 42, 255);
            _results.opacity = 1f;
            _results.size = new Vector2(ResultsW, resH);
            _results.relativePosition = new Vector3(BoxX, BoxY + BoxH + 4f);
            _results.isVisible = false;
            _results.eventMouseWheel += OnWheel;

            _status = _results.AddUIComponent<UILabel>();
            _status.textScale = 0.75f;
            _status.textColor = new Color32(170, 180, 190, 255);
            _status.isInteractive = false;
            _status.relativePosition = new Vector3(10f, 6f);

            for (int i = 0; i < slotCount; i++)
            {
                _slots.Add(CreateRow(atlas, i));
            }
        }

        // ------------------------------------------------------------------ API

        public void Focus()
        {
            if (_field != null) _field.Focus();
        }

        public void Clear()
        {
            if (_field != null) _field.text = "";
            HideResults();
        }

        public void HideResults()
        {
            if (_results != null) _results.isVisible = false;
        }

        public void ShowResults(List<Entry> shown, int total)
        {
            if (_results == null) return;
            _shown = shown;
            _total = total;
            _offset = 0;
            _results.isVisible = true;
            _results.BringToFront();
            Render();
        }

        public void Destroy()
        {
            try
            {
                if (_results != null)
                {
                    _options.RemoveUIComponent(_results);
                    UnityEngine.Object.Destroy(_results.gameObject);
                }
                if (_box != null)
                {
                    _options.RemoveUIComponent(_box);
                    UnityEngine.Object.Destroy(_box.gameObject);
                }
            }
            catch (Exception) { }
            _results = null;
            _box = null;
            _field = null;
        }

        // ---------------------------------------------------------------- inner

        void OnWheel(UIComponent c, UIMouseEventParameter p)
        {
            if (_shown == null || _shown.Count <= _slots.Count) { p.Use(); return; }
            int step = 3;
            _offset += p.wheelDelta > 0f ? -step : step;
            _offset = Mathf.Clamp(_offset, 0, _shown.Count - _slots.Count);
            Render();
            p.Use();
        }

        void Render()
        {
            int count = _shown.Count;
            if (count == 0)
            {
                _status.text = Str.NoResults;
            }
            else
            {
                int first = _offset + 1;
                int last = Mathf.Min(count, _offset + _slots.Count);
                _status.text = Str.Results(count, _total) + "  (" + first + "-" + last + ")";
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                Row r = _slots[i];
                int idx = _offset + i;
                if (idx < count)
                {
                    Entry e = _shown[idx];
                    r.Entry = e;
                    string t = e.IsPage ? (Str.PageTag + " " + e.Text) : e.Text;
                    if (t.Length > MaxTitleChars) t = t.Substring(0, MaxTitleChars) + "...";
                    r.Title.text = t;
                    string crumb = string.IsNullOrEmpty(e.Breadcrumb) ? " " : e.Breadcrumb;
                    if (crumb.Length > 110) crumb = crumb.Substring(0, 110) + "...";
                    r.Crumb.text = crumb;
                    r.Panel.isVisible = true;
                }
                else
                {
                    r.Entry = null;
                    r.Panel.isVisible = false;
                }
            }
        }

        Row CreateRow(UITextureAtlas atlas, int slot)
        {
            var row = new Row();
            UIPanel p = _results.AddUIComponent<UIPanel>();
            if (atlas != null) p.atlas = atlas;
            p.size = new Vector2(ResultsW - 12f, RowH - 2f);
            p.relativePosition = new Vector3(6f, HeaderH + slot * RowH);
            p.backgroundSprite = "GenericPanel";
            p.color = new Color32(36, 46, 58, 255);
            p.isVisible = false;
            row.Panel = p;

            row.Title = p.AddUIComponent<UILabel>();
            row.Title.autoSize = false;
            row.Title.wordWrap = false;
            row.Title.size = new Vector2(p.width - 20f, 18f);
            row.Title.textScale = 0.85f;
            row.Title.textColor = new Color32(255, 255, 255, 255);
            row.Title.isInteractive = false;
            row.Title.relativePosition = new Vector3(10f, 3f);

            row.Crumb = p.AddUIComponent<UILabel>();
            row.Crumb.autoSize = false;
            row.Crumb.wordWrap = false;
            row.Crumb.size = new Vector2(p.width - 20f, 14f);
            row.Crumb.textScale = 0.65f;
            row.Crumb.textColor = new Color32(150, 165, 180, 255);
            row.Crumb.isInteractive = false;
            row.Crumb.relativePosition = new Vector3(10f, 22f);

            p.eventMouseEnter += delegate (UIComponent c, UIMouseEventParameter e) { p.color = new Color32(70, 110, 150, 255); };
            p.eventMouseLeave += delegate (UIComponent c, UIMouseEventParameter e) { p.color = new Color32(36, 46, 58, 255); };
            p.eventMouseWheel += OnWheel;
            p.eventClick += delegate (UIComponent c, UIMouseEventParameter e)
            {
                if (row.Entry != null) _onPick(row.Entry);
            };
            return row;
        }
    }
}
