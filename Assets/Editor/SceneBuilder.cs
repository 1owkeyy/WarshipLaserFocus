// Builds the Fleet Battle slot machine scene from scratch. Re-runnable: it wipes the
// generated roots and rebuilds, so layout tweaks are a one-liner away.
using System.IO;
using FleetBattle;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FleetBattle.EditorTools
{
    public static class SceneBuilder
    {
        const string Art = "Assets/Art/";

        // ---- layout constants (1080x1920 portrait) ----
        const float ReelW = 300f, ReelH = 300f, ReelGap = 18f;

        static readonly Color Steel = new Color(0.78f, 0.87f, 0.96f);
        static readonly Color SteelDim = new Color(0.45f, 0.56f, 0.68f);
        static readonly Color PanelFill = new Color(0.055f, 0.105f, 0.165f, 0.92f);
        static readonly Color PanelStroke = new Color(0.25f, 0.42f, 0.60f, 0.55f);

        [MenuItem("Fleet Battle/Build Scene")]
        public static string Build()
        {
            EnsureTmpResources();

            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "UI" || root.name == "GameSystems" || root.name == "EventSystem")
                    Object.DestroyImmediate(root);

            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.05f, 0.09f);
            }

            // ---------------- assets ----------------
            var sprPanel = Load<Sprite>(Art + "UI/panel_round.png");
            var sprStroke = Load<Sprite>(Art + "UI/panel_stroke.png");
            var sprGlow = Load<Sprite>(Art + "UI/glow_radial.png");
            var sprFade = Load<Sprite>(Art + "UI/fade_vert.png");
            var sprPx = Load<Sprite>(Art + "UI/px_white.png");
            var sprBg = Load<Sprite>(Art + "UI/bg_ocean.png");

            // ---------------- systems ----------------
            var systems = new GameObject("GameSystems");
            var library = systems.AddComponent<SymbolLibrary>();
            ConfigureLibrary(library);
            var resolver = systems.AddComponent<OutcomeResolver>();
            var controller = systems.AddComponent<SlotMachineController>();

            // ---------------- canvas ----------------
            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var canvasRt = (RectTransform)canvasGo.transform;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                // This project is on the Input System package, so the legacy
                // StandaloneInputModule would throw on the first click.
                var moduleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem")
                                 ?? typeof(StandaloneInputModule);
                es.AddComponent(moduleType);
            }

            // ---------------- background ----------------
            var bg = UiFactory.Image("Background", canvasRt, sprBg, Color.white);
            bg.rectTransform.Stretch();
            bg.preserveAspect = false;

            // ---------------- header ----------------
            var header = UiFactory.Rect("Header", canvasRt).Place(new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(900f, 160f));
            var titleText = UiFactory.Text("Title", header, "FLEET BATTLE", 86f, Steel);
            titleText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(900f, 110f));
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 14f;
            var rule = UiFactory.Image("Rule", header, sprPx, new Color(0.25f, 0.5f, 0.75f, 0.55f));
            rule.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), new Vector2(420f, 4f));

            // ---------------- reels ----------------
            float frameW = ReelW * 3f + ReelGap * 2f + 48f;
            var reelFrame = UiFactory.Rect("ReelFrame", canvasRt).Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 380f), new Vector2(frameW, ReelH + 44f));
            var frameBg = UiFactory.Image("Frame", reelFrame, sprPanel, new Color(0.03f, 0.07f, 0.12f, 0.95f), Image.Type.Sliced);
            frameBg.rectTransform.Stretch();
            var frameStroke = UiFactory.Image("FrameStroke", reelFrame, sprStroke, new Color(0.28f, 0.48f, 0.70f, 0.7f), Image.Type.Sliced);
            frameStroke.rectTransform.Stretch();

            var reels = new ReelView[3];
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * (ReelW + ReelGap);
                var reelRt = UiFactory.Rect("Reel_" + i, reelFrame).Place(new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(ReelW, ReelH));
                // draw order inside a reel: background, masked strip, glass fades
                var slotBg = UiFactory.Image("SlotBg", reelRt, sprPanel, new Color(0.015f, 0.035f, 0.06f, 1f), Image.Type.Sliced);
                slotBg.rectTransform.Stretch();

                var viewport = UiFactory.Rect("Viewport", reelRt).Stretch();
                viewport.gameObject.AddComponent<RectMask2D>();

                var fadeTop = UiFactory.Image("FadeTop", reelRt, sprFade, new Color(0.012f, 0.03f, 0.055f, 0.9f));
                fadeTop.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(ReelW - 8f, 56f));
                var fadeBottom = UiFactory.Image("FadeBottom", reelRt, sprFade, new Color(0.012f, 0.03f, 0.055f, 0.9f));
                fadeBottom.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(ReelW - 8f, 56f));
                fadeBottom.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);

                var reel = reelRt.gameObject.AddComponent<ReelView>();
                reel.reelIndex = i;
                reel.library = library;
                reel.viewport = viewport;
                reel.tileSprite = sprPanel;
                reel.glowSprite = sprGlow;
                reel.cellHeight = ReelH;
                reel.cellWidth = ReelW;
                reel.BuildStrip();
                reel.SnapTo((SlotSymbol)i);
                reels[i] = reel;
            }

            // payline markers either side of the reel row
            for (int s = -1; s <= 1; s += 2)
            {
                var m = UiFactory.Image("Payline" + s, reelFrame, sprPx, new Color(0.95f, 0.55f, 0.2f, 0.8f));
                m.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(s * (frameW * 0.5f + 18f), 0f), new Vector2(26f, 6f));
            }

            // ---------------- message panel ----------------
            var msg = UiFactory.Rect("MessagePanel", canvasRt).Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(940f, 360f));
            var msgBg = UiFactory.Image("Bg", msg, sprPanel, PanelFill, Image.Type.Sliced);
            msgBg.rectTransform.Stretch();
            var msgStroke = UiFactory.Image("Stroke", msg, sprStroke, PanelStroke, Image.Type.Sliced);
            msgStroke.rectTransform.Stretch();

            var content = UiFactory.Rect("Content", msg).Stretch(24f);
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 6f;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var msgIcon = UiFactory.Image("Icon", content, null, Color.white);
            msgIcon.preserveAspect = true;
            AddLayout(msgIcon.gameObject, 132f);
            msgIcon.gameObject.SetActive(false); // only shown for icon-carrying outcomes
            var msgTitle = UiFactory.Text("Title", content, "READY TO FIRE", 62f, Steel);
            msgTitle.fontStyle = FontStyles.Bold;
            msgTitle.characterSpacing = 6f;
            msgTitle.overflowMode = TextOverflowModes.Overflow;
            // auto-fit keeps long copy (BROADSIDE!, SHIELD ACQUIRED!) inside the panel
            msgTitle.enableAutoSizing = true;
            msgTitle.fontSizeMin = 34f;
            msgTitle.fontSizeMax = 62f;
            AddLayout(msgTitle.gameObject, 132f);
            var msgSub = UiFactory.Text("Subtitle", content, "Tap SPIN to take your shot", 40f, SteelDim);
            AddLayout(msgSub.gameObject, 52f);

            var burstRoot = UiFactory.Rect("Burst", msg).Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));

            var presenter = msg.gameObject.AddComponent<OutcomePresenter>();
            presenter.library = library;
            presenter.panel = msg;
            presenter.panelBackground = msgBg;
            presenter.panelStroke = msgStroke;
            presenter.icon = msgIcon;
            presenter.title = msgTitle;
            presenter.subtitle = msgSub;
            presenter.burstRoot = burstRoot;
            presenter.burstSprite = sprGlow;

            presenter.canvasGroup = msg.gameObject.AddComponent<CanvasGroup>();

            // ---------------- torpedo mini-event ----------------
            var sprCross = Load<Sprite>(Art + "UI/icon_crosshair.png");
            var sprShield = Load<Sprite>(Art + "Symbols/icon_shield.png");

            var torp = UiFactory.Rect("TorpedoPanel", canvasRt).Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(940f, 470f));
            var torpCg = torp.gameObject.AddComponent<CanvasGroup>();
            var torpBg = UiFactory.Image("Bg", torp, sprPanel, new Color(0.03f, 0.09f, 0.13f, 0.96f), Image.Type.Sliced);
            torpBg.rectTransform.Stretch();
            var torpStroke = UiFactory.Image("Stroke", torp, sprStroke, new Color(0.145f, 0.816f, 0.784f, 0.8f), Image.Type.Sliced);
            torpStroke.rectTransform.Stretch();

            var torpHead = UiFactory.Text("Headline", torp, "TORPEDO READY", 64f, new Color(0.145f, 0.816f, 0.784f));
            torpHead.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 168f), new Vector2(860f, 92f));
            torpHead.fontStyle = FontStyles.Bold;
            torpHead.characterSpacing = 6f;
            torpHead.enableAutoSizing = true;
            torpHead.fontSizeMin = 34f;
            torpHead.fontSizeMax = 68f;

            var torpHint = UiFactory.Text("Hint", torp, "Choose your target", 38f, SteelDim);
            torpHint.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 106f), new Vector2(860f, 52f));

            var zones = new TorpedoZone[3];
            string[] zoneNames = { "ZONE A", "ZONE B", "ZONE C" };
            for (int i = 0; i < 3; i++)
            {
                var zoneRt = UiFactory.Rect("Zone_" + i, torp)
                    .Place(new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 294f, -74f), new Vector2(280f, 258f));

                var zBg = UiFactory.Image("Bg", zoneRt, sprPanel, new Color(0.07f, 0.13f, 0.20f, 0.95f), Image.Type.Sliced);
                zBg.rectTransform.Stretch();
                zBg.raycastTarget = true;
                var zStroke = UiFactory.Image("Stroke", zoneRt, sprStroke, new Color(0.30f, 0.48f, 0.68f, 0.75f), Image.Type.Sliced);
                zStroke.rectTransform.Stretch();
                var zChosen = UiFactory.Image("ChosenStroke", zoneRt, sprStroke, new Color(1f, 0.86f, 0.55f, 0.95f), Image.Type.Sliced);
                zChosen.rectTransform.Stretch(-7f);   // sits just outside the normal frame
                zChosen.enabled = false;

                var zIcon = UiFactory.Image("Icon", zoneRt, sprCross, new Color(0.62f, 0.78f, 0.94f, 0.9f));
                zIcon.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 32f), new Vector2(118f, 118f));
                zIcon.preserveAspect = true;

                var zLabel = UiFactory.Text("Label", zoneRt, zoneNames[i], 34f, SteelDim);
                zLabel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -76f), new Vector2(264f, 52f));
                zLabel.fontStyle = FontStyles.Bold;
                zLabel.characterSpacing = 4f;
                zLabel.enableAutoSizing = true;
                zLabel.fontSizeMin = 22f;
                zLabel.fontSizeMax = 36f;

                var zButton = zoneRt.gameObject.AddComponent<Button>();
                zButton.targetGraphic = zBg;
                var zc = zButton.colors;
                zc.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
                zc.pressedColor = new Color(0.8f, 0.8f, 0.8f);
                zc.disabledColor = Color.white;
                zc.fadeDuration = 0.06f;
                zButton.colors = zc;

                var zone = zoneRt.gameObject.AddComponent<TorpedoZone>();
                zone.button = zButton;
                zone.background = zBg;
                zone.stroke = zStroke;
                zone.chosenStroke = zChosen;
                zone.icon = zIcon;
                zone.label = zLabel;
                zone.crosshairSprite = sprCross;
                zone.shieldSprite = sprShield;
                zone.zoneName = zoneNames[i];
                zones[i] = zone;
            }

            var torpedo = torp.gameObject.AddComponent<TorpedoEvent>();
            torpedo.panel = torp;
            torpedo.canvasGroup = torpCg;
            torpedo.headline = torpHead;
            torpedo.hint = torpHint;
            torpedo.zones = zones;
            torpCg.alpha = 0f;              // hidden until a Torpedo outcome calls it up
            torpCg.blocksRaycasts = false;
            torpCg.interactable = false;

            // ---------------- spin button ----------------
            var btnRt = UiFactory.Rect("SpinButton", canvasRt).Place(new Vector2(0.5f, 0f), new Vector2(0f, 300f), new Vector2(720f, 190f));
            var btnGlow = UiFactory.Image("Glow", btnRt, sprGlow, new Color(1f, 0.5f, 0.15f, 0.28f));
            btnGlow.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 420f));
            var btnBg = UiFactory.Image("Bg", btnRt, sprPanel, new Color(0.93f, 0.42f, 0.16f), Image.Type.Sliced);
            btnBg.rectTransform.Stretch();
            btnBg.raycastTarget = true;
            var btnStroke = UiFactory.Image("Stroke", btnRt, sprStroke, new Color(1f, 0.78f, 0.45f, 0.85f), Image.Type.Sliced);
            btnStroke.rectTransform.Stretch();
            var btnLabel = UiFactory.Text("Label", btnRt, "SPIN", 84f, Color.white);
            btnLabel.rectTransform.Stretch();
            btnLabel.fontStyle = FontStyles.Bold;
            btnLabel.characterSpacing = 12f;
            var button = btnRt.gameObject.AddComponent<Button>();
            button.targetGraphic = btnBg;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.06f;
            button.colors = colors;

            // ---------------- full screen flash (jackpot) ----------------
            var flash = UiFactory.Image("ScreenFlash", canvasRt, sprPx, new Color(1f, 1f, 1f, 0f));
            flash.rectTransform.Stretch();
            flash.transform.SetAsLastSibling();
            presenter.screenFlash = flash;

            // ---------------- wire controller ----------------
            controller.resolver = resolver;
            controller.library = library;
            controller.presenter = presenter;
            controller.torpedoEvent = torpedo;
            controller.reels = reels;
            controller.spinButton = button;
            controller.spinButtonBackground = btnBg;
            controller.spinButtonLabel = btnLabel;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Scene built and saved: " + scene.path;
        }

        static void AddLayout(GameObject go, float height)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
        }

        static void ConfigureLibrary(SymbolLibrary lib)
        {
            lib.broadsideFragments = new[] { "BRO", "AD", "SIDE" };
            lib.styles = new[]
            {
                Style(SlotSymbol.XP,        "icon_xp",      "#FFC93C", "XP FOUND!",         "Crew experience gained"),
                Style(SlotSymbol.Cannon,    "icon_cannon",  "#E2532E", "CANNON HIT!",       "Direct hit on the enemy hull"),
                Style(SlotSymbol.Shield,    "icon_shield",  "#3E9BE8", "SHIELD ACQUIRED!",  "Incoming damage absorbed"),
                Style(SlotSymbol.Torpedo,   "icon_torpedo", "#25D0C8", "TORPEDO READY",     "Choose your target"),
                Style(SlotSymbol.Energy,    "icon_energy",  "#AEE235", "ENERGY RESTORED!",  "Reactors back to full"),
                Style(SlotSymbol.Broadside, null,           "#FF6A1A", "BROADSIDE!",        "Massive attack - every gun fires"),
            };
        }

        static SymbolStyle Style(SlotSymbol symbol, string iconName, string hex, string title, string sub)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return new SymbolStyle
            {
                symbol = symbol,
                icon = iconName == null ? null : Load<Sprite>(Art + "Symbols/" + iconName + ".png"),
                tint = color,
                resultTitle = title,
                resultSubtitle = sub,
            };
        }

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Debug.LogWarning("[SceneBuilder] Missing asset: " + path);
            return asset;
        }

        static void EnsureTmpResources()
        {
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null) return;

            // TMP ships its runtime font/shader assets in a .unitypackage that has to be
            // imported into the project once before any TextMeshProUGUI can render.
            var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Settings).Assembly);
            var path = pkg != null ? Path.Combine(pkg.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage") : null;
            if (path != null && File.Exists(path)) { AssetDatabase.ImportPackage(path, false); AssetDatabase.Refresh(); }
            else Debug.LogWarning("[SceneBuilder] Import TMP Essential Resources (Window > TextMeshPro) before building.");
        }
    }
}
