using System;
using System.Collections.Generic;
using System.Text;
using ColossalFramework.UI;
using UnityEngine;

namespace OptionsSearch
{
    internal sealed class Entry
    {
        public string Text = "";
        public string Extra = "";
        public string Breadcrumb = "";
        public UIComponent Target;
        public bool IsPage;

        public string TextLower = "";
        public string ExtraLower = "";
        public string CrumbLower = "";

        public void Finish()
        {
            TextLower = (Text ?? "").ToLowerInvariant();
            ExtraLower = (Extra ?? "").ToLowerInvariant();
            CrumbLower = (Breadcrumb ?? "").ToLowerInvariant();
        }
    }

    /// <summary>タブ(カテゴリ/MODページ)の1階層分。</summary>
    internal struct Ctx
    {
        public UITabContainer Container;
        public int Index;
        public UIComponent Selector; // 左のリストボックス / タブストリップ(無ければnull)
        public string Name;
    }

    internal static class OptionsIndex
    {
        sealed class Deferred
        {
            public UIComponent Source;
            public string Text;
            public bool Fallback;
        }

        // ---------------------------------------------------------------- build

        public static List<Entry> Build(UIComponent root, UIComponent exclude)
        {
            var entries = new List<Entry>(2048);
            var labelMap = new Dictionary<UIComponent, Entry>();
            var deferred = new List<Deferred>();
            var selCache = new Dictionary<UITabContainer, UIComponent>();

            Walk(root, exclude, 0, entries, labelMap, deferred, selCache);

            for (int i = 0; i < deferred.Count; i++)
            {
                AttachExtra(deferred[i], entries, labelMap);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e.Target != null)
                {
                    e.Breadcrumb = Crumb(Contexts(e.Target, root, false, selCache));
                }
                e.Finish();
            }
            return entries;
        }

        static void Walk(UIComponent c, UIComponent exclude, int depth, List<Entry> entries,
                         Dictionary<UIComponent, Entry> labelMap, List<Deferred> deferred,
                         Dictionary<UITabContainer, UIComponent> selCache)
        {
            if (c == null || c == exclude || depth > 100) return;

            UITabContainer tc = c as UITabContainer;
            if (tc != null)
            {
                AddPageEntries(tc, entries, selCache);
            }

            UILabel label = c as UILabel;
            UIButton button = c as UIButton;
            UIDropDown dropdown = c as UIDropDown;

            if (label != null)
            {
                if (Useful(label.text))
                {
                    var e = new Entry { Text = label.text.Trim(), Target = label };
                    entries.Add(e);
                    labelMap[label] = e;
                }
            }
            else if (button != null)
            {
                if (Useful(button.text) && !(button.parent is UITabstrip) && !(button.parent is UIDropDown) && !(button.parent is UIScrollbar))
                {
                    entries.Add(new Entry { Text = button.text.Trim(), Target = button });
                }
            }
            else if (dropdown != null && Config.IncludeDropdownItems.value)
            {
                string[] items = dropdown.items;
                if (items != null && items.Length > 0)
                {
                    deferred.Add(new Deferred { Source = dropdown, Text = string.Join(" ", items), Fallback = false });
                }
            }

            if (label == null && Config.IncludeTooltips.value && !string.IsNullOrEmpty(c.tooltip))
            {
                deferred.Add(new Deferred { Source = c, Text = c.tooltip, Fallback = true });
            }

            for (int i = 0; i < c.components.Count; i++)
            {
                Walk(c.components[i], exclude, depth + 1, entries, labelMap, deferred, selCache);
            }
        }

        static void AddPageEntries(UITabContainer tc, List<Entry> entries, Dictionary<UITabContainer, UIComponent> selCache)
        {
            UIComponent sel = FindSelector(tc, selCache);
            if (sel == null) return;
            for (int i = 0; i < tc.components.Count; i++)
            {
                string name = SelectorName(sel, i);
                if (string.IsNullOrEmpty(name)) continue;
                entries.Add(new Entry { Text = name, Target = tc.components[i], IsPage = true });
            }
        }

        static void AttachExtra(Deferred d, List<Entry> entries, Dictionary<UIComponent, Entry> labelMap)
        {
            if (d.Source == null || string.IsNullOrEmpty(d.Text)) return;

            Entry host = null;
            // 1) 子のラベル(チェックボックスなど)
            for (int i = 0; i < d.Source.components.Count && host == null; i++)
            {
                UILabel l = d.Source.components[i] as UILabel;
                if (l != null) labelMap.TryGetValue(l, out host);
            }
            // 2) 同じ親の兄弟ラベル(UIHelperのスライダー/ドロップダウン)
            UIComponent parent = d.Source.parent;
            if (host == null && parent != null)
            {
                for (int i = 0; i < parent.components.Count && host == null; i++)
                {
                    UILabel l = parent.components[i] as UILabel;
                    if (l != null) labelMap.TryGetValue(l, out host);
                }
            }

            if (host != null)
            {
                host.Extra = host.Extra + " " + d.Text;
            }
            else if (d.Fallback)
            {
                entries.Add(new Entry { Text = d.Text.Trim(), Target = d.Source });
            }
        }

        static bool Useful(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string t = text.Trim();
            if (t.Length < 2) return false;
            for (int i = 0; i < t.Length; i++)
            {
                if (char.IsLetter(t[i])) return true; // "50%" や "1.0" のような値ラベルを除外
            }
            return false;
        }

        // ------------------------------------------------------------- contexts

