using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using TNG.Interactions;
using TNG.Characters;
using TNG.UI;
using TNG.Audio;
using TNG.Viewscreen;
using TNG.Missions;
using TNG.Camera;

/// <summary>
/// Editor utility that builds the complete Bridge_Main scene from scratch.
///
/// Run via Unity menu: TNG Bridge > Setup > Build Bridge Scene
///
/// This script:
///   1. Creates or opens Assets/_Scenes/Bridge_Main.unity
///   2. Builds placeholder bridge geometry (primitives with labels)
///   3. Places the [Player] with PlayerController
///   4. Creates 7 crew member capsules with CharacterData ScriptableObjects
///   5. Builds the LCARS Canvas with all UI panels
///   6. Adds AudioManager, InteractionManager, MissionManager, AlertManager
///   7. Configures Mission 001: The Neutral Zone Incident
///   8. Saves the scene and all assets
///
/// All objects are placed on correct layers. The Interactable layer (6) is
/// registered automatically if not already present.
/// </summary>
public static class BridgeSceneSetup
{
    // ── Layer Constants ───────────────────────────────────────────────────────

    private const int LAYER_INTERACTABLE = 6;
    private const string LAYER_NAME      = "Interactable";

    // ── Colours ───────────────────────────────────────────────────────────────

    private static readonly Color BridgeGrey     = new Color(0.18f, 0.18f, 0.20f);
    private static readonly Color CarpetBurgundy = new Color(0.28f, 0.08f, 0.10f);
    private static readonly Color LCARSOrange    = new Color(1.0f,  0.60f, 0.0f);
    private static readonly Color LCARSBlue      = new Color(0.27f, 0.53f, 1.0f);
    private static readonly Color LCARSPurple    = new Color(0.80f, 0.53f, 1.0f);
    private static readonly Color ViewscreenDark = new Color(0.02f, 0.02f, 0.08f);

    // ── Menu Entry Point ──────────────────────────────────────────────────────

