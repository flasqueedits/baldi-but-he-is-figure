#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds an Animator Controller (idle / walk / run / crouch) for the student model,
/// assigns it to the networked player prefab and adds the replication component.
/// Works with whatever clips exist in the imported FBX; generates simple ones if needed.
/// </summary>
public static class CharacterAnimatorSetup
{
    private const string FbxPath = "Assets/Baldi Student Model 2.fbx";
    private const string PrefabPath = "Assets/Multiplayer/NetworkPlayer.prefab";
    private const string ControllerPath = "Assets/Multiplayer/StudentAnimator.controller";
    private const string IdleClipPath = "Assets/Multiplayer/StudentIdle.anim";
    private const string WalkClipPath = "Assets/Multiplayer/StudentWalk.anim";
    private const string RunClipPath = "Assets/Multiplayer/StudentRun.anim";
    private const string CrouchClipPath = "Assets/Multiplayer/StudentCrouch.anim";

    private const string Hip = "Armature.001/Bone";
    private const string Head = "Armature.001/Bone/Bone.009";
    private const string RightThigh = "Armature.001/Bone/Bone.002";
    private const string LeftThigh = "Armature.001/Bone/Bone.005";
    private const string RightShin = "Armature.001/Bone/Bone.003";
    private const string LeftShin = "Armature.001/Bone/Bone.006";
    private const string RightArm = "Armature.001/Bone/Bone.013";
    private const string LeftArm = "Armature.001/Bone/Bone.014";

