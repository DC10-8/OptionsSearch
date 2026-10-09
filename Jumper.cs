using System;
using System.Collections;
using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace OptionsSearch
{
    /// <summary>検索結果の項目までタブ切替→スクロール→点滅で案内する。</summary>
    internal static class Jumper
    {
        public static IEnumerator Run(Entry e, UIComponent root)
        {
            if (e == null || e.Target == null) yield break;

            List<Ctx> ctx = OptionsIndex.Contexts(e.Target, root, e.IsPage, null);
            for (int i = 0; i < ctx.Count; i++)
            {
                OptionsIndex.Select(ctx[i]);
                yield return null; // 遅延生成・レイアウト反映を待つ
            }
            yield return null;
            yield return null;

            if (e.IsPage) yield break;
            if (e.Target == null) yield break;

            ScrollIntoView(e.Target);
            yield return null;
            ScrollIntoView(e.Target);

            float original = SafeOpacity(e.Target);
            float end = Time.realtimeSinceStartup + 1.8f;
            bool dim = false;
            float next = 0f;
            while (Time.realtimeSinceStartup < end)
            {
                if (e.Target == null) yield break;
                if (Time.realtimeSinceStartup >= next)
                {
                    dim = !dim;
                    SetOpacity(e.Target, dim ? 0.15f : original);
                    next = Time.realtimeSinceStartup + 0.18f;
                }
                yield return null;
            }
            if (e.Target != null) SetOpacity(e.Target, original);
        }

        static void ScrollIntoView(UIComponent target)
        {
            try
            {
                UIComponent a = target.parent;
                int guard = 0;
                while (a != null && guard++ < 100)
                {
                    UIScrollablePanel sp = a as UIScrollablePanel;
                    if (sp != null)
                    {
                        float delta = target.absolutePosition.y - sp.absolutePosition.y - 30f;
                        float y = Mathf.Max(0f, sp.scrollPosition.y + delta);
                        sp.scrollPosition = new Vector2(sp.scrollPosition.x, y);
                    }
                    a = a.parent;
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[OptionsSearch] Scroll failed: " + ex.Message);
            }
        }

        static float SafeOpacity(UIComponent c)
        {
            try { return c.opacity; } catch (Exception) { return 1f; }
        }

        static void SetOpacity(UIComponent c, float v)
        {
            try { c.opacity = v; } catch (Exception) { }
        }
    }
}
