using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Dựng model greybox và animation prototype bằng khối cơ bản, sau này thay bằng model thật
public static class PrototypeModelBuilder
{
    private const string PrefabRoot = "Assets/Prefabs/Prototype";
    private const string MaterialRoot = "Assets/Arts/Materials/Prototype";
    private const string AnimationRoot = "Assets/Arts/Animations/Prototype";
    private const string ShowcaseScenePath = "Assets/Scenes/PrototypeModels.unity";
    private const float FrameRate = 30f;

    // Thứ tự các hàng trong scene trưng bày, gần camera trước
    private static readonly string[] ShowcaseOrder = { "Food", "Items", "PcParts", "StreamSetup", "Minigames", "Furniture", "Characters", "Vehicles", "World" };

    // Các tông xám dùng chung cho mọi model
    private enum Tone { Light, Mid, Dark, Screen, Accent, Ground }

    // Trục quay của khối trụ
    private enum Axis { X, Y, Z }

    // Cách gắn collider khi lưu prefab
    private enum ColliderMode { Bounds, Parts, None }

    // Giá trị tham số Action của nhân vật
    private static readonly (int value, string name, float length)[] CharacterActions =
    {
        (1, "Sit", 2f),
        (2, "Typing", 1f),
        (3, "Sleep", 4f),
        (4, "Talk", 2f),
        (5, "Dance", 1f),
        (6, "Eat", 1.5f),
    };

    private static readonly Dictionary<Tone, Material> materials = new();
    private static readonly Dictionary<string, RuntimeAnimatorController> controllers = new();

