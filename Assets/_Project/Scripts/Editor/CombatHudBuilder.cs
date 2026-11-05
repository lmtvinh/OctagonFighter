using System;
using OctagonFighter.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace OctagonFighter.Editor
{
    /// <summary>
    /// Rebuilds the combat HUD deterministically: a modern, symmetric, MMA-inspired layout with a
    /// dominant Health bar, a secondary Stamina bar, per-side accent colors, and a delayed damage
    /// trail. Safe to re-run: the existing <c>CombatHud</c> root is deleted and recreated, and the
    /// fighter references are rebound by name. No other scene object is touched.
    /// </summary>
    /// <remarks>
    /// Layout (anchored to the top edge, mirrored per side):
    ///   PlayerHealth  (container, top-left)  — name row, framed Health bar with damage trail, accent rule
    ///   PlayerStamina (container, below it)  — small caps label + framed Stamina bar
    ///   PlayerGuard   (container, below it)  — small caps label + framed Guard bar
    ///   OpponentHealth / OpponentStamina / OpponentGuard — mirrored on the right edge
    /// </remarks>
    public static class CombatHudBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/CombatTest.unity";
        private const string HudRootName = "CombatHud";
        private const string PlayerName = "Fighter_Player";
        private const string OpponentName = "Fighter_Opponent";

        private static readonly Color PanelBackground = Rgba(18, 20, 27, 200);
        private static readonly Color InnerShadow = Rgba(0, 0, 0, 96);
        private static readonly Color InnerHighlight = Rgba(255, 255, 255, 22);
        private static readonly Color PlayerAccent = Rgba(65, 217, 126, 255);
        private static readonly Color OpponentAccent = Rgba(255, 107, 61, 255);
        private static readonly Color StaminaFill = Rgba(56, 182, 232, 255);
        private static readonly Color GuardFill = Rgba(242, 199, 71, 255);
        private static readonly Color DamageTrail = Rgba(255, 200, 87, 255);
        private static readonly Color NameColor = Rgba(242, 243, 245, 255);
        private static readonly Color MutedColor = Rgba(199, 208, 220, 255);
        private static readonly Color ValueColor = Rgba(255, 255, 255, 255);

        private const string HealthValueFormat = "<size=38>{0}</size><size=17> <color=#C7D0DC>/ {1}</color></size>";
        private const string StaminaValueFormat = "<size=19>{0}</size><size=12> <color=#C7D0DC>/ {1}</color></size>";
        private const string GuardValueFormat = "<size=15>{0}</size><size=10> <color=#C7D0DC>/ {1}</color></size>";

        [MenuItem("OctagonFighter/Assets/Build Combat HUD")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find(PlayerName);
            GameObject opponent = GameObject.Find(OpponentName);
            if (player == null || opponent == null)
            {
                throw new InvalidOperationException(
                    $"CombatTest must contain '{PlayerName}' and '{OpponentName}'.");
            }

            GameObject oldHud = GameObject.Find(HudRootName);
            if (oldHud != null)
            {
                UnityEngine.Object.DestroyImmediate(oldHud);
            }

            Font font = LoadInterfaceFont();

            GameObject root = new GameObject(HudRootName, typeof(RectTransform));
            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = Vector2.zero;
            rootRect.localScale = Vector3.one;
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            root.AddComponent<GraphicRaycaster>();

            BuildSide(
                root.transform,
                mirror: false,
                title: "P L A Y E R",
                fighter: player.GetComponent<Presentation.Fighter.FighterController>(),
                accent: PlayerAccent,
                font: font);

            BuildSide(
                root.transform,
                mirror: true,
                title: "O P P O N E N T",
                fighter: opponent.GetComponent<Presentation.Fighter.FighterController>(),
                accent: OpponentAccent,
                font: font);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CombatHud] Combat HUD rebuilt and saved into CombatTest.unity.");
        }

        private static void BuildSide(
            Transform parent,
            bool mirror,
            string title,
            Presentation.Fighter.FighterController fighter,
            Color accent,
            Font font)
        {
            string side = mirror ? "Opponent" : "Player";
            Vector2 anchor = new Vector2(mirror ? 1f : 0f, 1f);
            Vector2 pivot = new Vector2(mirror ? 1f : 0f, 1f);
            float edge = mirror ? -44f : 44f;
            TextAnchor innerAlign = mirror ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            TextAnchor outerAlign = mirror ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            float innerAnchor = mirror ? 1f : 0f;
            float outerAnchor = mirror ? 0f : 1f;
            int fillOrigin = mirror ? 1 : 0;

            // Translucent dark plate behind the whole cluster so every label clears the
            // background contrast bar on any arena backdrop. Rendered first, so it sits behind.
            GameObject plate = CreateImage(
                parent,
                side + "Plate",
                Rgba(13, 15, 20, 152),
                anchor: new Vector2(mirror ? 1f : 0f, 1f),
                pivot: new Vector2(mirror ? 1f : 0f, 1f),
                anchoredPosition: new Vector2(edge, -16f),
                size: new Vector2(664f, 204f));
            AddInnerDepth(plate.transform);

            GameObject healthHud = CreateContainer(
                parent,
                side + "Health",
                anchor,
                pivot,
                new Vector2(edge, -26f),
                new Vector2(640f, 78f));

            CreateText(
                healthHud.transform,
                "NameLabel",
                title,
                font,
                fontSize: 24,
                fontStyle: FontStyle.BoldAndItalic,
                color: NameColor,
                alignment: innerAlign,
                anchor: new Vector2(innerAnchor, 1f),
                pivot: new Vector2(innerAnchor, 1f),
                anchoredPosition: Vector2.zero,
                size: new Vector2(320f, 30f));

            Text healthValue = CreateText(
                healthHud.transform,
                "HealthValue",
                "<size=38>100</size><size=17> <color=#C7D0DC>/ 100</color></size>",
                font,
                fontSize: 30,
                fontStyle: FontStyle.BoldAndItalic,
                color: ValueColor,
                alignment: outerAlign,
                anchor: new Vector2(outerAnchor, 1f),
                pivot: new Vector2(outerAnchor, 1f),
                anchoredPosition: Vector2.zero,
                size: new Vector2(240f, 34f),
                richText: true);

            GameObject healthBack = CreateImage(
                healthHud.transform,
                "BarBack",
                PanelBackground,
                anchor: new Vector2(0f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(0f, -38f),
                size: new Vector2(640f, 34f),
                stretchWidth: true);

            Image healthTrail = CreateInsetImage(healthBack.transform, "DamageTrail", DamageTrail, fillOrigin);
            Image healthFill = CreateInsetImage(healthBack.transform, "Fill", accent, fillOrigin);
            AddInnerDepth(healthBack.transform);

            CreateImage(
                healthHud.transform,
                "Accent",
                WithAlpha(accent, 140),
                anchor: new Vector2(0f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(0f, -75f),
                size: new Vector2(0f, 3f),
                stretchWidth: true);

            GameObject staminaHud = CreateContainer(
                parent,
                side + "Stamina",
                anchor,
                pivot,
                new Vector2(edge, -118f),
                new Vector2(640f, 44f));

            CreateText(
                staminaHud.transform,
                "StaminaLabel",
                "S T A M I N A",
                font,
                fontSize: 14,
                fontStyle: FontStyle.Bold,
                color: MutedColor,
                alignment: innerAlign,
                anchor: new Vector2(innerAnchor, 1f),
                pivot: new Vector2(innerAnchor, 1f),
                anchoredPosition: new Vector2(0f, 0f),
                size: new Vector2(280f, 20f));

            Text staminaValue = CreateText(
                staminaHud.transform,
                "StaminaValue",
                "<size=19>100</size><size=12> <color=#C7D0DC>/ 100</color></size>",
                font,
                fontSize: 18,
                fontStyle: FontStyle.Bold,
                color: ValueColor,
                alignment: outerAlign,
                anchor: new Vector2(outerAnchor, 1f),
                pivot: new Vector2(outerAnchor, 1f),
                anchoredPosition: new Vector2(0f, 0f),
                size: new Vector2(160f, 20f),
                richText: true);

            GameObject staminaBack = CreateImage(
                staminaHud.transform,
                "BarBack",
                PanelBackground,
                anchor: new Vector2(0f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(0f, -26f),
                size: new Vector2(640f, 15f),
                stretchWidth: true);

            Image staminaFill = CreateInsetImage(staminaBack.transform, "Fill", StaminaFill, fillOrigin);
            AddInnerDepth(staminaBack.transform);

            GameObject guardHud = CreateContainer(
                parent,
                side + "Guard",
                anchor,
                pivot,
                new Vector2(edge, -166f),
                new Vector2(640f, 40f));

            CreateText(
                guardHud.transform,
                "GuardLabel",
                "G U A R D",
                font,
                fontSize: 12,
                fontStyle: FontStyle.Bold,
                color: MutedColor,
                alignment: innerAlign,
                anchor: new Vector2(innerAnchor, 1f),
                pivot: new Vector2(innerAnchor, 1f),
                anchoredPosition: Vector2.zero,
                size: new Vector2(280f, 18f));

            Text guardValue = CreateText(
                guardHud.transform,
                "GuardValue",
                "<size=15>100</size><size=10> <color=#C7D0DC>/ 100</color></size>",
                font,
                fontSize: 14,
                fontStyle: FontStyle.Bold,
                color: ValueColor,
                alignment: outerAlign,
                anchor: new Vector2(outerAnchor, 1f),
                pivot: new Vector2(outerAnchor, 1f),
                anchoredPosition: Vector2.zero,
                size: new Vector2(160f, 18f),
                richText: true);

            GameObject guardBack = CreateImage(
                guardHud.transform,
                "BarBack",
                PanelBackground,
                anchor: new Vector2(0f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(0f, -22f),
                size: new Vector2(640f, 10f),
                stretchWidth: true);

            Image guardFill = CreateInsetImage(guardBack.transform, "Fill", GuardFill, fillOrigin);
            AddInnerDepth(guardBack.transform);

            WireHealthBar(healthHud, fighter, healthFill, healthTrail, healthValue, accent);
            WireStaminaBar(staminaHud, fighter, staminaFill, staminaValue);
            WireGuardBar(guardHud, fighter, guardFill, guardValue);
        }

        private static void WireHealthBar(
            GameObject healthHud,
            Presentation.Fighter.FighterController fighter,
            Image fill,
            Image trail,
            Text valueLabel,
            Color accent)
        {
            FighterHealthBar bar = healthHud.AddComponent<Presentation.UI.FighterHealthBar>();
            SetPrivate(bar, "_fighter", fighter);
            SetPrivate(bar, "_fill", fill);
            SetPrivate(bar, "_trailFill", trail);
            SetPrivate(bar, "_label", valueLabel);
            SetPrivate(bar, "_title", string.Empty);
            SetPrivate(bar, "_valueFormat", HealthValueFormat);
            SetPrivate(bar, "_healthyColor", accent);
        }

        private static void WireStaminaBar(
            GameObject staminaHud,
            Presentation.Fighter.FighterController fighter,
            Image fill,
            Text valueLabel)
        {
            FighterStaminaBar bar = staminaHud.AddComponent<Presentation.UI.FighterStaminaBar>();
            SetPrivate(bar, "_fighter", fighter);
            SetPrivate(bar, "_fill", fill);
            SetPrivate(bar, "_label", valueLabel);
            SetPrivate(bar, "_title", string.Empty);
            SetPrivate(bar, "_valueFormat", StaminaValueFormat);
        }

        private static void WireGuardBar(
            GameObject guardHud,
            Presentation.Fighter.FighterController fighter,
            Image fill,
            Text valueLabel)
        {
            FighterGuardBar bar = guardHud.AddComponent<Presentation.UI.FighterGuardBar>();
            SetPrivate(bar, "_fighter", fighter);
            SetPrivate(bar, "_fill", fill);
            SetPrivate(bar, "_label", valueLabel);
            SetPrivate(bar, "_title", string.Empty);
            SetPrivate(bar, "_valueFormat", GuardValueFormat);
        }

        private static Image CreateInsetImage(Transform parent, string name, Color color, int fillOrigin)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            StretchFull(rect, inset: 3f);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = fillOrigin;
            image.fillAmount = 1f;
            image.raycastTarget = false;
            return image;
        }

        private static void AddInnerDepth(Transform back)
        {
            CreateImage(
                back,
                "InnerHighlight",
                InnerHighlight,
                anchor: new Vector2(0f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: Vector2.zero,
                size: new Vector2(0f, 2f),
                stretchWidth: true);

            CreateImage(
                back,
                "InnerShadow",
                InnerShadow,
                anchor: new Vector2(0f, 0f),
                pivot: new Vector2(0.5f, 0f),
                anchoredPosition: Vector2.zero,
                size: new Vector2(0f, 2f),
                stretchWidth: true);
        }

        private static GameObject CreateContainer(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return go;
        }

        private static GameObject CreateImage(
            Transform parent,
            string name,
            Color color,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            bool stretchWidth = false)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = new Vector2(stretchWidth ? 1f : anchor.x, anchor.y);
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = stretchWidth ? new Vector2(0f, size.y) : size;
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string text,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            TextAnchor alignment,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            bool richText = false)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Text uiText = go.AddComponent<Text>();
            uiText.text = text;
            uiText.font = font;
            uiText.fontSize = fontSize;
            uiText.fontStyle = fontStyle;
            uiText.color = color;
            uiText.alignment = alignment;
            uiText.supportRichText = richText;
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            uiText.raycastTarget = false;
            return uiText;
        }

        private static void StretchFull(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-inset * 2f, -inset * 2f);
        }

        private static Font LoadInterfaceFont()
        {
            string[] candidates = { "LegacyRuntime.ttf", "Arial.ttf" };
            foreach (string candidate in candidates)
            {
                try
                {
                    Font font = Resources.GetBuiltinResource<Font>(candidate);
                    if (font != null)
                    {
                        return font;
                    }
                }
                catch (Exception)
                {
                    // Try the next built-in font name.
                }
            }

            throw new InvalidOperationException("No built-in interface font is available.");
        }

        private static Color Rgba(int r, int g, int b, int a)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        private static Color WithAlpha(Color color, int alpha)
        {
            color.a = alpha / 255f;
            return color;
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            if (field == null)
            {
                throw new InvalidOperationException(
                    $"Field '{fieldName}' was not found on {target.GetType().FullName}.");
            }

            field.SetValue(target, value);
        }
    }
}