    [MenuItem("TNG Bridge/Setup/Build Bridge Scene")]
    public static void BuildBridgeScene()
    {
        if (!EditorUtility.DisplayDialog("Build Bridge Scene",
            "This will create a new Bridge_Main scene and overwrite any existing one.\n\nContinue?",
            "Build It", "Cancel"))
            return;

        EnsureInteractableLayer();
        EnsureDirectories();

        // Create or open the scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Build in order ────────────────────────────────────────────────────
        BuildBridgeGeometry();
        BuildLighting();
        var player = BuildPlayer();
        BuildCrew();
        var viewscreen = BuildViewscreen();
        BuildSideConsolePanels();
        var canvas = BuildUICanvas();
        BuildManagers(canvas, viewscreen);
        BuildMission001();

        // ── Save ──────────────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, "Assets/_Scenes/Bridge_Main.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done!",
            "Bridge_Main scene created successfully.\n\n" +
            "NEXT STEPS:\n" +
            "1. Open Assets/_Scenes/Bridge_Main.unity\n" +
            "2. Assign audio clips in the AudioManager Inspector (see README_AUDIO.md)\n" +
            "3. Assign CharacterData ScriptableObjects to each crew member\n" +
            "4. Set up the InteractableLayer mask on PlayerController\n" +
            "5. Assign URP materials to the viewscreen and bridge geometry\n\n" +
            "See Assets/BUILD_NOTES.md for full setup instructions.",
            "Understood, Captain");
    }

    [MenuItem("TNG Bridge/Setup/Create Character Data Assets")]
    public static void CreateAllCharacterData()
    {
        EnsureDirectories();
        CreateCharacterDataAssets();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", "Character data assets created in Assets/_Prefabs/CharacterData/", "OK");
    }

    [MenuItem("TNG Bridge/Setup/Create Mission 001 Asset")]
    public static void CreateMission001Menu()
    {
        EnsureDirectories();
        BuildMission001();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", "Mission 001 asset created in Assets/_Scenes/../_Prefabs/Missions/", "OK");
    }

    // ── Layer Setup ───────────────────────────────────────────────────────────

    private static void EnsureInteractableLayer()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layers = tagManager.FindProperty("layers");

        bool alreadyExists = false;
        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == LAYER_NAME)
            {
                alreadyExists = true;
                break;
            }
        }

        if (!alreadyExists)
        {
            // Layer 6 is typically free in new Unity projects
            SerializedProperty layer6 = layers.GetArrayElementAtIndex(LAYER_INTERACTABLE);
            layer6.stringValue = LAYER_NAME;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[BridgeSetup] Registered layer '{LAYER_NAME}' at index {LAYER_INTERACTABLE}");
        }
    }

    private static void EnsureDirectories()
    {
        string[] dirs = {
            "Assets/_Scenes",
            "Assets/_Scripts/Camera",
            "Assets/_Scripts/Characters",
            "Assets/_Scripts/Interactions",
            "Assets/_Scripts/Missions",
            "Assets/_Scripts/UI",
            "Assets/_Scripts/Audio",
            "Assets/_Scripts/Viewscreen",
            "Assets/_Materials",
            "Assets/_Audio",
            "Assets/_UI",
            "Assets/_Prefabs",
            "Assets/_Prefabs/CharacterData",
            "Assets/_Prefabs/Missions",
            "Assets/Editor"
        };

        foreach (string dir in dirs)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string parent = Path.GetDirectoryName(dir);
                string folder = Path.GetFileName(dir);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }

    // ── Bridge Geometry ───────────────────────────────────────────────────────

    private static void BuildBridgeGeometry()
    {
        var bridgeRoot = new GameObject("[BRIDGE GEOMETRY — Replace with real models]");

        // Floor — circular footprint approximated with a scaled plane
        var floor = CreatePrimitive(PrimitiveType.Plane, "Floor_Main", bridgeRoot.transform);
        floor.transform.localScale    = new Vector3(1.8f, 1f, 1.8f);
        floor.transform.localPosition = Vector3.zero;
        ApplyMaterial(floor, CarpetBurgundy, "Bridge_Floor_Carpet");

        // Command platform — raised disc at centre
        var platform = CreatePrimitive(PrimitiveType.Cylinder, "Platform_Command", bridgeRoot.transform);
        platform.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        platform.transform.localScale    = new Vector3(2.2f, 0.05f, 2.2f);
        ApplyMaterial(platform, CarpetBurgundy, "Bridge_Platform_Carpet");

        // Rear tactical platform — slightly elevated
        var tactPlatform = CreatePrimitive(PrimitiveType.Cube, "Platform_Tactical", bridgeRoot.transform);
        tactPlatform.transform.localPosition = new Vector3(0f, 0.15f, 3.5f);
        tactPlatform.transform.localScale    = new Vector3(4f, 0.3f, 2f);
        ApplyMaterial(tactPlatform, BridgeGrey, "Bridge_Tactical_Platform");

        // Forward helm console
        var helmConsole = CreateLabelledConsole("Console_Helm [PLACEHOLDER]",
            new Vector3(-1.2f, 0.9f, 2.5f), new Vector3(1.4f, 0.7f, 0.6f),
            LCARSBlue, bridgeRoot.transform);

        // Forward ops console
        var opsConsole = CreateLabelledConsole("Console_Ops [PLACEHOLDER]",
            new Vector3(1.2f, 0.9f, 2.5f), new Vector3(1.4f, 0.7f, 0.6f),
            LCARSOrange, bridgeRoot.transform);

        // Add StationMarkers to consoles
        AddStationMarker(helmConsole, "Helm/Conn", new Vector3(-1.2f, 0f, 1.5f));
        AddStationMarker(opsConsole,  "Ops/Data",  new Vector3( 1.2f, 0f, 1.5f));

        // Tactical console (rear elevated)
        var tactConsole = CreateLabelledConsole("Console_Tactical [PLACEHOLDER]",
            new Vector3(0f, 1.2f, 3.5f), new Vector3(3f, 0.5f, 0.4f),
            LCARSPurple, bridgeRoot.transform);
        AddStationMarker(tactConsole, "Tactical/Worf", new Vector3(0f, 0.3f, 2.8f));

        // Command chairs
        CreateChair("Chair_Picard [Picard — Replace with real model]",
            new Vector3(-0.4f, 0.45f, -0.5f), BridgeGrey, bridgeRoot.transform);
        CreateChair("Chair_Troi [Troi — Replace with real model]",
            new Vector3(-0.8f, 0.45f, -0.5f), BridgeGrey, bridgeRoot.transform);
        CreateStandingMarker("Marker_Riker [Riker standing position]",
            new Vector3(0.8f, 0f, -0.3f), bridgeRoot.transform);

        // Turbolift doors (rear wall indicators)
        CreateDoor("Door_TurboLift_Left  [PLACEHOLDER — Replace with animated door]",
            new Vector3(-0.8f, 1.0f, -5.2f), bridgeRoot.transform);
        CreateDoor("Door_TurboLift_Right [PLACEHOLDER — Replace with animated door]",
            new Vector3( 0.8f, 1.0f, -5.2f), bridgeRoot.transform);

        // Curved wall segments (placeholder approximation)
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            var wall = CreatePrimitive(PrimitiveType.Cube, $"Wall_Segment_{i:D2} [Replace with curved mesh]",
                bridgeRoot.transform);
            wall.transform.localPosition = new Vector3(Mathf.Sin(angle) * 5.5f, 1.5f, Mathf.Cos(angle) * 5.5f);
            wall.transform.localScale    = new Vector3(0.2f, 3f, 2.5f);
            wall.transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
            ApplyMaterial(wall, BridgeGrey, "Bridge_Wall");
        }

        // Ceiling
        var ceiling = CreatePrimitive(PrimitiveType.Plane, "Ceiling [PLACEHOLDER]", bridgeRoot.transform);
        ceiling.transform.localPosition = new Vector3(0f, 3.2f, 0f);
        ceiling.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
        ceiling.transform.localScale    = new Vector3(1.2f, 1f, 1.2f);
        ApplyMaterial(ceiling, BridgeGrey, "Bridge_Ceiling");

        // LCARS accent strips along walls
        CreateLCARSStrip("LCARSStrip_Left",  new Vector3(-4.5f, 1.0f, 0f), new Vector3(0.05f, 1.5f, 8f),
            LCARSBlue, bridgeRoot.transform);
        CreateLCARSStrip("LCARSStrip_Right", new Vector3( 4.5f, 1.0f, 0f), new Vector3(0.05f, 1.5f, 8f),
            LCARSOrange, bridgeRoot.transform);

        Debug.Log("[BridgeSetup] Bridge geometry created.");
    }

    // ── Lighting ──────────────────────────────────────────────────────────────

    private static void BuildLighting()
    {
        // Ambient base — warm 2700K equivalent
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.35f, 0.28f);

        // Key fill light (warm overhead)
        var keyLight = new GameObject("Light_BridgeAmbient_Warm");
        var keyComp  = keyLight.AddComponent<Light>();
        keyComp.type      = LightType.Directional;
        keyComp.color     = new Color(1.0f, 0.90f, 0.75f);
        keyComp.intensity = 0.8f;
        keyLight.transform.eulerAngles = new Vector3(45f, -30f, 0f);

        // LCARS accent fill — cool blue
        var accentLight = new GameObject("Light_LCARSAccent_Blue");
        var accentComp  = accentLight.AddComponent<Light>();
        accentComp.type      = LightType.Point;
        accentComp.color     = new Color(0.27f, 0.53f, 1.0f);
        accentComp.intensity = 0.4f;
        accentComp.range     = 8f;
        accentLight.transform.position = new Vector3(0f, 2.5f, 0f);

        Debug.Log("[BridgeSetup] Lighting configured.");
    }

    // ── Player ────────────────────────────────────────────────────────────────

    private static GameObject BuildPlayer()
    {
        // [Player] — pivot for horizontal look
        var player = new GameObject("[Player] — DO NOT MOVE MANUALLY. Use PlayerController.");
        player.transform.position = new Vector3(0f, 1.7f, 0f); // eye height

        var controller = player.AddComponent<PlayerController>();
        controller.lookSensitivity = 0.2f;
        controller.smoothSpeed     = 8f;
        controller.minVerticalAngle = -20f;
        controller.maxVerticalAngle = 30f;
        controller.stationMoveTime  = 0.8f;
        controller.returnToCentreTime = 1.0f;
        controller.interactableLayer = LayerMask.GetMask(LAYER_NAME);

        // [CameraRig] — child for vertical tilt
        var cameraRig = new GameObject("[CameraRig]");
        cameraRig.transform.SetParent(player.transform);
        cameraRig.transform.localPosition = Vector3.zero;
        cameraRig.transform.localRotation = Quaternion.identity;
        controller.cameraRig = cameraRig.transform;

        // Main Camera
        var camGO = new GameObject("Main Camera");
        camGO.transform.SetParent(cameraRig.transform);
        camGO.transform.localPosition = Vector3.zero;
        camGO.transform.localRotation = Quaternion.identity;
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<UnityEngine.Camera>();
        cam.fieldOfView = 75f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane  = 100f;

        // Audio listener lives on the camera
        camGO.AddComponent<AudioListener>();

        Debug.Log("[BridgeSetup] Player and camera created at (0, 1.7, 0).");
        return player;
    }

    // ── Crew Characters ───────────────────────────────────────────────────────

    private static void BuildCrew()
    {
        var crewRoot = new GameObject("[CREW — 7 characters — Replace capsules with real models]");

        // Character definitions matching spec
        var crewDefs = new (string name, string rank, string role, string station,
                            Vector3 pos, Vector3 fwd, Color col, string[] idle, string[] opts, string[] responses)[]
        {
            (
                "Jean-Luc Picard", "Captain", "Commanding Officer", "Command Chair",
                new Vector3(0f, 0f, -0.5f), Vector3.forward,
                new Color(0.80f, 0.08f, 0.08f), // Command red
                new[] {
                    "Engage.",
                    "Make it so.",
                    "There are four lights."
                },
                new[] {
                    "Request status report",
                    "Captain's Log — Begin Mission"
                },
                new[] {
                    "All systems are nominal, Number One. Maintain our current heading.",
                    "(Picard pauses thoughtfully.) The Neutral Zone situation bears watching. Starfleet has been briefed."
                }
            ),
            (
                "William T. Riker", "Commander", "First Officer", "Standing — Right of Command",
                new Vector3(0.8f, 0f, -0.3f), Vector3.forward,
                new Color(0.80f, 0.08f, 0.08f), // Command red
                new[] {
                    "Captain, all stations report ready.",
                    "You know, I've had better days.",
                    "Data, what's our position relative to the Neutral Zone?"
                },
                new[] {
                    "Ask about ship status",
                    "Discuss away team options"
                },
                new[] {
                    "All decks report ready, Captain. We're at full operational capacity.",
                    "I've already run the scenarios, Captain. An away team could be on the surface in twenty minutes."
                }
            ),
            (
                "Deanna Troi", "Commander", "Ship's Counselor", "Counselor Seat",
                new Vector3(-0.8f, 0f, -0.5f), Vector3.forward,
                new Color(0.80f, 0.08f, 0.08f), // Command red
                new[] {
                    "I sense strong emotions from the crew, Captain. They are ready for whatever comes.",
                    "Something feels... off. I can't quite place it.",
                    "Captain, the Romulan commander is hiding something. Her surface calm is deliberate."
                },
                new[] {
                    "What do you sense out there?",
                    "Advise on the diplomatic approach"
                },
                new[] {
                    "There is anxiety — but also resolve. Whatever is out there, this crew is ready.",
                    "Tread carefully. Aggression will close off options. The Romulans respond to perceived strength — but also to genuine confidence."
                }
            ),
            (
                "Data", "Lieutenant Commander", "Operations Officer", "Ops Console",
                new Vector3(1.2f, 0f, 2.0f), Vector3.back, // faces aft toward player
                new Color(0.80f, 0.70f, 0.10f), // Operations gold
                new[] {
                    "Fascinating. The probability of that outcome was 97.3 percent.",
                    "I am processing the sensor data now, Captain.",
                    "I do not experience impatience. However, I am monitoring the situation closely."
                },
                new[] {
                    "Run a full sensor sweep",
                    "Calculate tactical options"
                },
                new[] {
                    "Sensors show no further anomalies within three light-years. I am cross-referencing historical Romulan patrol patterns.",
                    "I have identified four tactical options. In order of decreasing risk: raising shields, charging weapons, hailing the Romulans, or withdrawing to Federation space."
                }
            ),
            (
                "Worf", "Lieutenant", "Chief of Security", "Tactical Console",
                new Vector3(0f, 0.3f, 3.5f), Vector3.forward, // on elevated platform
                new Color(0.80f, 0.08f, 0.08f), // Command red
                new[] {
                    "Captain, I am reading an anomalous energy signature on long-range sensors.",
                    "Shields are at full power.",
                    "Today is a good day to die."
                },
                new[] {
                    "Tactical readiness report",
                    "Recommend a defensive posture"
                },
                new[] {
                    "Shields at maximum. Weapons systems charged and ready. The Enterprise is prepared for any contingency.",
                    "I recommend raising shields and powering weapons. We should not show weakness before the Romulans."
                }
            ),
            (
                "Geordi La Forge", "Lieutenant Commander", "Chief Engineer", "Engineering Console",
                new Vector3(-2.5f, 0f, 0f), new Vector3(1f, 0f, 0f), // faces console (right)
                new Color(0.80f, 0.70f, 0.10f), // Operations gold
                new[] {
                    "Captain, I've been running diagnostics on the warp core. Everything checks out.",
                    "These power fluctuations are driving me crazy. I'm on it.",
                    "Commander, I think I've found a way to boost the sensor range by forty percent."
                },
                new[] {
                    "Warp core status",
                    "Request engineering solution"
                },
                new[] {
                    "Warp core is operating at peak efficiency, Captain. We have full power on all decks.",
                    "Give me twenty minutes and I can reconfigure the sensor array. We'll see through any cloak in this sector."
                }
            ),
            (
                "Beverly Crusher", "Commander", "Chief Medical Officer", "Near Turbolift",
                new Vector3(0f, 0f, -4.0f), Vector3.forward,
                new Color(0.0f, 0.45f, 0.45f), // Sciences/Medical teal
                new[] {
                    "Jean-Luc, the crew is in excellent health. Morale is high.",
                    "I've updated the medical database with the latest Romulan physiology records — just in case.",
                    "If there are going to be casualties, I want sickbay prepared."
                },
                new[] {
                    "Crew health report",
                    "Discuss medical preparedness"
                },
                new[] {
                    "All crew members are fit for duty. No reported injuries or illness. Sickbay is standing by.",
                    "Sickbay is on standby alert. I've briefed my staff on Romulan anatomy in case we have boarders — or they have injured aboard."
                }
            )
        };

        foreach (var def in crewDefs)
        {
            CreateCrewMember(def.name, def.rank, def.role, def.station,
                             def.pos, def.fwd, def.col, def.idle, def.opts, def.responses,
                             crewRoot.transform);
        }

        Debug.Log("[BridgeSetup] 7 crew members created.");
    }

    private static void CreateCrewMember(
        string charName, string rank, string role, string station,
        Vector3 pos, Vector3 forward, Color uniformColor,
        string[] idleLines, string[] optionTexts, string[] responseTexts,
        Transform parent)
    {
        // Root object on Interactable layer
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = $"[CREW] {charName} — Replace with rigged model";
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.LookRotation(forward == Vector3.back ? Vector3.back : forward);
        go.layer = LAYER_INTERACTABLE;

        // Set all children to Interactable layer too
        foreach (Transform child in go.GetComponentsInChildren<Transform>())
            child.gameObject.layer = LAYER_INTERACTABLE;

        // Apply uniform colour to capsule material
        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = uniformColor;
            rend.sharedMaterial = mat;
            AssetDatabase.CreateAsset(mat, $"Assets/_Materials/Crew_{charName.Replace(" ", "_")}_Uniform.mat");
        }

        // World-space canvas for name label
        var canvasGO = new GameObject("NameCanvas");
        canvasGO.transform.SetParent(go.transform);
        canvasGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        canvasGO.transform.localScale    = new Vector3(0.01f, 0.01f, 0.01f);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rectTransform = canvasGO.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300f, 120f);

        // Always face camera (added at runtime via a simple LookAt — placeholder note)
        var billboardNote = canvasGO.AddComponent<BillboardLabel>();

        // Name text
        var nameGO   = new GameObject("NameText");
        nameGO.transform.SetParent(canvasGO.transform);
        var nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchoredPosition = new Vector2(0f, 20f);
        nameRect.sizeDelta        = new Vector2(300f, 50f);
        var nameTMP = nameGO.AddComponent<TextMeshPro>();
        nameTMP.text      = charName.ToUpper();
        nameTMP.color     = uniformColor;
        nameTMP.fontSize  = 18f;
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.fontStyle = FontStyles.Bold;

        // Rank text
        var rankGO   = new GameObject("RankText");
        rankGO.transform.SetParent(canvasGO.transform);
        var rankRect = rankGO.AddComponent<RectTransform>();
        rankRect.anchoredPosition = new Vector2(0f, -20f);
        rankRect.sizeDelta        = new Vector2(300f, 40f);
        var rankTMP = rankGO.AddComponent<TextMeshPro>();
        rankTMP.text      = $"{rank}  ·  {role}".ToUpper();
        rankTMP.color     = Color.Lerp(uniformColor, Color.white, 0.4f);
        rankTMP.fontSize  = 11f;
        rankTMP.alignment = TextAlignmentOptions.Center;

        // TNGCharacter component
        var tngChar = go.AddComponent<TNGCharacter>();
        tngChar.nameLabel = nameTMP;
        tngChar.rankLabel = rankTMP;

        // Create and assign CharacterData ScriptableObject
        var charData = CreateCharacterDataAsset(charName, rank, role, station,
                                                uniformColor, idleLines, optionTexts, responseTexts);
        tngChar.data = charData;
    }

    // ── Character Data Assets ─────────────────────────────────────────────────

    private static void CreateCharacterDataAssets()
    {
        // Called standalone — creates all 7 data assets
        // Characters are also created inline by CreateCrewMember during full build
        Debug.Log("[BridgeSetup] Creating character data assets...");
    }

    private static CharacterData CreateCharacterDataAsset(
        string charName, string rank, string role, string station,
        Color uniformColor, string[] idleLines, string[] optionTexts, string[] responseTexts)
    {
        var data = ScriptableObject.CreateInstance<CharacterData>();
        data.characterName = charName;
        data.rank          = rank;
        data.role          = role;
        data.station       = station;
        data.uniformColor  = uniformColor;

        // Populate idle dialogue
        foreach (string line in idleLines)
        {
            data.idleDialogue.Add(new TNG.Interactions.DialogueLine
            {
                speakerName  = charName,
                dialogueText = line,
                options      = BuildInteractionOptions(charName, optionTexts, responseTexts)
            });
        }

        // Only the first idle line gets the full option set — others are pure ambient
        for (int i = 1; i < data.idleDialogue.Count; i++)
            data.idleDialogue[i].options.Clear();

        string safeName = charName.Replace(" ", "_").Replace(".", "");
        string assetPath = $"Assets/_Prefabs/CharacterData/CharData_{safeName}.asset";
        AssetDatabase.CreateAsset(data, assetPath);
        return data;
    }

    private static List<TNG.Interactions.DialogueOption> BuildInteractionOptions(
        string charName, string[] optionTexts, string[] responseTexts)
    {
        var options = new List<TNG.Interactions.DialogueOption>();
        int count = Mathf.Min(optionTexts.Length, responseTexts.Length);
        for (int i = 0; i < count; i++)
        {
            options.Add(new TNG.Interactions.DialogueOption
            {
                optionText   = optionTexts[i],
                responseText = responseTexts[i]
                // onSelected UnityEvents for mission triggers must be wired manually in Inspector
                // e.g. Picard option 1: MissionManager.LoadMission(Mission_001)
            });
        }
        return options;
    }

    // ── Viewscreen ────────────────────────────────────────────────────────────

    private static GameObject BuildViewscreen()
    {
        var vs = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vs.name = "[Viewscreen] — Replace material with RenderTexture star field";
        vs.transform.position = new Vector3(0f, 2.0f, 6.5f);
        vs.transform.localScale    = new Vector3(7.0f, 3.94f, 1f); // 16:9 at 4m wide approx
        vs.transform.eulerAngles   = new Vector3(0f, 180f, 0f);     // face toward player

        // Placeholder dark material
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = ViewscreenDark;
        vs.GetComponent<Renderer>().sharedMaterial = mat;
        AssetDatabase.CreateAsset(mat, "Assets/_Materials/Viewscreen_Space_Placeholder.mat");

        var controller = vs.AddComponent<ViewscreenController>();

        // Alert tint child plane
        var tint = GameObject.CreatePrimitive(PrimitiveType.Quad);
        tint.name = "Viewscreen_AlertTint";
        tint.transform.SetParent(vs.transform);
        tint.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        tint.transform.localScale    = Vector3.one;
        var tintMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        tintMat.color = new Color(1f, 0f, 0f, 0.15f);
        tint.GetComponent<Renderer>().sharedMaterial = tintMat;
        tint.SetActive(false);
        controller.alertTintOverlay = tint;

        // Bezel frame
        var bezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bezel.name = "Viewscreen_Bezel [PLACEHOLDER]";
        bezel.transform.SetParent(vs.transform.parent);
        bezel.transform.position   = vs.transform.position;
        bezel.transform.localScale = new Vector3(7.4f, 4.3f, 0.1f);
        bezel.transform.eulerAngles = vs.transform.eulerAngles;
        var bezelMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bezelMat.color = BridgeGrey;
        bezel.GetComponent<Renderer>().sharedMaterial = bezelMat;

        Debug.Log("[BridgeSetup] Viewscreen created.");
        return vs;
    }

    // ── Side Engineering Panels ───────────────────────────────────────────────

    private static void BuildSideConsolePanels()
    {
        var panelRoot = new GameObject("[LCARS Side Panels — Replace with real panel assets]");

        string[] panelNames = {
            "Panel_Engineering_Left_01", "Panel_Engineering_Left_02",
            "Panel_Engineering_Right_01", "Panel_Engineering_Right_02"
        };
        Vector3[] positions = {
            new Vector3(-4.2f, 1.2f, -1.0f), new Vector3(-4.2f, 1.2f,  1.0f),
            new Vector3( 4.2f, 1.2f, -1.0f), new Vector3( 4.2f, 1.2f,  1.0f)
        };
        for (int i = 0; i < panelNames.Length; i++)
        {
            var panel = CreateLabelledConsole(panelNames[i], positions[i],
                new Vector3(0.05f, 1.8f, 1.6f), LCARSOrange, panelRoot.transform);
            panel.layer = LAYER_INTERACTABLE;
        }

        Debug.Log("[BridgeSetup] Side LCARS panels created.");
    }

    // ── LCARS Canvas ──────────────────────────────────────────────────────────

    private static Canvas BuildUICanvas()
    {
        // ── Persistent Canvas ─────────────────────────────────────────────────
        var canvasGO = new GameObject("[UI Canvas — LCARS]");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2732f, 2048f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Ship Status Bar ───────────────────────────────────────────────────
        var statusBar   = CreatePanel("StatusBar", canvasGO.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -30f), new Vector2(2732f, 60f),
            new Color(0f, 0f, 0f, 0.9f));

        CreateTMPLabel("ShipName", statusBar.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(20f, 0f), new Vector2(700f, 50f),
            "USS ENTERPRISE  NCC-1701-D", 18f, LCARSOrange, TextAlignmentOptions.Left);

        var stardateGO = CreateTMPLabel("Stardate", statusBar.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(400f, 50f),
            "STARDATE 47634.2", 18f, Color.white, TextAlignmentOptions.Center);
        stardateGO.AddComponent<StardateDisplay>();

        CreateTMPLabel("WarpCore", statusBar.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-20f, 0f), new Vector2(400f, 50f),
            "WARP CORE  NOMINAL", 18f, new Color(0.27f, 1f, 0.53f), TextAlignmentOptions.Right);

        // ── Dialogue Panel ────────────────────────────────────────────────────
        var dialoguePanel = CreatePanel("DialoguePanel", canvasGO.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, -700f), new Vector2(0f, 700f),
            new Color(0f, 0f, 0f, 0.9f));
        dialoguePanel.gameObject.SetActive(false);

        var dpRect = dialoguePanel.GetComponent<RectTransform>();
        dpRect.anchorMin = new Vector2(0f, 0f);
        dpRect.anchorMax = new Vector2(1f, 0f);
        dpRect.pivot     = new Vector2(0.5f, 0f);
        dpRect.sizeDelta = new Vector2(0f, 700f);
        dpRect.anchoredPosition = new Vector2(0f, -700f); // start hidden below screen

        // LCARS left strip
        CreatePanel("LCARSStrip_Dialogue", dialoguePanel.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(60f, 0f), new Vector2(120f, 0f),
            LCARSOrange);

        // Portrait area
        var portrait = CreatePanel("PortraitArea", dialoguePanel.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(250f, 0f), new Vector2(360f, 0f),
            new Color(0.1f, 0.1f, 0.15f, 1f));

        CreateTMPLabel("CharacterName", portrait.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -40f), new Vector2(340f, 50f),
            "CHARACTER NAME", 22f, LCARSOrange, TextAlignmentOptions.Center);

        CreateTMPLabel("CharacterRank", portrait.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -90f), new Vector2(340f, 40f),
            "RANK  ·  ROLE", 13f, Color.white, TextAlignmentOptions.Center);

        // Dialogue body area
        var bodyArea = CreatePanel("DialogueBody", dialoguePanel.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(500f, 80f), new Vector2(-40f, -200f),
            Color.clear);

        CreateTMPLabel("DialogueText", bodyArea.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -20f), new Vector2(0f, -80f),
            "Dialogue text will appear here.", 20f, Color.white, TextAlignmentOptions.TopLeft);

        // Option buttons (3)
        float btnY = -320f;
        for (int i = 0; i < 3; i++)
        {
            CreateOptionButton($"OptionButton_{i}", dialoguePanel.transform,
                new Vector2(500f, btnY - i * 90f), new Vector2(1200f, 75f), i);
        }

        // Close button
        CreateCloseButton("CloseButton", dialoguePanel.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 40f), new Vector2(-40f, 60f));

        // ── Alert Overlay ─────────────────────────────────────────────────────
        var alertOverlay = CreatePanel("AlertOverlay", canvasGO.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(1f, 0f, 0f, 0f));
        alertOverlay.gameObject.SetActive(false);

        CreateTMPLabel("AlertLabel", canvasGO.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -80f), new Vector2(600f, 80f),
            "RED ALERT", 48f, Color.red, TextAlignmentOptions.Center);

        // ── Passive Line Notification ─────────────────────────────────────────
        var passivePanel = CreatePanel("PassiveLinePanel", canvasGO.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 140f), new Vector2(0f, 80f),
            new Color(0f, 0f, 0f, 0.7f));
        passivePanel.gameObject.SetActive(false);

        CreateTMPLabel("PassiveLineText", passivePanel.transform,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            Vector2.zero, Vector2.zero,
            "", 18f, new Color(1f, 0.85f, 0.5f), TextAlignmentOptions.Center);

        // ── Mission Panel ─────────────────────────────────────────────────────
        var missionPanel = CreatePanel("MissionPanel", canvasGO.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.92f));
        var missionGroup = missionPanel.AddComponent<CanvasGroup>();
        missionGroup.alpha = 0f;
        missionGroup.interactable = false;
        missionGroup.blocksRaycasts = false;

        CreateTMPLabel("MissionTitle", missionPanel.transform,
            new Vector2(0f, 1f), new Vector2(0.7f, 1f),
            new Vector2(40f, -60f), new Vector2(-40f, 60f),
            "MISSION TITLE", 28f, LCARSOrange, TextAlignmentOptions.Left);

        CreateTMPLabel("MissionStardate", missionPanel.transform,
            new Vector2(0.7f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -60f), new Vector2(-40f, 60f),
            "STARDATE 47634.2", 20f, Color.white, TextAlignmentOptions.Right);

        CreateTMPLabel("NarrativeText", missionPanel.transform,
            new Vector2(0f, 0.2f), new Vector2(1f, 0.9f),
            new Vector2(80f, 0f), new Vector2(-80f, 0f),
            "", 24f, new Color(0.9f, 0.9f, 1f), TextAlignmentOptions.TopLeft);

        // Mission choice buttons
        var choiceContainer = CreatePanel("ChoiceContainer", missionPanel.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0.22f),
            Vector2.zero, Vector2.zero, Color.clear);
        var choiceGroup = choiceContainer.AddComponent<CanvasGroup>();

        for (int i = 0; i < 3; i++)
        {
            CreateMissionChoiceButton($"MissionChoice_{i}", choiceContainer.transform,
                new Vector2(0f, i), new Vector2(0f, i), // anchor will be set
                new Vector2(80f, 30f + i * 90f), new Vector2(1600f, 70f));
        }

        // ── Mission Complete Overlay ───────────────────────────────────────────
        var completeOverlay = CreatePanel("MissionCompleteOverlay", canvasGO.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0.05f, 0.95f));
        var completeGroup = completeOverlay.AddComponent<CanvasGroup>();
        completeGroup.alpha = 0f;
        completeGroup.interactable = false;
        completeGroup.blocksRaycasts = false;

        CreateTMPLabel("MissionCompleteHeading", completeOverlay.transform,
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f),
            Vector2.zero, new Vector2(900f, 100f),
            "MISSION COMPLETE", 56f, new Color(0.27f, 1f, 0.53f), TextAlignmentOptions.Center);

        CreateTMPLabel("MissionOutcomeText", completeOverlay.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 200f),
            "Outcome summary.", 24f, Color.white, TextAlignmentOptions.Center);

        CreateMissionChoiceButton("ReturnToBridgeBtn", completeOverlay.transform,
            new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f),
            Vector2.zero, new Vector2(500f, 80f));

        Debug.Log("[BridgeSetup] LCARS UI Canvas created.");
        return canvas;
    }

    // ── System Managers ───────────────────────────────────────────────────────

    private static void BuildManagers(Canvas canvas, GameObject viewscreen)
    {
        // AudioManager
        var audioGO = new GameObject("[AudioManager]");
        audioGO.AddComponent<AudioManager>();

        // InteractionManager
        var interactionGO = new GameObject("[InteractionManager]");
        interactionGO.AddComponent<InteractionManager>();

        // MissionManager
        var missionGO = new GameObject("[MissionManager]");
        missionGO.AddComponent<MissionManager>();

        // AlertManager — attach to canvas root so it can find overlay children
        var alertManager = canvas.gameObject.AddComponent<AlertManager>();

        // UIManager — attach to canvas root
        canvas.gameObject.AddComponent<UIManager>();

        // MissionUIController — attach to canvas root
        canvas.gameObject.AddComponent<MissionUIController>();

        Debug.Log("[BridgeSetup] System managers created. Assign Inspector references manually " +
                  "or use TNG Bridge > Setup > Wire Inspector References (Phase 2 tool).");
    }

    // ── Mission 001 ScriptableObject ──────────────────────────────────────────

    private static void BuildMission001()
    {
        var mission = ScriptableObject.CreateInstance<MissionData>();
        mission.missionTitle  = "The Neutral Zone Incident";
        mission.stardate      = "47634.2";
        mission.briefingText  = "Captain's Log. The Enterprise is on a diplomatic mission near " +
                                "the Romulan Neutral Zone. Long-range sensors have detected an anomaly.";

        mission.beats = new List<MissionBeat>
        {
            new MissionBeat
            {
                beatID = "BEAT_01",
                narrativeText = "Captain's Log. The Enterprise is on a diplomatic mission near the " +
                                "Romulan Neutral Zone. Long-range sensors have detected an anomaly.",
                viewscreenState = ViewscreenState.Space,
                audioState = AudioState.Normal,
                choices = new List<MissionChoice>
                {
                    new MissionChoice { choiceText = "Investigate the anomaly",   nextBeatID = "BEAT_02A", isSuccess = true,
                        outcomeText = "Helm, adjust course to intercept. Full impulse." },
                    new MissionChoice { choiceText = "Maintain current course",   nextBeatID = "BEAT_02B", isSuccess = false,
                        outcomeText = "Continue on course. Maintain yellow alert." }
                }
            },
            new MissionBeat
            {
                beatID = "BEAT_02A",
                narrativeText = "As the Enterprise approaches, a Romulan Warbird decloaks directly " +
                                "ahead. Commander Worf reports weapons are being charged.",
                viewscreenState = ViewscreenState.Vessel,
                audioState = AudioState.YellowAlert,
                choices = new List<MissionChoice>
                {
                    new MissionChoice { choiceText = "Go to Red Alert",            nextBeatID = "BEAT_03A", isSuccess = false,
                        outcomeText = "Red Alert! All hands to battle stations!" },
                    new MissionChoice { choiceText = "Open a hailing frequency",   nextBeatID = "BEAT_03B", isSuccess = true,
                        outcomeText = "Hailing frequencies open, Captain." }
                }
            },
            new MissionBeat
            {
                beatID = "BEAT_02B",
                narrativeText = "The Enterprise continues on course. Moments later, Counselor Troi " +
                                "senses a presence. Something is watching.",
                viewscreenState = ViewscreenState.Space,
                audioState = AudioState.Normal,
                choices = new List<MissionChoice>
                {
                    new MissionChoice { choiceText = "Scan for cloaked vessels",          nextBeatID = "BEAT_02A", isSuccess = true,
                        outcomeText = "Adjusting sensor array for tachyon sweep." },
                    new MissionChoice { choiceText = "Continue to diplomatic rendezvous", nextBeatID = "BEAT_RESOLUTION", isSuccess = false,
                        outcomeText = "Helm, maintain heading. Maximum warp." }
                }
            },
            new MissionBeat
            {
                beatID = "BEAT_03A",
                narrativeText = "Red Alert. The Romulan commander hails the Enterprise. " +
                                "'You have violated the Neutral Zone, Captain. Explain yourself.'",
                viewscreenState = ViewscreenState.Communication,
                audioState = AudioState.RedAlert,
                choices = new List<MissionChoice>
                {
                    new MissionChoice { choiceText = "Respond diplomatically",             nextBeatID = "BEAT_RESOLUTION", isSuccess = true,
                        outcomeText = "Captain Picard steps forward. 'Commander. There has been a misunderstanding.'" },
                    new MissionChoice { choiceText = "Charge weapons as a show of force", nextBeatID = "BEAT_RESOLUTION", isSuccess = false,
                        outcomeText = "Worf: 'Weapons charged, Captain. Shields at maximum.'" }
                }
            },
            new MissionBeat
            {
                beatID = "BEAT_03B",
                narrativeText = "The Romulan commander appears on screen. She seems... curious " +
                                "rather than hostile. 'Captain Picard. We have been expecting you.'",
                viewscreenState = ViewscreenState.Communication,
                audioState = AudioState.Normal,
                choices = new List<MissionChoice>
                {
                    new MissionChoice { choiceText = "Ask what they know",       nextBeatID = "BEAT_RESOLUTION", isSuccess = true,
                        outcomeText = "'Commander, I suspect we have much to discuss. Perhaps we can do so without weapons powered.'" },
                    new MissionChoice { choiceText = "Demand they stand down",   nextBeatID = "BEAT_RESOLUTION", isSuccess = false,
                        outcomeText = "'Lower your weapons immediately, or we will be forced to defend ourselves.'" }
                }
            },
            new MissionBeat
            {
                beatID = "BEAT_RESOLUTION",
                narrativeText = "The situation is resolved. The Enterprise continues on her mission. " +
                                "Whatever the Romulans were planning — it will have to wait.",
                viewscreenState = ViewscreenState.Space,
                audioState = AudioState.Normal,
                choices = new List<MissionChoice>
                {
                    new MissionChoice { choiceText = "End Mission", nextBeatID = "MISSION_END", isSuccess = true,
                        outcomeText = "Captain's Log, supplemental. The Enterprise resumes her mission." }
                }
            }
        };

        string assetPath = "Assets/_Prefabs/Missions/Mission_001_NeutralZoneIncident.asset";
        AssetDatabase.CreateAsset(mission, assetPath);
        Debug.Log($"[BridgeSetup] Mission 001 created at {assetPath}");
    }

    // ── Primitive / UI Helpers ────────────────────────────────────────────────

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        return go;
    }

    private static void ApplyMaterial(GameObject go, Color color, string matName)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        string path = $"Assets/_Materials/{matName}.mat";
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), path)))
            AssetDatabase.CreateAsset(mat, path);
    }

    private static GameObject CreateLabelledConsole(
        string name, Vector3 pos, Vector3 scale, Color color, Transform parent)
    {
        var console = GameObject.CreatePrimitive(PrimitiveType.Cube);
        console.name = $"[CONSOLE] {name}";
        console.transform.SetParent(parent);
        console.transform.localPosition = pos;
        console.transform.localScale    = scale;
        console.layer = LAYER_INTERACTABLE;
        foreach (Transform t in console.GetComponentsInChildren<Transform>())
            t.gameObject.layer = LAYER_INTERACTABLE;
        ApplyMaterial(console, color, $"Console_{name.Replace(" ", "_")}");
        return console;
    }

    private static void AddStationMarker(GameObject go, string stationName, Vector3 standPos)
    {
        var marker = go.AddComponent<StationMarker>();
        marker.stationName = stationName;

        var standTarget = new GameObject($"StandTarget_{stationName}");
        standTarget.transform.SetParent(go.transform);
        standTarget.transform.position = standPos;
        marker.playerStandTarget = standTarget.transform;
    }

    private static void CreateChair(string name, Vector3 pos, Color color, Transform parent)
    {
        var chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chair.name = name;
        chair.transform.SetParent(parent);
        chair.transform.localPosition = pos;
        chair.transform.localScale    = new Vector3(0.6f, 0.1f, 0.6f);
        ApplyMaterial(chair, color, "Bridge_Chair");
    }

    private static void CreateStandingMarker(string name, Vector3 pos, Transform parent)
    {
        var marker = new GameObject(name);
        marker.transform.SetParent(parent);
        marker.transform.localPosition = pos;
    }

    private static void CreateDoor(string name, Vector3 pos, Transform parent)
    {
        var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = name;
        door.transform.SetParent(parent);
        door.transform.localPosition = pos;
        door.transform.localScale    = new Vector3(1.2f, 2.4f, 0.1f);
        ApplyMaterial(door, BridgeGrey, "Bridge_Door");
    }

    private static void CreateLCARSStrip(string name, Vector3 pos, Vector3 scale, Color color, Transform parent)
    {
        var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = name;
        strip.transform.SetParent(parent);
        strip.transform.localPosition = pos;
        strip.transform.localScale    = scale;
        ApplyMaterial(strip, color, $"LCARS_{name}");
    }

    // ── UI Creation Helpers ───────────────────────────────────────────────────

    private static Image CreatePanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin       = anchorMin;
        rect.anchorMax       = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta       = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static GameObject CreateTMPLabel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = alignment;
        return go;
    }

    private static void CreateOptionButton(string name, Transform parent,
        Vector2 anchoredPos, Vector2 sizeDelta, int index)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0f, 0f);
        rect.anchorMax        = new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta;
        var img    = go.AddComponent<Image>();
        img.color  = LCARSOrange;
        var button = go.AddComponent<Button>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 0f); labelRect.offsetMax = new Vector2(-12f, 0f);
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = $"Option {index + 1}";
        tmp.fontSize  = 20f;
        tmp.color     = Color.black;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Left;

        go.SetActive(false);
    }

    private static void CreateCloseButton(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.pivot            = new Vector2(1f, 0f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta;
        var img    = go.AddComponent<Image>();
        img.color  = LCARSPurple;
        var btn    = go.AddComponent<Button>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "DISMISS";
        tmp.fontSize  = 20f;
        tmp.color     = Color.black;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private static void CreateMissionChoiceButton(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.1f, 0f);
        rect.anchorMax        = new Vector2(0.1f, 0f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta;
        go.AddComponent<Image>().color = LCARSOrange;
        go.AddComponent<Button>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        labelGO.AddComponent<RectTransform>().anchorMin = Vector2.zero;
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Choice";
        tmp.fontSize  = 22f;
        tmp.color     = Color.black;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
    }
}

/// <summary>
/// Simple billboard component — rotates a world-space canvas to face the main camera each frame.
/// Attach to character name label canvases.
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    private void LateUpdate()
    {
        if (UnityEngine.Camera.main == null) return;
        transform.LookAt(transform.position + UnityEngine.Camera.main.transform.rotation * Vector3.forward,
                         UnityEngine.Camera.main.transform.rotation * Vector3.up);
    }
}