        /// <summary>targetを含むタブ階層を外側→内側の順で返す。</summary>
        public static List<Ctx> Contexts(UIComponent target, UIComponent root, bool includeSelf,
                                         Dictionary<UITabContainer, UIComponent> selCache)
        {
            var list = new List<Ctx>();
            UIComponent child = includeSelf ? target : (target != null ? target.parent : null);
            int guard = 0;
            while (child != null && child != root && guard++ < 200)
            {
                UITabContainer p = child.parent as UITabContainer;
                if (p != null)
                {
                    int idx = IndexOf(p, child);
                    UIComponent sel = FindSelector(p, selCache);
                    string name = sel != null ? SelectorName(sel, idx) : null;
                    if (string.IsNullOrEmpty(name)) name = child.name;
                    list.Insert(0, new Ctx { Container = p, Index = idx, Selector = sel, Name = name });
                }
                child = child.parent;
            }
            return list;
        }

        static string Crumb(List<Ctx> ctx)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < ctx.Count; i++)
            {
                if (string.IsNullOrEmpty(ctx[i].Name)) continue;
                if (sb.Length > 0) sb.Append(" > ");
                sb.Append(ctx[i].Name);
            }
            return sb.ToString();
        }

        static int IndexOf(UITabContainer tc, UIComponent child)
        {
            for (int i = 0; i < tc.components.Count; i++)
            {
                if (tc.components[i] == child) return i;
            }
            return -1;
        }

        public static void Select(Ctx c)
        {
            try
            {
                if (c.Index < 0 || c.Container == null) return;
                UIListBox lb = c.Selector as UIListBox;
                UITabstrip ts = c.Selector as UITabstrip;
                if (lb != null) lb.selectedIndex = c.Index;
                else if (ts != null) ts.selectedIndex = c.Index;
                if (c.Container.selectedIndex != c.Index) c.Container.selectedIndex = c.Index;
            }
            catch (Exception ex)
            {
                Debug.Log("[OptionsSearch] Select failed: " + ex.Message);
            }
        }

        // ------------------------------------------------------------ selectors

        static UIComponent FindSelector(UITabContainer tc, Dictionary<UITabContainer, UIComponent> cache)
        {
            UIComponent found;
            if (cache != null && cache.TryGetValue(tc, out found)) return found;

            found = null;
            int count = tc.components.Count;
            UIComponent scope = tc.parent;
            for (int level = 0; level < 3 && scope != null && found == null; level++)
            {
                found = SearchSelector(scope, tc, count, 2);
                scope = scope.parent;
            }
            if (cache != null) cache[tc] = found;
            return found;
        }

        static UIComponent SearchSelector(UIComponent scope, UITabContainer skip, int count, int depth)
        {
            for (int i = 0; i < scope.components.Count; i++)
            {
                UIComponent c = scope.components[i];
                if (c == null || c == skip) continue;
                if (SelectorCount(c) == count) return c;
            }
            if (depth > 0)
            {
                for (int i = 0; i < scope.components.Count; i++)
                {
                    UIComponent c = scope.components[i];
                    if (c == null || c == skip) continue;
                    UIComponent r = SearchSelector(c, skip, count, depth - 1);
                    if (r != null) return r;
                }
            }
            return null;
        }

        static int SelectorCount(UIComponent c)
        {
            UIListBox lb = c as UIListBox;
            if (lb != null) return lb.items == null ? -1 : lb.items.Length;
            UITabstrip ts = c as UITabstrip;
            if (ts != null) return ts.components.Count;
            return -1;
        }

        static string SelectorName(UIComponent sel, int index)
        {
            if (index < 0) return null;
            UIListBox lb = sel as UIListBox;
            if (lb != null)
            {
                return (lb.items != null && index < lb.items.Length) ? lb.items[index] : null;
            }
            UITabstrip ts = sel as UITabstrip;
            if (ts != null && index < ts.components.Count)
            {
                UIButton b = ts.components[index] as UIButton;
                return b != null ? b.text : null;
            }
            return null;
        }

        // --------------------------------------------------------------- search

        public static List<Entry> Search(List<Entry> all, string query, out int total)
        {
            total = 0;
            var result = new List<Entry>();
            if (all == null || string.IsNullOrEmpty(query)) return result;

            string[] tokens = query.ToLowerInvariant().Split(new char[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return result;

            var scored = new List<KeyValuePair<int, Entry>>();
            for (int i = 0; i < all.Count; i++)
            {
                Entry e = all[i];
                if (e.Target == null) continue;

                int score = 0;
                bool ok = true;
                for (int t = 0; t < tokens.Length; t++)
                {
                    string tok = tokens[t];
                    if (e.TextLower.IndexOf(tok, StringComparison.Ordinal) >= 0)
                    {
                        score += 4;
                        if (e.TextLower.StartsWith(tok, StringComparison.Ordinal)) score += 2;
                    }
                    else if (e.ExtraLower.IndexOf(tok, StringComparison.Ordinal) >= 0) score += 2;
                    else if (e.CrumbLower.IndexOf(tok, StringComparison.Ordinal) >= 0) score += 1;
                    else { ok = false; break; }
                }
                if (!ok) continue;
                if (e.IsPage) score += 1;
                scored.Add(new KeyValuePair<int, Entry>(score, e));
            }

            scored.Sort(delegate (KeyValuePair<int, Entry> a, KeyValuePair<int, Entry> b)
            {
                int c = b.Key.CompareTo(a.Key);
                if (c != 0) return c;
                c = string.CompareOrdinal(a.Value.CrumbLower, b.Value.CrumbLower);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Value.TextLower, b.Value.TextLower);
            });

            total = scored.Count;
            int max = Mathf.Max(10, Config.MaxResults.value);
            for (int i = 0; i < scored.Count && i < max; i++)
            {
                result.Add(scored[i].Value);
            }
            return result;
        }
    }
}