    [MenuItem("Tools/Figure Multiplayer/Karakter Animasyonlarını Kur")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Character] Play modundayken kurulum yapılamaz.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<Object>(PrefabPath) == null)
        {
            Debug.LogError("[Character] Önce co-op kurulumunu çalıştır (NetworkPlayer.prefab yok).");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Multiplayer"))
            AssetDatabase.CreateFolder("Assets", "Multiplayer");

        AnimationClip idle = NewClip(IdleClipPath, "StudentIdle", 0.2f, true, c => { });
        AnimationClip walk = CreateWalkClip();
        AnimationClip run = CreateRunClip();
        AnimationClip crouch = CreateCrouchClip();

        AnimatorController controller = BuildController(idle, walk, run, crouch);

        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);

        Transform model = FixHierarchy(contents);

        Animator animator = model != null ? model.GetComponent<Animator>() : null;
        if (animator == null) animator = contents.GetComponentInChildren<Animator>(true);
        Transform modelRoot = FindModelRoot(contents, animator);

        if (animator == null)
        {
            if (modelRoot == null)
            {
                Debug.LogError("[Character] Prefab içinde karakter modeli bulunamadı.");
                PrefabUtility.UnloadPrefabContents(contents);
                return;
            }
            animator = modelRoot.gameObject.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        NetworkCharacterAnimator netAnimator = contents.GetComponent<NetworkCharacterAnimator>();
        if (netAnimator == null) netAnimator = contents.AddComponent<NetworkCharacterAnimator>();
        netAnimator.animator = animator;
        netAnimator.visualRoot = modelRoot != null ? modelRoot : animator.transform;
        netAnimator.visualRenderers = (modelRoot != null ? modelRoot : animator.transform)
            .GetComponentsInChildren<Renderer>(true);

        PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Character] Animasyonlar kuruldu: idle='" + idle.name + "', walk='" + walk.name +
                  "', run='" + run.name + "', crouch='" + crouch.name + "'. " +
                  "Yerel oyuncunun modeli gizleniyor, diğer oyuncularınki görünüyor.");
    }

    private static AnimatorController BuildController(AnimationClip idle, AnimationClip walk, AnimationClip run, AnimationClip crouch)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Run", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimatorState idleState = machine.AddState("Idle");
        idleState.motion = idle;

        AnimatorState walkState = machine.AddState("Walk");
        walkState.motion = walk;

        AnimatorState runState = machine.AddState("Run");
        runState.motion = run;
        runState.speed = 1.5f;

        AnimatorState crouchState = machine.AddState("Crouch");
        crouchState.motion = crouch;
        crouchState.speed = 0.6f;
        machine.defaultState = idleState;

        AddAnyTransition(machine, crouchState, 0, new[] { Condition("Crouch", AnimatorConditionMode.If, 0f) });
        AddAnyTransition(machine, runState, 1, new[]
        {
            Condition("Crouch", AnimatorConditionMode.IfNot, 0f),
            Condition("Run", AnimatorConditionMode.If, 0f)
        });
        AddAnyTransition(machine, walkState, 2, new[]
        {
            Condition("Crouch", AnimatorConditionMode.IfNot, 0f),
            Condition("Run", AnimatorConditionMode.IfNot, 0f),
            Condition("Speed", AnimatorConditionMode.Greater, 0.15f)
        });
        AddAnyTransition(machine, idleState, 3, new[]
        {
            Condition("Crouch", AnimatorConditionMode.IfNot, 0f),
            Condition("Run", AnimatorConditionMode.IfNot, 0f),
            Condition("Speed", AnimatorConditionMode.Less, 0.15f)
        });

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static (string, AnimatorConditionMode, float) Condition(string name, AnimatorConditionMode mode, float value)
    {
        return (name, mode, value);
    }

    private static void AddAnyTransition(AnimatorStateMachine machine, AnimatorState target, int order,
        (string, AnimatorConditionMode, float)[] conditions)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(target);
        transition.hasExitTime = false;
        transition.duration = 0.12f;
        transition.canTransitionToSelf = false;
        foreach (var c in conditions)
            transition.AddCondition(c.Item2, c.Item3, c.Item1);
    }

    private static AnimationClip NewClip(string path, string name, float duration, bool loop, System.Action<AnimationClip> build)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);

        AnimationClip clip = new AnimationClip { name = name, frameRate = 60 };
        AssetDatabase.CreateAsset(clip, path);
        build(clip);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.stopTime = duration;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void X(AnimationClip clip, string bone, float atStart, float atHalf, float atEnd, float duration)
    {
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, atStart),
            new Keyframe(duration * 0.5f, atHalf),
            new Keyframe(duration, atEnd));
        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        clip.SetCurve(bone, typeof(Transform), "localEulerAngles.x", curve);
    }

    private static void XConstant(AnimationClip clip, string bone, float degrees, float duration)
    {
        clip.SetCurve(bone, typeof(Transform), "localEulerAngles.x", AnimationCurve.Constant(0f, duration, degrees));
    }

    private static AnimationClip CreateWalkClip()
    {
        return NewClip(WalkClipPath, "StudentWalk", 1f, true, c =>
        {
            X(c, RightThigh, 28f, -28f, 28f, 1f);
            X(c, LeftThigh, -28f, 28f, -28f, 1f);
            X(c, RightShin, -8f, -34f, -8f, 1f);
            X(c, LeftShin, -34f, -8f, -34f, 1f);
            X(c, RightArm, -22f, 22f, -22f, 1f);
            X(c, LeftArm, 22f, -22f, 22f, 1f);
            X(c, Hip, 0f, 2f, 0f, 1f);
        });
    }

    private static AnimationClip CreateRunClip()
    {
        return NewClip(RunClipPath, "StudentRun", 0.7f, true, c =>
        {
            X(c, RightThigh, 46f, -46f, 46f, 0.7f);
            X(c, LeftThigh, -46f, 46f, -46f, 0.7f);
            X(c, RightShin, -15f, -55f, -15f, 0.7f);
            X(c, LeftShin, -55f, -15f, -55f, 0.7f);
            X(c, RightArm, -36f, 36f, -36f, 0.7f);
            X(c, LeftArm, 36f, -36f, 36f, 0.7f);
            X(c, Hip, 5f, 9f, 5f, 0.7f);
        });
    }

    private static AnimationClip CreateCrouchClip()
    {
        return NewClip(CrouchClipPath, "StudentCrouch", 0.25f, false, c =>
        {
            XConstant(c, Hip, 30f, 0.25f);
            XConstant(c, RightThigh, -55f, 0.25f);
            XConstant(c, LeftThigh, -55f, 0.25f);
            XConstant(c, RightShin, 85f, 0.25f);
            XConstant(c, LeftShin, 85f, 0.25f);
            XConstant(c, Head, -18f, 0.25f);
            XConstant(c, RightArm, -20f, 0.25f);
            XConstant(c, LeftArm, 20f, 0.25f);
        });
    }

    /// <summary>
    /// The model was parented under the first-person camera; on remote players that
    /// camera is disabled, so the model would never follow the player. Move it to
    /// the player root and disable duplicate cameras.
    /// </summary>
    private static Transform FixHierarchy(GameObject prefab)
    {
        Transform model = null;
        foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.IndexOf("Baldi Student Model", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                model = t;
                break;
            }
        }

        if (model != null && model.parent != prefab.transform)
        {
            model.SetParent(prefab.transform, false);
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
        }

        bool keptMain = false;
        foreach (Camera camera in prefab.GetComponentsInChildren<Camera>(true))
        {
            bool isMain = !keptMain && camera.GetComponent<CameraScript>() != null;
            if (!isMain) isMain = !keptMain && camera.name == "Main Camera";
            if (isMain)
            {
                keptMain = true;
                camera.enabled = true;
                camera.gameObject.tag = "MainCamera";
            }
            else
            {
                camera.enabled = false;
                camera.gameObject.tag = "Untagged";
            }
        }
        return model;
    }

    private static Transform FindModelRoot(GameObject prefab, Animator animator)
    {
        if (animator != null) return animator.transform;

        foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            if (t.name.IndexOf("Baldi Student Model", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return t;

        SkinnedMeshRenderer[] skins = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (skins.Length > 0) return skins[0].transform.parent != null ? skins[0].transform.parent : skins[0].transform;
        return null;
    }
}
#endif
