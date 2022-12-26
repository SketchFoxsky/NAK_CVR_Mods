﻿using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.IK;
using ABI_RC.Systems.IK.SubSystems;
using HarmonyLib;
using RootMotion.FinalIK;
using UnityEngine;

namespace DesktopVRIK;

[HarmonyPatch]
internal class HarmonyPatches
{

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "SetupAvatarGeneral")]
    private static void SetupDesktopIKSystem(ref CVRAvatar ____avatarDescriptor, ref Animator ____animator)
    {
        if (!MetaPort.Instance.isUsingVr && DesktopVRIK.Instance.Setting_Enabled)
        {
            if (____avatarDescriptor != null && ____animator != null && ____animator.isHuman)
            {
                //this will stop at the useless isVr return (the function is only ever called by vr anyways...)
                IKSystem.Instance.InitializeAvatar(____avatarDescriptor);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(IKSystem), "InitializeAvatar")]
    private static void InitializeDesktopAvatarVRIK(CVRAvatar avatar, ref VRIK ____vrik, ref HumanPoseHandler ____poseHandler, ref float[] ___HandCalibrationPoseMuscles, ref Vector3 ____referenceRootPosition, ref Quaternion ____referenceRootRotation, ref HumanPose ___humanPose)
    {
        if (!MetaPort.Instance.isUsingVr && DesktopVRIK.Instance.Setting_Enabled)
        {
            if (IKSystem.Instance.animator != null && IKSystem.Instance.animator.avatar != null && IKSystem.Instance.animator.avatar.isHuman)
            {
                //need IKSystem to see VRIK component for setup
                if (____vrik == null)
                {
                    ____vrik = avatar.gameObject.AddComponent<VRIK>();
                }

                //why the fuck does this fix bad armatures and heels in ground ???
                if (____poseHandler == null)
                {
                    ____poseHandler = new HumanPoseHandler(IKSystem.Instance.animator.avatar, IKSystem.Instance.animator.transform);
                }
                ____poseHandler.GetHumanPose(ref ___humanPose);
                ____poseHandler.SetHumanPose(ref ___humanPose);

                //now I calibrate DesktopVRIK
                DesktopVRIK.Instance.CalibrateDesktopVRIK(avatar);
            }
        }
    }

    private static bool emotePlayed = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "Update")]
    private static void CorrectVRIK(ref bool ____emotePlaying, ref LookAtIK ___lookIK)
    {
        if (MetaPort.Instance.isUsingVr || DesktopVRIK.Instance == null) return;

        //might need to rework this in the future
        if (____emotePlaying && !emotePlayed)
        {
            emotePlayed = true;
            if (DesktopVRIK.Instance.Setting_EmoteVRIK)
            {
                BodySystem.TrackingEnabled = false;
                //IKSystem.vrik.solver.Reset();
            }
            if (DesktopVRIK.Instance.Setting_EmoteLookAtIK && ___lookIK != null)
            {
                ___lookIK.enabled = false;
            }
        }
        else if (!____emotePlaying && emotePlayed)
        {
            emotePlayed = false;
            IKSystem.vrik.solver.Reset();
            BodySystem.TrackingEnabled = true;
            if (___lookIK != null)
            {
                ___lookIK.enabled = true;
            }
        }
    }
}