    [MenuItem("Tools/Prototype/Build Models And Animations")]
    public static void BuildAll()
    {
        var previous = SceneManager.GetActiveScene();
        var temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(temp);
        try
        {
            BuildMaterials();
            BuildAnimations();
            BuildCharacters();
            BuildPcParts();
            BuildStreamSetup();
            BuildFurniture();
            BuildFood();
            BuildItems();
            BuildVehicles();
            BuildWorld();
            BuildMinigames();
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(temp, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        Debug.Log("[Prototype] Đã dựng xong model và animation prototype");
    }

    #region Materials

    private static void BuildMaterials()
    {
        EnsureFolder(MaterialRoot);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var palette = new (Tone tone, Color color, float smoothness)[]
        {
            (Tone.Light, new Color(0.86f, 0.86f, 0.86f), 0.2f),
            (Tone.Mid, new Color(0.62f, 0.62f, 0.62f), 0.2f),
            (Tone.Dark, new Color(0.32f, 0.32f, 0.32f), 0.25f),
            (Tone.Screen, new Color(0.1f, 0.1f, 0.12f), 0.8f),
            (Tone.Accent, new Color(0.48f, 0.56f, 0.68f), 0.3f),
            (Tone.Ground, new Color(0.74f, 0.74f, 0.72f), 0.1f),
        };

        foreach (var (tone, color, smoothness) in palette)
        {
            var path = $"{MaterialRoot}/Proto_{tone}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            materials[tone] = material;
        }
    }

    #endregion

    #region Animations

    // Một tư thế của rig nhân vật, góc tính bằng độ
    private struct Pose
    {
        public Vector3 HipsPosition;
        public Vector3 Hips, Spine, Neck, ArmL, ArmR, LegL, LegR, KneeL, KneeR;
    }

    // Giá trị của một xương tại một thời điểm
    private readonly struct BoneKey
    {
        public readonly string Path;
        public readonly Quaternion Rotation;
        public readonly Vector3? Position;

        public BoneKey(string path, Vector3 euler, Vector3? position = null)
        {
            Path = path;
            Rotation = Quaternion.Euler(euler);
            Position = position;
        }
    }

    private static void BuildAnimations()
    {
        EnsureFolder(AnimationRoot);
        controllers.Clear();
        controllers["Character"] = BuildCharacterController();

        var door = HingeClips("Door", Vector3.up, -95f);
        var openable = ToggleController("Openable", "IsOpen", door.idle, door.open, door.close);
        controllers["Door"] = openable;
        var fridge = HingeClips("Fridge", Vector3.up, 95f);
        controllers["Fridge"] = OverrideController("Fridge", openable, (door.idle, fridge.idle), (door.open, fridge.open), (door.close, fridge.close));
        var toilet = HingeClips("Toilet", Vector3.right, -95f);
        controllers["Toilet"] = OverrideController("Toilet", openable, (door.idle, toilet.idle), (door.open, toilet.open), (door.close, toilet.close));

        var slot = SpinnerClips("SlotMachine", Vector3.right, 360f, 0.5f);
        var spinner = ToggleController("Spinner", "IsSpinning", slot.idle, slot.spin, null);
        controllers["SlotMachine"] = spinner;
        var wheel = SpinnerClips("LuckySpinMachine", Vector3.forward, -360f, 2f);
        controllers["LuckySpinMachine"] = OverrideController("LuckySpinMachine", spinner, (slot.idle, wheel.idle), (slot.spin, wheel.spin));
    }

    private static Pose Rest()
    {
        return new Pose
        {
            HipsPosition = new Vector3(0f, 0.95f, 0f),
            ArmL = new Vector3(0f, 0f, -5f),
            ArmR = new Vector3(0f, 0f, 5f),
        };
    }

    private static Pose Seated(float breath)
    {
        var pose = Rest();
        pose.HipsPosition = new Vector3(0f, 0.55f, 0f);
        pose.LegL.x = pose.LegR.x = -90f;
        pose.KneeL.x = pose.KneeR.x = 90f;
        pose.Spine.x = -4f + breath;
        pose.ArmL = new Vector3(-20f, 0f, -8f);
        pose.ArmR = new Vector3(-20f, 0f, 8f);
        return pose;
    }

    private static float Wave(float phase, float cycles = 1f, float offset = 0f)
    {
        return Mathf.Sin((phase * cycles + offset) * Mathf.PI * 2f);
    }

    private static BoneKey[] Keys(Pose pose)
    {
        return new[]
        {
            new BoneKey("Rig/Hips", pose.Hips, pose.HipsPosition),
            new BoneKey("Rig/Hips/Spine", pose.Spine),
            new BoneKey("Rig/Hips/Spine/Neck", pose.Neck),
            new BoneKey("Rig/Hips/Spine/ShoulderL", pose.ArmL),
            new BoneKey("Rig/Hips/Spine/ShoulderR", pose.ArmR),
            new BoneKey("Rig/Hips/LegL", pose.LegL),
            new BoneKey("Rig/Hips/LegR", pose.LegR),
            new BoneKey("Rig/Hips/LegL/KneeL", pose.KneeL),
            new BoneKey("Rig/Hips/LegR/KneeR", pose.KneeR),
        };
    }

    private static Pose Stride(float phase, float swing, float knee, float arms, float lean, float bob)
    {
        var pose = Rest();
        var a = Wave(phase);
        var c = Mathf.Cos(phase * Mathf.PI * 2f);
        pose.HipsPosition.y += bob * Mathf.Cos(phase * Mathf.PI * 4f);
        pose.Hips.y = 5f * a;
        pose.Spine.x = lean;
        pose.LegL.x = -swing * a;
        pose.LegR.x = swing * a;
        pose.KneeL.x = 5f + knee * Mathf.Max(0f, c);
        pose.KneeR.x = 5f + knee * Mathf.Max(0f, -c);
        pose.ArmL = new Vector3(arms * a, 0f, -6f);
        pose.ArmR = new Vector3(-arms * a, 0f, 6f);
        return pose;
    }

    private static Pose CharacterPose(string clip, float p)
    {
        var s = Wave(p);
        Pose pose;
        switch (clip)
        {
            case "Walk":
                return Stride(p, 25f, 35f, 25f, 4f, 0.02f);
            case "Run":
                return Stride(p, 45f, 70f, 50f, 12f, 0.04f);
            case "Sit":
                return Seated(s);
            case "Typing":
                pose = Seated(0f);
                pose.Spine.x = 6f;
                pose.Neck.x = 6f;
                pose.ArmL = new Vector3(-62f + 3f * Wave(p, 2f), 0f, 12f);
                pose.ArmR = new Vector3(-62f + 3f * Wave(p, 2f, 0.5f), 0f, -12f);
                return pose;
            case "Sleep":
                pose = Rest();
                pose.HipsPosition = new Vector3(0f, 0.62f, 0f);
                pose.Hips.x = -90f;
                pose.Spine.x = 1.5f * s;
                pose.ArmL.z = -8f;
                pose.ArmR.z = 8f;
                return pose;
            case "Talk":
                pose = Rest();
                pose.Neck = new Vector3(4f * Wave(p, 3f), 10f * s, 0f);
                pose.ArmR = new Vector3(-35f + 12f * Wave(p, 2f), 0f, 8f);
                return pose;
            case "Dance":
                pose = Rest();
                var b = Mathf.Abs(Wave(p, 2f));
                pose.HipsPosition.y -= 0.03f * b;
                pose.Hips.y = 15f * s;
                pose.Spine.z = 8f * s;
                pose.Neck.z = -6f * s;
                pose.LegL.x = pose.LegR.x = -8f * b;
                pose.KneeL.x = pose.KneeR.x = 15f * b;
                pose.ArmL = new Vector3(0f, 0f, -110f - 30f * s);
                pose.ArmR = new Vector3(0f, 0f, 110f - 30f * s);
                return pose;
            case "Eat":
                pose = Rest();
                pose.Neck.x = 6f + 3f * s;
                pose.ArmR = new Vector3(-125f + 10f * s, 0f, -15f);
                return pose;
            default:
                pose = Rest();
                pose.HipsPosition.y += 0.006f * s;
                pose.Spine.x = 1.5f * s;
                pose.ArmL.z = -5f - s;
                pose.ArmR.z = 5f + s;
                return pose;
        }
    }

    private static AnimationClip CharacterClip(string name, float length)
    {
        return WriteClip($"Character_{name}", length, true, p => Keys(CharacterPose(name, p)));
    }

    private static AnimatorController BuildCharacterController()
    {
        var idle = CharacterClip("Idle", 2f);
        var walk = CharacterClip("Walk", 1f);
        var run = CharacterClip("Run", 0.6f);

        var path = $"{AnimationRoot}/Character.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Action", AnimatorControllerParameterType.Int);

        var locomotion = controller.CreateBlendTreeInController("Locomotion", out var tree, 0);
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(idle, 0f);
        tree.AddChild(walk, 1.5f);
        tree.AddChild(run, 4.5f);

        var stateMachine = controller.layers[0].stateMachine;
        stateMachine.defaultState = locomotion;
        foreach (var (value, name, length) in CharacterActions)
        {
            var state = stateMachine.AddState(name);
            state.motion = CharacterClip(name, length);

            var enter = stateMachine.AddAnyStateTransition(state);
            enter.canTransitionToSelf = false;
            enter.hasExitTime = false;
            enter.duration = 0.2f;
            enter.AddCondition(AnimatorConditionMode.Equals, value, "Action");

            var exit = state.AddTransition(locomotion);
            exit.hasExitTime = false;
            exit.duration = 0.2f;
            exit.AddCondition(AnimatorConditionMode.Equals, 0, "Action");
        }
        return controller;
    }

    private static (AnimationClip idle, AnimationClip open, AnimationClip close) HingeClips(string prop, Vector3 axis, float angle)
    {
        var idle = WriteClip($"{prop}_Closed", 1f / FrameRate, false, _ => new[] { new BoneKey("Hinge", Vector3.zero) });
        var open = WriteClip($"{prop}_Open", 0.4f, false, p => new[] { new BoneKey("Hinge", axis * (angle * Mathf.SmoothStep(0f, 1f, p))) });
        var close = WriteClip($"{prop}_Close", 0.4f, false, p => new[] { new BoneKey("Hinge", axis * (angle * Mathf.SmoothStep(1f, 0f, p))) });
        return (idle, open, close);
    }

    private static (AnimationClip idle, AnimationClip spin) SpinnerClips(string prop, Vector3 axis, float degrees, float length)
    {
        var idle = WriteClip($"{prop}_Idle", 1f / FrameRate, false, _ => new[] { new BoneKey("Spinner", Vector3.zero) });
        var spin = WriteClip($"{prop}_Spin", length, true, p => new[] { new BoneKey("Spinner", axis * (degrees * p)) });
        return (idle, spin);
    }

    private static AnimationClip WriteClip(string name, float length, bool loop, Func<float, BoneKey[]> sample)
    {
        var path = $"{AnimationRoot}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.frameRate = FrameRate;

        var curves = new Dictionary<(string path, string property), AnimationCurve>();
        void Add(string bone, string property, float time, float value)
        {
            if (!curves.TryGetValue((bone, property), out var curve))
                curves[(bone, property)] = curve = new AnimationCurve();
            curve.AddKey(time, value);
        }

        var frames = Mathf.Max(1, Mathf.RoundToInt(length * FrameRate));
        for (var i = 0; i <= frames; i++)
        {
            var phase = (float)i / frames;
            var time = length * phase;
            foreach (var key in sample(phase))
            {
                Add(key.Path, "localRotation.x", time, key.Rotation.x);
                Add(key.Path, "localRotation.y", time, key.Rotation.y);
                Add(key.Path, "localRotation.z", time, key.Rotation.z);
                Add(key.Path, "localRotation.w", time, key.Rotation.w);
                if (key.Position is Vector3 position)
                {
                    Add(key.Path, "localPosition.x", time, position.x);
                    Add(key.Path, "localPosition.y", time, position.y);
                    Add(key.Path, "localPosition.z", time, position.z);
                }
            }
        }

        foreach (var pair in curves)
            clip.SetCurve(pair.Key.path, typeof(Transform), pair.Key.property, pair.Value);
        clip.EnsureQuaternionContinuity();

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController ToggleController(string name, string parameter, AnimationClip idle, AnimationClip on, AnimationClip off)
    {
        var path = $"{AnimationRoot}/{name}.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter(parameter, AnimatorControllerParameterType.Bool);

        var stateMachine = controller.layers[0].stateMachine;
        var idleState = stateMachine.AddState("Idle");
        idleState.motion = idle;
        var onState = stateMachine.AddState("On");
        onState.motion = on;
        stateMachine.defaultState = idleState;

        Link(idleState, onState, parameter, true);
        if (off == null)
        {
            Link(onState, idleState, parameter, false);
            return controller;
        }

        var offState = stateMachine.AddState("Off");
        offState.motion = off;
        Link(onState, offState, parameter, false);
        Link(offState, onState, parameter, true);
        return controller;
    }

    private static void Link(AnimatorState from, AnimatorState to, string parameter, bool value)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static AnimatorOverrideController OverrideController(string name, AnimatorController baseController, params (AnimationClip from, AnimationClip to)[] pairs)
    {
        var path = $"{AnimationRoot}/{name}.overrideController";
        AssetDatabase.DeleteAsset(path);
        var controller = new AnimatorOverrideController(baseController);
        controller.ApplyOverrides(pairs.Select(pair => new KeyValuePair<AnimationClip, AnimationClip>(pair.from, pair.to)).ToList());
        AssetDatabase.CreateAsset(controller, path);
        return controller;
    }

    #endregion

    #region Model helpers

    // Prefab đang dựng, mọi tọa độ tính theo world với gốc ở giữa đáy model
    private sealed class Model
    {
        public readonly GameObject Root;

        public Model(string name)
        {
            Root = new GameObject($"Proto_{name}");
        }

        public Transform Pivot(string name, Vector3 position, Transform parent = null)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent ? parent : Root.transform, false);
            pivot.position = position;
            return pivot;
        }

        public Transform Shape(PrimitiveType type, string name, Vector3 center, Vector3 scale, Tone tone, Transform parent = null, Vector3 euler = default, bool collider = false)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = $"Mesh_{name}";
            if (!collider)
                UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent ? parent : Root.transform, false);
            part.transform.SetPositionAndRotation(center, Quaternion.Euler(euler));
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = materials[tone];
            return part.transform;
        }

        public Transform Box(string name, Vector3 center, Vector3 size, Tone tone, Transform parent = null, Vector3 euler = default, bool collider = false)
        {
            return Shape(PrimitiveType.Cube, name, center, size, tone, parent, euler, collider);
        }

        public Transform Cylinder(string name, Vector3 center, float radius, float height, Tone tone, Axis axis = Axis.Y, Transform parent = null, bool collider = false)
        {
            var euler = axis == Axis.X ? new Vector3(0f, 0f, 90f) : axis == Axis.Z ? new Vector3(90f, 0f, 0f) : Vector3.zero;
            return Shape(PrimitiveType.Cylinder, name, center, new Vector3(radius * 2f, height * 0.5f, radius * 2f), tone, parent, euler, collider);
        }

        public Transform Capsule(string name, Vector3 center, float radius, float height, Tone tone, Transform parent = null, Vector3 euler = default)
        {
            return Shape(PrimitiveType.Capsule, name, center, new Vector3(radius * 2f, height * 0.5f, radius * 2f), tone, parent, euler);
        }

        public Transform Sphere(string name, Vector3 center, Vector3 size, Tone tone, Transform parent = null)
        {
            return Shape(PrimitiveType.Sphere, name, center, size, tone, parent);
        }
    }

    private static void Save(Model model, string category, ColliderMode colliders = ColliderMode.Bounds, string controller = null)
    {
        var root = model.Root;
        if (colliders == ColliderMode.Bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            var box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
        }

        if (controller != null)
        {
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controllers[controller];
            animator.applyRootMotion = false;
        }

        var folder = $"{PrefabRoot}/{category}";
        EnsureFolder(folder);
        PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/{root.name}.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    #endregion

    #region Characters

    private static void BuildCharacters()
    {
        var player = Humanoid("Player", Tone.Mid, Tone.Dark);
        Save(player.model, "Characters", ColliderMode.None, "Character");

        var civil = Humanoid("NpcCivil", Tone.Light, Tone.Mid);
        civil.model.Box("Hair", new Vector3(0f, 1.81f, -0.01f), new Vector3(0.27f, 0.08f, 0.27f), Tone.Dark, civil.neck);
        Save(civil.model, "Characters", ColliderMode.None, "Character");

        var courier = Humanoid("NpcCourier", Tone.Accent, Tone.Mid);
        courier.model.Box("Cap", new Vector3(0f, 1.83f, 0.01f), new Vector3(0.27f, 0.06f, 0.28f), Tone.Accent, courier.neck);
        courier.model.Box("CapBrim", new Vector3(0f, 1.81f, 0.17f), new Vector3(0.2f, 0.015f, 0.12f), Tone.Accent, courier.neck);
        courier.model.Box("Backpack", new Vector3(0f, 1.28f, -0.22f), new Vector3(0.36f, 0.4f, 0.2f), Tone.Light, courier.spine);
        Save(courier.model, "Characters", ColliderMode.None, "Character");

        var cat = new Model("Cat");
        cat.Shape(PrimitiveType.Capsule, "Body", new Vector3(0f, 0.22f, 0f), new Vector3(0.18f, 0.22f, 0.18f), Tone.Mid, euler: new Vector3(90f, 0f, 0f));
        cat.Sphere("Head", new Vector3(0f, 0.32f, 0.26f), new Vector3(0.16f, 0.15f, 0.15f), Tone.Mid);
        cat.Box("EarL", new Vector3(-0.045f, 0.41f, 0.26f), new Vector3(0.035f, 0.05f, 0.02f), Tone.Mid);
        cat.Box("EarR", new Vector3(0.045f, 0.41f, 0.26f), new Vector3(0.035f, 0.05f, 0.02f), Tone.Mid);
        cat.Box("Face", new Vector3(0f, 0.33f, 0.335f), new Vector3(0.08f, 0.03f, 0.01f), Tone.Screen);
        cat.Cylinder("LegFL", new Vector3(-0.06f, 0.07f, 0.12f), 0.025f, 0.14f, Tone.Mid);
        cat.Cylinder("LegFR", new Vector3(0.06f, 0.07f, 0.12f), 0.025f, 0.14f, Tone.Mid);
        cat.Cylinder("LegBL", new Vector3(-0.06f, 0.07f, -0.12f), 0.025f, 0.14f, Tone.Mid);
        cat.Cylinder("LegBR", new Vector3(0.06f, 0.07f, -0.12f), 0.025f, 0.14f, Tone.Mid);
        cat.Shape(PrimitiveType.Capsule, "Tail", new Vector3(0f, 0.32f, -0.28f), new Vector3(0.04f, 0.14f, 0.04f), Tone.Mid, euler: new Vector3(-35f, 0f, 0f));
        Save(cat, "Characters");
    }

    // Rig nhân vật cao khoảng 1.85m, mặt hướng +Z, các pivot là khớp để animation xoay
    private static (Model model, Transform spine, Transform neck) Humanoid(string name, Tone top, Tone bottom)
    {
        var m = new Model(name);
        var rig = m.Pivot("Rig", Vector3.zero);
        var hips = m.Pivot("Hips", new Vector3(0f, 0.95f, 0f), rig);
        m.Box("Pelvis", new Vector3(0f, 0.93f, 0f), new Vector3(0.36f, 0.16f, 0.22f), bottom, hips);

        var spine = m.Pivot("Spine", new Vector3(0f, 0.95f, 0f), hips);
        m.Box("Torso", new Vector3(0f, 1.26f, 0f), new Vector3(0.42f, 0.56f, 0.24f), top, spine);

        var neck = m.Pivot("Neck", new Vector3(0f, 1.55f, 0f), spine);
        m.Cylinder("NeckPart", new Vector3(0f, 1.58f, 0f), 0.05f, 0.08f, Tone.Light, parent: neck);
        m.Sphere("Head", new Vector3(0f, 1.72f, 0f), Vector3.one * 0.26f, Tone.Light, neck);
        m.Box("Face", new Vector3(0f, 1.74f, 0.12f), new Vector3(0.18f, 0.06f, 0.03f), Tone.Screen, neck);

        var shoulderL = m.Pivot("ShoulderL", new Vector3(-0.28f, 1.5f, 0f), spine);
        m.Capsule("ArmL", new Vector3(-0.28f, 1.19f, 0f), 0.06f, 0.62f, top, shoulderL);
        var shoulderR = m.Pivot("ShoulderR", new Vector3(0.28f, 1.5f, 0f), spine);
        m.Capsule("ArmR", new Vector3(0.28f, 1.19f, 0f), 0.06f, 0.62f, top, shoulderR);

        foreach (var (side, x) in new[] { ("L", -0.1f), ("R", 0.1f) })
        {
            var leg = m.Pivot($"Leg{side}", new Vector3(x, 0.9f, 0f), hips);
            m.Capsule($"Thigh{side}", new Vector3(x, 0.675f, 0f), 0.08f, 0.47f, bottom, leg);
            var knee = m.Pivot($"Knee{side}", new Vector3(x, 0.45f, 0f), leg);
            m.Capsule($"Shin{side}", new Vector3(x, 0.24f, 0f), 0.07f, 0.44f, bottom, knee);
            m.Box($"Foot{side}", new Vector3(x, 0.03f, 0.04f), new Vector3(0.1f, 0.06f, 0.22f), Tone.Dark, knee);
        }

        var capsule = m.Root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.925f, 0f);
        capsule.height = 1.85f;
        capsule.radius = 0.25f;
        return (m, spine, neck);
    }

    #endregion

    #region PC parts

    private static void BuildPcParts()
    {
        var pcCase = new Model("PcCase");
        pcCase.Box("Body", new Vector3(0f, 0.24f, 0f), new Vector3(0.2f, 0.46f, 0.44f), Tone.Dark);
        pcCase.Box("Front", new Vector3(0f, 0.24f, 0.225f), new Vector3(0.2f, 0.46f, 0.01f), Tone.Mid);
        pcCase.Cylinder("Power", new Vector3(0f, 0.43f, 0.232f), 0.012f, 0.006f, Tone.Accent, Axis.Z);
        pcCase.Box("SidePanel", new Vector3(0.101f, 0.25f, 0f), new Vector3(0.004f, 0.38f, 0.38f), Tone.Screen);
        foreach (var (x, z) in Corners(0.08f, 0.19f))
            pcCase.Box($"Foot{x}{z}", new Vector3(x, 0.005f, z), new Vector3(0.03f, 0.01f, 0.03f), Tone.Mid);
        Save(pcCase, "PcParts");

        var board = new Model("Motherboard");
        board.Box("Board", new Vector3(0f, 0.004f, 0f), new Vector3(0.24f, 0.008f, 0.3f), Tone.Dark);
        board.Box("Socket", new Vector3(0f, 0.012f, 0.06f), new Vector3(0.05f, 0.008f, 0.05f), Tone.Mid);
        for (var i = 0; i < 4; i++)
            board.Box($"RamSlot{i}", new Vector3(0.07f + i * 0.012f, 0.011f, 0.04f), new Vector3(0.006f, 0.006f, 0.13f), Tone.Mid);
        for (var i = 0; i < 2; i++)
            board.Box($"PcieSlot{i}", new Vector3(-0.02f, 0.011f, -0.06f - i * 0.05f), new Vector3(0.18f, 0.006f, 0.008f), Tone.Accent);
        board.Box("IoShield", new Vector3(-0.1f, 0.02f, 0.08f), new Vector3(0.03f, 0.03f, 0.12f), Tone.Mid);
        Save(board, "PcParts");

        var cpu = new Model("Cpu");
        cpu.Box("Substrate", new Vector3(0f, 0.002f, 0f), new Vector3(0.04f, 0.004f, 0.04f), Tone.Accent);
        cpu.Box("Lid", new Vector3(0f, 0.005f, 0f), new Vector3(0.03f, 0.002f, 0.03f), Tone.Light);
        Save(cpu, "PcParts");

        var cooler = new Model("CpuCooler");
        cooler.Box("Base", new Vector3(0f, 0.01f, 0f), new Vector3(0.08f, 0.02f, 0.08f), Tone.Mid);
        cooler.Box("Fins", new Vector3(0f, 0.08f, 0f), new Vector3(0.12f, 0.12f, 0.06f), Tone.Light);
        cooler.Cylinder("Fan", new Vector3(0f, 0.08f, 0.045f), 0.055f, 0.025f, Tone.Dark, Axis.Z);
        Save(cooler, "PcParts");

        var ram = new Model("Ram");
        ram.Box("Stick", new Vector3(0f, 0.017f, 0f), new Vector3(0.133f, 0.03f, 0.006f), Tone.Dark);
        ram.Box("Heatsink", new Vector3(0f, 0.024f, 0f), new Vector3(0.13f, 0.02f, 0.009f), Tone.Mid);
        ram.Box("Contacts", new Vector3(0f, 0.002f, 0f), new Vector3(0.12f, 0.004f, 0.0065f), Tone.Accent);
        Save(ram, "PcParts");

        var gpu = new Model("GraphicsCard");
        gpu.Box("Shroud", new Vector3(0f, 0.02f, 0f), new Vector3(0.28f, 0.04f, 0.12f), Tone.Dark);
        for (var i = 0; i < 2; i++)
            gpu.Cylinder($"Fan{i}", new Vector3(-0.06f + i * 0.12f, 0.042f, 0f), 0.045f, 0.006f, Tone.Mid);
        gpu.Box("Bracket", new Vector3(-0.142f, 0.03f, 0f), new Vector3(0.004f, 0.06f, 0.12f), Tone.Light);
        gpu.Box("PcieEdge", new Vector3(0.02f, 0.02f, -0.062f), new Vector3(0.09f, 0.008f, 0.004f), Tone.Accent);
        Save(gpu, "PcParts");

        var psu = new Model("Psu");
        psu.Box("Body", new Vector3(0f, 0.043f, 0f), new Vector3(0.15f, 0.086f, 0.14f), Tone.Dark);
        psu.Cylinder("Fan", new Vector3(0f, 0.087f, 0f), 0.055f, 0.004f, Tone.Mid);
        psu.Box("Socket", new Vector3(0f, 0.043f, -0.071f), new Vector3(0.04f, 0.025f, 0.004f), Tone.Light);
        Save(psu, "PcParts");

        var hdd = new Model("Hdd");
        hdd.Box("Body", new Vector3(0f, 0.013f, 0f), new Vector3(0.1f, 0.026f, 0.147f), Tone.Mid);
        hdd.Box("Label", new Vector3(0f, 0.0265f, 0f), new Vector3(0.08f, 0.001f, 0.1f), Tone.Light);
        Save(hdd, "PcParts");

        var cdRom = new Model("CdRom");
        cdRom.Box("Body", new Vector3(0f, 0.021f, 0f), new Vector3(0.146f, 0.042f, 0.17f), Tone.Mid);
        cdRom.Box("Tray", new Vector3(0f, 0.021f, 0.0855f), new Vector3(0.13f, 0.012f, 0.002f), Tone.Dark);
        cdRom.Box("Button", new Vector3(0.055f, 0.012f, 0.0865f), new Vector3(0.012f, 0.006f, 0.002f), Tone.Accent);
        Save(cdRom, "PcParts");

        var router = new Model("Router");
        router.Box("Body", new Vector3(0f, 0.02f, 0f), new Vector3(0.2f, 0.04f, 0.13f), Tone.Dark);
        for (var i = 0; i < 3; i++)
            router.Cylinder($"Antenna{i}", new Vector3(-0.07f + i * 0.07f, 0.11f, -0.06f), 0.006f, 0.14f, Tone.Mid);
        for (var i = 0; i < 4; i++)
            router.Box($"Led{i}", new Vector3(-0.06f + i * 0.04f, 0.025f, 0.066f), new Vector3(0.01f, 0.006f, 0.002f), Tone.Accent);
        Save(router, "PcParts");
    }

    #endregion

    #region Stream setup

    private static void BuildStreamSetup()
    {
        var desk = new Model("ComputerDesk");
        desk.Box("Top", new Vector3(0f, 0.74f, 0f), new Vector3(1.4f, 0.04f, 0.7f), Tone.Light);
        foreach (var (x, z) in Corners(0.66f, 0.31f))
            desk.Box($"Leg{x}{z}", new Vector3(x, 0.36f, z), new Vector3(0.05f, 0.72f, 0.05f), Tone.Dark);
        Save(desk, "StreamSetup");

        var monitor = new Model("Monitor");
        monitor.Box("Base", new Vector3(0f, 0.0075f, 0f), new Vector3(0.22f, 0.015f, 0.16f), Tone.Dark);
        monitor.Box("Neck", new Vector3(0f, 0.17f, -0.04f), new Vector3(0.04f, 0.32f, 0.03f), Tone.Dark);
        monitor.Box("Frame", new Vector3(0f, 0.42f, 0f), new Vector3(0.62f, 0.37f, 0.03f), Tone.Dark);
        monitor.Box("Screen", new Vector3(0f, 0.425f, 0.016f), new Vector3(0.58f, 0.33f, 0.004f), Tone.Screen);
        Save(monitor, "StreamSetup");

        var keyboard = new Model("Keyboard");
        keyboard.Box("Body", new Vector3(0f, 0.0125f, 0f), new Vector3(0.44f, 0.025f, 0.14f), Tone.Dark);
        keyboard.Box("Keys", new Vector3(0f, 0.0275f, 0f), new Vector3(0.42f, 0.005f, 0.12f), Tone.Mid);
        Save(keyboard, "StreamSetup");

        var mouse = new Model("Mouse");
        mouse.Sphere("Body", new Vector3(0f, 0.018f, 0f), new Vector3(0.065f, 0.036f, 0.11f), Tone.Dark);
        mouse.Box("Wheel", new Vector3(0f, 0.035f, 0.025f), new Vector3(0.008f, 0.006f, 0.018f), Tone.Accent);
        Save(mouse, "StreamSetup");

        var pad = new Model("MousePad");
        pad.Box("Pad", new Vector3(0f, 0.002f, 0f), new Vector3(0.32f, 0.004f, 0.27f), Tone.Dark);
        Save(pad, "StreamSetup");

        var mic = new Model("Microphone");
        mic.Cylinder("Base", new Vector3(0f, 0.01f, 0f), 0.07f, 0.02f, Tone.Dark);
        mic.Cylinder("Pole", new Vector3(0f, 0.12f, 0f), 0.008f, 0.2f, Tone.Mid);
        mic.Capsule("Head", new Vector3(0f, 0.28f, 0f), 0.035f, 0.13f, Tone.Mid);
        mic.Cylinder("Band", new Vector3(0f, 0.25f, 0f), 0.037f, 0.01f, Tone.Accent);
        Save(mic, "StreamSetup");

        var webcam = new Model("Webcam");
        webcam.Box("Clip", new Vector3(0f, 0.012f, -0.005f), new Vector3(0.04f, 0.025f, 0.04f), Tone.Mid);
        webcam.Box("Body", new Vector3(0f, 0.04f, 0f), new Vector3(0.09f, 0.03f, 0.03f), Tone.Dark);
        webcam.Cylinder("Lens", new Vector3(0f, 0.04f, 0.016f), 0.011f, 0.004f, Tone.Screen, Axis.Z);
        Save(webcam, "StreamSetup");

        var speaker = new Model("Speaker");
        speaker.Box("Body", new Vector3(0f, 0.09f, 0f), new Vector3(0.1f, 0.18f, 0.12f), Tone.Dark);
        speaker.Cylinder("Woofer", new Vector3(0f, 0.06f, 0.061f), 0.035f, 0.004f, Tone.Mid, Axis.Z);
        speaker.Cylinder("Tweeter", new Vector3(0f, 0.135f, 0.061f), 0.015f, 0.004f, Tone.Mid, Axis.Z);
        Save(speaker, "StreamSetup");

        var chair = new Model("GamingChair");
        chair.Cylinder("Base", new Vector3(0f, 0.04f, 0f), 0.3f, 0.03f, Tone.Dark);
        chair.Cylinder("Pole", new Vector3(0f, 0.25f, 0f), 0.03f, 0.4f, Tone.Mid);
        chair.Box("Seat", new Vector3(0f, 0.48f, 0f), new Vector3(0.5f, 0.08f, 0.5f), Tone.Dark);
        chair.Box("Back", new Vector3(0f, 0.9f, -0.24f), new Vector3(0.5f, 0.75f, 0.08f), Tone.Dark, euler: new Vector3(-8f, 0f, 0f));
        foreach (var x in new[] { -0.27f, 0.27f })
        {
            chair.Box($"ArmSupport{x}", new Vector3(x, 0.56f, 0f), new Vector3(0.03f, 0.12f, 0.03f), Tone.Mid);
            chair.Box($"Armrest{x}", new Vector3(x, 0.63f, 0f), new Vector3(0.06f, 0.03f, 0.3f), Tone.Mid);
        }
        Save(chair, "StreamSetup");

        var greenScreen = new Model("GreenScreen");
        foreach (var x in new[] { -0.8f, 0.8f })
        {
            greenScreen.Cylinder($"Pole{x}", new Vector3(x, 0.9f, 0f), 0.02f, 1.8f, Tone.Mid);
            greenScreen.Box($"Foot{x}", new Vector3(x, 0.01f, 0f), new Vector3(0.04f, 0.02f, 0.5f), Tone.Dark);
        }
        greenScreen.Box("TopBar", new Vector3(0f, 1.8f, 0f), new Vector3(1.64f, 0.03f, 0.03f), Tone.Mid);
        greenScreen.Box("Cloth", new Vector3(0f, 1.02f, 0.02f), new Vector3(1.56f, 1.55f, 0.01f), Tone.Accent);
        Save(greenScreen, "StreamSetup");

        var light = new Model("StudioLight");
        light.Box("FootX", new Vector3(0f, 0.01f, 0f), new Vector3(0.6f, 0.02f, 0.04f), Tone.Dark);
        light.Box("FootZ", new Vector3(0f, 0.01f, 0f), new Vector3(0.04f, 0.02f, 0.6f), Tone.Dark);
        light.Cylinder("Pole", new Vector3(0f, 0.8f, 0f), 0.015f, 1.6f, Tone.Mid);
        light.Box("Panel", new Vector3(0f, 1.65f, 0f), new Vector3(0.4f, 0.3f, 0.06f), Tone.Dark);
        light.Box("Diffuser", new Vector3(0f, 1.65f, 0.031f), new Vector3(0.37f, 0.27f, 0.004f), Tone.Light);
        Save(light, "StreamSetup");
    }

    #endregion

    #region Furniture

    private static void BuildFurniture()
    {
        var bed = new Model("Bed");
        bed.Box("Frame", new Vector3(0f, 0.15f, 0f), new Vector3(1f, 0.3f, 2f), Tone.Mid);
        bed.Box("Mattress", new Vector3(0f, 0.39f, 0f), new Vector3(0.95f, 0.18f, 1.95f), Tone.Light);
        bed.Box("Pillow", new Vector3(0f, 0.53f, -0.75f), new Vector3(0.6f, 0.1f, 0.3f), Tone.Light);
        bed.Box("Blanket", new Vector3(0f, 0.495f, 0.35f), new Vector3(0.97f, 0.03f, 1.1f), Tone.Accent);
        bed.Box("Headboard", new Vector3(0f, 0.5f, -1.03f), new Vector3(1f, 1f, 0.06f), Tone.Dark);
        Save(bed, "Furniture");

        var door = new Model("Door");
        door.Box("FrameL", new Vector3(-0.48f, 1.05f, 0f), new Vector3(0.06f, 2.1f, 0.12f), Tone.Mid, collider: true);
        door.Box("FrameR", new Vector3(0.48f, 1.05f, 0f), new Vector3(0.06f, 2.1f, 0.12f), Tone.Mid, collider: true);
        door.Box("FrameTop", new Vector3(0f, 2.13f, 0f), new Vector3(1.02f, 0.06f, 0.12f), Tone.Mid, collider: true);
        var doorHinge = door.Pivot("Hinge", new Vector3(-0.45f, 0f, 0f));
        door.Box("Panel", new Vector3(0f, 1.02f, 0f), new Vector3(0.9f, 2.04f, 0.045f), Tone.Light, doorHinge, collider: true);
        door.Cylinder("Handle", new Vector3(0.36f, 1f, 0.04f), 0.015f, 0.1f, Tone.Dark, Axis.Z, doorHinge);
        Save(door, "Furniture", ColliderMode.Parts, "Door");

        var fridge = new Model("Fridge");
        fridge.Box("Body", new Vector3(0f, 0.9f, -0.02f), new Vector3(0.7f, 1.8f, 0.62f), Tone.Mid, collider: true);
        var fridgeHinge = fridge.Pivot("Hinge", new Vector3(0.35f, 0f, 0.29f));
        fridge.Box("Door", new Vector3(0f, 0.9f, 0.31f), new Vector3(0.7f, 1.8f, 0.04f), Tone.Light, fridgeHinge, collider: true);
        fridge.Box("Handle", new Vector3(-0.28f, 1.1f, 0.345f), new Vector3(0.03f, 0.4f, 0.03f), Tone.Dark, fridgeHinge);
        fridge.Box("FreezerLine", new Vector3(0f, 1.3f, 0.331f), new Vector3(0.66f, 0.008f, 0.002f), Tone.Dark, fridgeHinge);
        Save(fridge, "Furniture", ColliderMode.Parts, "Fridge");

        var toilet = new Model("Toilet");
        toilet.Shape(PrimitiveType.Cylinder, "Bowl", new Vector3(0f, 0.2f, 0.05f), new Vector3(0.38f, 0.2f, 0.5f), Tone.Light);
        toilet.Box("Seat", new Vector3(0f, 0.41f, 0.06f), new Vector3(0.38f, 0.02f, 0.46f), Tone.Light);
        toilet.Box("Tank", new Vector3(0f, 0.6f, -0.22f), new Vector3(0.42f, 0.38f, 0.18f), Tone.Light);
        toilet.Box("Flush", new Vector3(0f, 0.795f, -0.22f), new Vector3(0.06f, 0.01f, 0.04f), Tone.Accent);
        var lidHinge = toilet.Pivot("Hinge", new Vector3(0f, 0.43f, -0.15f));
        toilet.Box("Lid", new Vector3(0f, 0.43f, 0.07f), new Vector3(0.38f, 0.02f, 0.44f), Tone.Mid, lidHinge);
        Save(toilet, "Furniture", ColliderMode.Bounds, "Toilet");

        var sofa = new Model("Sofa");
        sofa.Box("Base", new Vector3(0f, 0.2f, 0f), new Vector3(1.8f, 0.4f, 0.85f), Tone.Mid);
        sofa.Box("Cushion", new Vector3(0f, 0.46f, 0.05f), new Vector3(1.4f, 0.12f, 0.7f), Tone.Light);
        sofa.Box("Back", new Vector3(0f, 0.65f, -0.33f), new Vector3(1.8f, 0.5f, 0.2f), Tone.Mid);
        foreach (var x in new[] { -0.8f, 0.8f })
            sofa.Box($"Arm{x}", new Vector3(x, 0.3f, 0f), new Vector3(0.2f, 0.6f, 0.85f), Tone.Mid);
        Save(sofa, "Furniture");

        var table = new Model("CoffeeTable");
        table.Box("Top", new Vector3(0f, 0.42f, 0f), new Vector3(1f, 0.04f, 0.5f), Tone.Light);
        foreach (var (x, z) in Corners(0.45f, 0.2f))
            table.Box($"Leg{x}{z}", new Vector3(x, 0.2f, z), new Vector3(0.04f, 0.4f, 0.04f), Tone.Dark);
        Save(table, "Furniture");

        var shelf = new Model("Shelf");
        foreach (var x in new[] { -0.4f, 0.4f })
            shelf.Box($"Side{x}", new Vector3(x, 0.9f, 0f), new Vector3(0.03f, 1.8f, 0.35f), Tone.Mid);
        for (var i = 0; i < 5; i++)
            shelf.Box($"Board{i}", new Vector3(0f, 0.02f + i * 0.44f, 0f), new Vector3(0.8f, 0.03f, 0.35f), Tone.Light);
        shelf.Box("Back", new Vector3(0f, 0.9f, -0.17f), new Vector3(0.8f, 1.8f, 0.01f), Tone.Dark);
        Save(shelf, "Furniture");

        var cabinet = new Model("Cabinet");
        cabinet.Box("Body", new Vector3(0f, 0.45f, 0f), new Vector3(0.8f, 0.9f, 0.45f), Tone.Mid);
        foreach (var x in new[] { -0.2f, 0.2f })
        {
            cabinet.Box($"Door{x}", new Vector3(x, 0.45f, 0.226f), new Vector3(0.38f, 0.86f, 0.004f), Tone.Light);
            cabinet.Box($"Handle{x}", new Vector3(x * 0.2f, 0.55f, 0.235f), new Vector3(0.015f, 0.12f, 0.015f), Tone.Dark);
        }
        Save(cabinet, "Furniture");

        var lamp = new Model("FloorLamp");
        lamp.Cylinder("Base", new Vector3(0f, 0.015f, 0f), 0.15f, 0.03f, Tone.Dark);
        lamp.Cylinder("Pole", new Vector3(0f, 0.8f, 0f), 0.015f, 1.55f, Tone.Mid);
        lamp.Cylinder("Shade", new Vector3(0f, 1.55f, 0f), 0.18f, 0.28f, Tone.Light);
        Save(lamp, "Furniture");

        var plant = new Model("Plant");
        plant.Cylinder("Pot", new Vector3(0f, 0.15f, 0f), 0.15f, 0.3f, Tone.Mid);
        plant.Cylinder("Soil", new Vector3(0f, 0.29f, 0f), 0.14f, 0.02f, Tone.Dark);
        plant.Sphere("Leaves", new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.55f, 0.5f), Tone.Accent);
        plant.Sphere("LeavesTop", new Vector3(0.1f, 0.85f, 0.05f), new Vector3(0.3f, 0.3f, 0.3f), Tone.Accent);
        Save(plant, "Furniture");

        var bathtub = new Model("Bathtub");
        bathtub.Box("Outer", new Vector3(0f, 0.28f, 0f), new Vector3(0.8f, 0.56f, 1.7f), Tone.Light);
        bathtub.Box("Water", new Vector3(0f, 0.5f, 0f), new Vector3(0.66f, 0.04f, 1.54f), Tone.Accent);
        bathtub.Cylinder("Faucet", new Vector3(0f, 0.62f, -0.78f), 0.02f, 0.12f, Tone.Dark);
        Save(bathtub, "Furniture");
    }

    #endregion

    #region Food and items

    private static void BuildFood()
    {
        var pizza = new Model("PizzaBox");
        pizza.Box("Box", new Vector3(0f, 0.02f, 0f), new Vector3(0.35f, 0.04f, 0.35f), Tone.Light);
        pizza.Box("Logo", new Vector3(0f, 0.0405f, 0f), new Vector3(0.12f, 0.001f, 0.12f), Tone.Accent);
        Save(pizza, "Food");

        var burger = new Model("BurgerBox");
        burger.Box("Box", new Vector3(0f, 0.04f, 0f), new Vector3(0.12f, 0.08f, 0.12f), Tone.Light);
        burger.Box("LidLine", new Vector3(0f, 0.05f, 0.0605f), new Vector3(0.12f, 0.004f, 0.001f), Tone.Dark);
        Save(burger, "Food");

        var can = new Model("Can");
        can.Cylinder("Body", new Vector3(0f, 0.06f, 0f), 0.033f, 0.12f, Tone.Accent);
        can.Cylinder("Top", new Vector3(0f, 0.1205f, 0f), 0.03f, 0.002f, Tone.Light);
        Save(can, "Food");

        var chips = new Model("ChipsBag");
        chips.Box("Bag", new Vector3(0f, 0.1f, 0f), new Vector3(0.15f, 0.2f, 0.05f), Tone.Accent);
        chips.Box("Label", new Vector3(0f, 0.11f, 0.0255f), new Vector3(0.1f, 0.08f, 0.001f), Tone.Light);
        Save(chips, "Food");

        var cup = new Model("Cup");
        cup.Cylinder("Body", new Vector3(0f, 0.06f, 0f), 0.04f, 0.12f, Tone.Light);
        cup.Cylinder("Lid", new Vector3(0f, 0.123f, 0f), 0.042f, 0.006f, Tone.Dark);
        cup.Cylinder("Straw", new Vector3(0.01f, 0.15f, 0f), 0.004f, 0.08f, Tone.Mid);
        Save(cup, "Food");

        var donut = new Model("Donut");
        donut.Cylinder("Dough", new Vector3(0f, 0.02f, 0f), 0.05f, 0.035f, Tone.Mid);
        donut.Cylinder("Icing", new Vector3(0f, 0.035f, 0f), 0.046f, 0.008f, Tone.Accent);
        donut.Cylinder("Hole", new Vector3(0f, 0.02f, 0f), 0.018f, 0.05f, Tone.Dark);
        Save(donut, "Food");
    }

    private static void BuildItems()
    {
        var box = new Model("CargoBox");
        box.Box("Box", new Vector3(0f, 0.2f, 0f), new Vector3(0.5f, 0.4f, 0.4f), Tone.Mid);
        box.Box("Tape", new Vector3(0f, 0.4005f, 0f), new Vector3(0.06f, 0.001f, 0.4f), Tone.Light);
        Save(box, "Items");

        var mop = new Model("Mop");
        mop.Cylinder("Stick", new Vector3(0f, 0.7f, 0f), 0.012f, 1.3f, Tone.Mid);
        mop.Box("Head", new Vector3(0f, 0.03f, 0f), new Vector3(0.35f, 0.04f, 0.1f), Tone.Dark);
        Save(mop, "Items");

        var medicine = new Model("Medicine");
        medicine.Cylinder("Bottle", new Vector3(0f, 0.05f, 0f), 0.025f, 0.1f, Tone.Light);
        medicine.Cylinder("Cap", new Vector3(0f, 0.11f, 0f), 0.026f, 0.02f, Tone.Accent);
        Save(medicine, "Items");

        var paper = new Model("ToiletPaper");
        paper.Cylinder("Roll", new Vector3(0f, 0.05f, 0f), 0.055f, 0.1f, Tone.Light);
        paper.Cylinder("Core", new Vector3(0f, 0.05f, 0f), 0.02f, 0.102f, Tone.Dark);
        Save(paper, "Items");
    }

    #endregion

    #region Vehicles

    private static void BuildVehicles()
    {
        var car = new Model("Car");
        car.Box("Body", new Vector3(0f, 0.62f, 0f), new Vector3(1.8f, 0.6f, 4.2f), Tone.Mid);
        car.Box("Cabin", new Vector3(0f, 1.15f, -0.2f), new Vector3(1.6f, 0.5f, 2.1f), Tone.Screen);
        foreach (var (x, z) in Corners(0.85f, 1.35f))
            car.Cylinder($"Wheel{x}{z}", new Vector3(x, 0.33f, z), 0.33f, 0.25f, Tone.Dark, Axis.X);
        foreach (var x in new[] { -0.6f, 0.6f })
        {
            car.Box($"Headlight{x}", new Vector3(x, 0.7f, 2.101f), new Vector3(0.3f, 0.12f, 0.01f), Tone.Light);
            car.Box($"Taillight{x}", new Vector3(x, 0.7f, -2.101f), new Vector3(0.3f, 0.12f, 0.01f), Tone.Accent);
        }
        Save(car, "Vehicles");

        var motorbike = new Model("Motorbike");
        foreach (var z in new[] { -0.7f, 0.7f })
            motorbike.Cylinder($"Wheel{z}", new Vector3(0f, 0.32f, z), 0.32f, 0.12f, Tone.Dark, Axis.X);
        motorbike.Box("Frame", new Vector3(0f, 0.6f, 0f), new Vector3(0.25f, 0.35f, 1.1f), Tone.Mid);
        motorbike.Box("Seat", new Vector3(0f, 0.82f, -0.2f), new Vector3(0.28f, 0.08f, 0.6f), Tone.Dark);
        motorbike.Box("Tank", new Vector3(0f, 0.82f, 0.25f), new Vector3(0.3f, 0.2f, 0.35f), Tone.Accent);
        motorbike.Box("Fork", new Vector3(0f, 0.68f, 0.62f), new Vector3(0.05f, 0.7f, 0.05f), Tone.Dark, euler: new Vector3(-20f, 0f, 0f));
        motorbike.Cylinder("Handlebar", new Vector3(0f, 1.02f, 0.5f), 0.015f, 0.7f, Tone.Dark, Axis.X);
        Save(motorbike, "Vehicles");

        var bicycle = new Model("Bicycle");
        foreach (var z in new[] { -0.52f, 0.52f })
            bicycle.Cylinder($"Wheel{z}", new Vector3(0f, 0.34f, z), 0.34f, 0.04f, Tone.Dark, Axis.X);
        bicycle.Box("TopTube", new Vector3(0f, 0.75f, 0.05f), new Vector3(0.04f, 0.04f, 0.7f), Tone.Mid);
        bicycle.Box("DownTube", new Vector3(0f, 0.55f, 0.1f), new Vector3(0.04f, 0.04f, 0.75f), Tone.Mid, euler: new Vector3(-35f, 0f, 0f));
        bicycle.Box("SeatPost", new Vector3(0f, 0.7f, -0.3f), new Vector3(0.03f, 0.5f, 0.03f), Tone.Mid);
        bicycle.Box("Seat", new Vector3(0f, 0.96f, -0.3f), new Vector3(0.12f, 0.04f, 0.25f), Tone.Dark);
        bicycle.Box("Stem", new Vector3(0f, 0.75f, 0.45f), new Vector3(0.03f, 0.5f, 0.03f), Tone.Mid);
        bicycle.Cylinder("Handlebar", new Vector3(0f, 1f, 0.45f), 0.012f, 0.5f, Tone.Dark, Axis.X);
        Save(bicycle, "Vehicles");
    }

    #endregion

    #region World

    private static void BuildWorld()
    {
        var room = new Model("Room");
        room.Box("Floor", new Vector3(0f, 0.05f, 0f), new Vector3(6f, 0.1f, 6f), Tone.Mid, collider: true);
        room.Box("WallBack", new Vector3(0f, 1.6f, -3f), new Vector3(6f, 3f, 0.2f), Tone.Light, collider: true);
        room.Box("WallLeft", new Vector3(-3f, 1.6f, 0f), new Vector3(0.2f, 3f, 6f), Tone.Light, collider: true);
        room.Box("WallRight", new Vector3(3f, 1.6f, 0f), new Vector3(0.2f, 3f, 6f), Tone.Light, collider: true);
        room.Box("WallFrontL", new Vector3(-1.75f, 1.6f, 3f), new Vector3(2.5f, 3f, 0.2f), Tone.Light, collider: true);
        room.Box("WallFrontR", new Vector3(1.75f, 1.6f, 3f), new Vector3(2.5f, 3f, 0.2f), Tone.Light, collider: true);
        room.Box("Lintel", new Vector3(0f, 2.65f, 3f), new Vector3(1f, 0.9f, 0.2f), Tone.Light, collider: true);
        Save(room, "World", ColliderMode.Parts);

        var road = new Model("RoadSegment");
        road.Box("Asphalt", new Vector3(0f, 0.025f, 0f), new Vector3(10f, 0.05f, 7f), Tone.Dark, collider: true);
        for (var i = 0; i < 5; i++)
            road.Box($"Dash{i}", new Vector3(-4f + i * 2f, 0.051f, 0f), new Vector3(1f, 0.002f, 0.15f), Tone.Light);
        foreach (var z in new[] { -4.5f, 4.5f })
            road.Box($"Sidewalk{z}", new Vector3(0f, 0.08f, z), new Vector3(10f, 0.16f, 2f), Tone.Mid, collider: true);
        Save(road, "World", ColliderMode.Parts);

        var streetLamp = new Model("StreetLamp");
        streetLamp.Cylinder("Pole", new Vector3(0f, 2f, 0f), 0.06f, 4f, Tone.Dark, collider: true);
        streetLamp.Box("Arm", new Vector3(0f, 3.95f, 0.5f), new Vector3(0.08f, 0.08f, 1f), Tone.Dark);
        streetLamp.Box("Lamp", new Vector3(0f, 3.88f, 0.95f), new Vector3(0.3f, 0.08f, 0.2f), Tone.Light);
        Save(streetLamp, "World", ColliderMode.Parts);

        var tree = new Model("Tree");
        tree.Cylinder("Trunk", new Vector3(0f, 1f, 0f), 0.15f, 2f, Tone.Dark, collider: true);
        tree.Sphere("Crown", new Vector3(0f, 2.6f, 0f), new Vector3(2.2f, 2f, 2.2f), Tone.Accent);
        tree.Sphere("CrownTop", new Vector3(0.5f, 3.2f, 0.2f), new Vector3(1.3f, 1.2f, 1.3f), Tone.Accent);
        Save(tree, "World", ColliderMode.Parts);

        var apartment = new Model("Apartment");
        apartment.Box("Body", new Vector3(0f, 5f, 0f), new Vector3(8f, 10f, 8f), Tone.Light);
        apartment.Box("Roof", new Vector3(0f, 10.1f, 0f), new Vector3(8.2f, 0.2f, 8.2f), Tone.Mid);
        apartment.Box("Door", new Vector3(0f, 1.1f, 4.005f), new Vector3(1.4f, 2.2f, 0.02f), Tone.Dark);
        for (var row = 0; row < 3; row++)
            for (var col = 0; col < 3; col++)
                apartment.Box($"Window{row}{col}", new Vector3(-2.5f + col * 2.5f, 3.5f + row * 2.5f, 4.005f), new Vector3(1.2f, 1.2f, 0.02f), Tone.Screen);
        Save(apartment, "World");

        var shop = new Model("Shop");
        shop.Box("Body", new Vector3(0f, 2.5f, 0f), new Vector3(10f, 5f, 8f), Tone.Mid);
        shop.Box("Window", new Vector3(-2f, 1.6f, 4.005f), new Vector3(4.5f, 2.2f, 0.02f), Tone.Screen);
        shop.Box("Door", new Vector3(2.5f, 1.1f, 4.005f), new Vector3(1.4f, 2.2f, 0.02f), Tone.Dark);
        shop.Box("Sign", new Vector3(0f, 4.2f, 4.05f), new Vector3(6f, 0.8f, 0.1f), Tone.Accent);
        Save(shop, "World");
    }

    #endregion

    #region Minigames

    private static void BuildMinigames()
    {
        var slot = new Model("SlotMachine");
        slot.Box("Cabinet", new Vector3(0f, 0.8f, 0f), new Vector3(0.7f, 1.6f, 0.6f), Tone.Mid);
        slot.Box("Window", new Vector3(0f, 1.15f, 0.301f), new Vector3(0.56f, 0.36f, 0.004f), Tone.Screen);
        slot.Box("Topper", new Vector3(0f, 1.7f, 0f), new Vector3(0.7f, 0.2f, 0.6f), Tone.Accent);
        slot.Box("Ledge", new Vector3(0f, 0.82f, 0.36f), new Vector3(0.7f, 0.04f, 0.12f), Tone.Dark);
        slot.Box("Button", new Vector3(0f, 0.85f, 0.37f), new Vector3(0.12f, 0.03f, 0.06f), Tone.Accent);
        slot.Cylinder("Lever", new Vector3(0.4f, 1.2f, 0f), 0.02f, 0.4f, Tone.Dark);
        slot.Sphere("Knob", new Vector3(0.4f, 1.42f, 0f), Vector3.one * 0.07f, Tone.Accent);
        var reels = slot.Pivot("Spinner", new Vector3(0f, 1.15f, 0.2f));
        for (var i = 0; i < 3; i++)
        {
            var x = -0.18f + i * 0.18f;
            slot.Cylinder($"Reel{i}", new Vector3(x, 1.15f, 0.2f), 0.12f, 0.15f, Tone.Light, Axis.X, reels);
            slot.Box($"ReelMark{i}", new Vector3(x, 1.15f, 0.32f), new Vector3(0.1f, 0.03f, 0.01f), Tone.Dark, reels);
        }
        Save(slot, "Minigames", ColliderMode.Bounds, "SlotMachine");

        var luckySpin = new Model("LuckySpinMachine");
        luckySpin.Box("Stand", new Vector3(0f, 0.5f, -0.15f), new Vector3(0.3f, 1f, 0.2f), Tone.Mid);
        luckySpin.Box("Backboard", new Vector3(0f, 1.6f, -0.05f), new Vector3(1.4f, 1.4f, 0.06f), Tone.Dark);
        luckySpin.Box("Pointer", new Vector3(0f, 2.25f, 0.06f), new Vector3(0.08f, 0.14f, 0.04f), Tone.Accent);
        var wheel = luckySpin.Pivot("Spinner", new Vector3(0f, 1.6f, 0.02f));
        luckySpin.Cylinder("Wheel", new Vector3(0f, 1.6f, 0.02f), 0.6f, 0.04f, Tone.Light, Axis.Z, wheel);
        for (var i = 0; i < 8; i++)
        {
            var angle = i * 45f;
            var direction = new Vector3(-Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad), 0f);
            luckySpin.Box($"Segment{i}", new Vector3(0f, 1.6f, 0.045f) + direction * 0.3f, new Vector3(0.02f, 0.58f, 0.005f), Tone.Dark, wheel, new Vector3(0f, 0f, angle));
        }
        Save(luckySpin, "Minigames", ColliderMode.Bounds, "LuckySpinMachine");

        var card = new Model("PlayingCard");
        card.Box("Card", new Vector3(0f, 0.001f, 0f), new Vector3(0.063f, 0.002f, 0.088f), Tone.Light);
        card.Box("Mark", new Vector3(0f, 0.0021f, 0f), new Vector3(0.03f, 0.0002f, 0.04f), Tone.Dark);
        Save(card, "Minigames");

        var blackjack = new Model("BlackjackTable");
        blackjack.Box("Rim", new Vector3(0f, 0.83f, 0f), new Vector3(2.1f, 0.06f, 1.2f), Tone.Dark);
        blackjack.Box("Felt", new Vector3(0f, 0.865f, 0f), new Vector3(2f, 0.01f, 1.1f), Tone.Accent);
        foreach (var (x, z) in Corners(0.9f, 0.45f))
            blackjack.Box($"Leg{x}{z}", new Vector3(x, 0.4f, z), new Vector3(0.08f, 0.8f, 0.08f), Tone.Dark);
        for (var i = 0; i < 3; i++)
            blackjack.Cylinder($"Chips{i}", new Vector3(0.5f + i * 0.08f, 0.88f, 0.2f), 0.02f, 0.02f + i * 0.01f, Tone.Light);
        Save(blackjack, "Minigames");
    }

    #endregion

    #region Showcase

    [MenuItem("Tools/Prototype/Build Showcase Scene")]
    public static void BuildShowcase()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var z = 0f;
        var maxX = 0f;
        foreach (var category in ShowcaseOrder)
        {
            var row = Row(category, z, font, out var width);
            z = row + 3f;
            maxX = Mathf.Max(maxX, width);
            if (category == "Characters")
            {
                z = PoseRow(z, font, out width) + 3f;
                maxX = Mathf.Max(maxX, width);
            }
        }

        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(maxX * 0.5f, -0.05f, z * 0.5f - 1f);
        ground.transform.localScale = new Vector3(maxX + 6f, 0.1f, z + 4f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/Proto_Ground.mat");

        var camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(4f, 3f, -4f);
            camera.transform.LookAt(new Vector3(4f, 0f, 2f));
        }

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ShowcaseScenePath);
    }

    private static float Row(string category, float z, Font font, out float width)
    {
        var parent = new GameObject($"Row_{category}").transform;
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { $"{PrefabRoot}/{category}" });
        var items = new List<(GameObject instance, Bounds bounds)>();
        foreach (var path in guids.Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            AddPreview(instance, prefab.name);
            items.Add((instance, WorldBounds(instance)));
        }

        var widths = items.Select(item => item.bounds.size.x).OrderBy(w => w).ToList();
        var labelSize = Mathf.Clamp(widths[widths.Count / 2] * 0.05f, 0.005f, 0.08f);
        var x = 0f;
        var depth = 0f;
        foreach (var (instance, bounds) in items)
        {
            var gap = Mathf.Max(labelSize * 25f, bounds.size.x * 0.4f);
            var text = instance.name.Replace("Proto_", "");
            var fitSize = Mathf.Min(labelSize, (bounds.size.x + gap * 0.8f) / (text.Length * 3.2f));
            instance.transform.position = new Vector3(x - bounds.min.x, 0f, z - bounds.min.z);
            Label(font, text, new Vector3(x + bounds.size.x * 0.5f, 0.01f, z - 0.05f), fitSize, parent);
            x += bounds.size.x + gap;
            depth = Mathf.Max(depth, bounds.size.z);
        }
        Label(font, category, new Vector3(-0.5f, 0.01f, z), Mathf.Max(labelSize * 2.5f, 0.03f), parent, TextAnchor.UpperRight);
        width = x;
        return z + depth;
    }

    // Hàng nhân vật đứng ở từng tư thế, có ghế và giường đi kèm để xem tư thế ngồi, nằm
    private static float PoseRow(float z, Font font, out float width)
    {
        var parent = new GameObject("Row_CharacterPoses").transform;
        var player = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Characters/Proto_Player.prefab");
        var chair = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/StreamSetup/Proto_GamingChair.prefab");
        var bed = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/Furniture/Proto_Bed.prefab");

        var poses = new List<(string name, float speed, int action)> { ("Idle", 0f, 0), ("Walk", 1.5f, 0), ("Run", 4.5f, 0) };
        poses.AddRange(CharacterActions.Select(a => (a.name, 0f, a.value)));

        var x = 0f;
        foreach (var (name, speed, action) in poses)
        {
            var position = new Vector3(x + 0.6f, 0f, z + 1.1f);
            var rotation = Quaternion.Euler(0f, 180f, 0f);
            if (name == "Sit" || name == "Typing")
                ((GameObject)PrefabUtility.InstantiatePrefab(chair, parent)).transform.SetPositionAndRotation(position, rotation);
            if (name == "Sleep")
                ((GameObject)PrefabUtility.InstantiatePrefab(bed, parent)).transform.SetPositionAndRotation(position, rotation);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(player, parent);
            instance.name = $"Pose_{name}";
            instance.transform.SetPositionAndRotation(position, rotation);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationRoot}/Character_{name}.anim");
            clip.SampleAnimation(instance, clip.length * 0.25f);

            var preview = instance.AddComponent<PrototypeAnimatorPreview>();
            var serialized = new SerializedObject(preview);
            serialized.FindProperty("speed").floatValue = speed;
            serialized.FindProperty("action").intValue = action;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Label(font, name, new Vector3(x + 0.6f, 0.01f, z - 0.05f), 0.03f, parent);
            x += 1.6f;
        }
        Label(font, "Poses", new Vector3(-0.5f, 0.01f, z), 0.06f, parent, TextAnchor.UpperRight);
        width = x;
        return z + 2.2f;
    }

    private static void AddPreview(GameObject instance, string prefabName)
    {
        if (instance.GetComponent<Animator>() == null)
            return;

        var preview = instance.AddComponent<PrototypeAnimatorPreview>();
        var serialized = new SerializedObject(preview);
        var spinner = prefabName.Contains("Slot") || prefabName.Contains("Spin");
        if (!prefabName.Contains("Player") && !prefabName.Contains("Npc"))
        {
            serialized.FindProperty("boolParameter").stringValue = spinner ? "IsSpinning" : "IsOpen";
            serialized.FindProperty("toggleInterval").floatValue = spinner ? 0f : 2f;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Bounds WorldBounds(GameObject instance)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static void Label(Font font, string text, Vector3 position, float size, Transform parent, TextAnchor anchor = TextAnchor.UpperCenter)
    {
        var label = new GameObject($"Label_{text}");
        label.transform.SetParent(parent, false);
        label.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, 0f, 0f));
        var mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.font = font;
        mesh.fontSize = 64;
        mesh.characterSize = size;
        mesh.anchor = anchor;
        mesh.color = new Color(0.2f, 0.2f, 0.2f);
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }

    #endregion

    private static IEnumerable<(float x, float z)> Corners(float x, float z)
    {
        yield return (-x, -z);
        yield return (x, -z);
        yield return (-x, z);
        yield return (x, z);
    }
}
