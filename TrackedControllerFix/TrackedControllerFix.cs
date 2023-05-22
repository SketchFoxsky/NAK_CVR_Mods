﻿using UnityEngine;
using Valve.VR;

namespace NAK.TrackedControllerFix;

public class TrackedControllerFixer : MonoBehaviour
{
    public SteamVR_Input_Sources inputSource;
    public int deviceIndex;

    SteamVR_TrackedObject trackedObject;
    SteamVR_Behaviour_Pose oldBehaviourPose;
    SteamVR_Action_Pose actionPose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose", false);

    public void Initialize()
    {
        trackedObject = gameObject.AddComponent<SteamVR_TrackedObject>();
        oldBehaviourPose = gameObject.GetComponent<SteamVR_Behaviour_Pose>();
        oldBehaviourPose.broadcastDeviceChanges = false; //this fucks us
        if (actionPose != null) CheckDeviceIndex();
    }

    void OnEnable()
    {
        if (actionPose != null) actionPose[inputSource].onDeviceConnectedChanged += OnDeviceConnectedChanged;
        if (oldBehaviourPose != null)
            oldBehaviourPose.enabled = false;
    }

    void OnDisable()
    {
        if (actionPose != null) actionPose[inputSource].onDeviceConnectedChanged -= OnDeviceConnectedChanged;
        if (oldBehaviourPose != null)
            oldBehaviourPose.enabled = true;
    }

    void OnDeviceConnectedChanged(SteamVR_Action_Pose changedAction, SteamVR_Input_Sources changedSource, bool connected)
    {
        if (actionPose != changedAction) actionPose = changedAction;
        if (changedSource != inputSource) return;
        CheckDeviceIndex();
    }

    void CheckDeviceIndex()
    {
        if (actionPose[inputSource].active && actionPose[inputSource].deviceIsConnected)
        {
            int trackedDeviceIndex = (int)actionPose[inputSource].trackedDeviceIndex;
            if (deviceIndex != trackedDeviceIndex)
            {
                deviceIndex = trackedDeviceIndex;
                trackedObject.SetDeviceIndex(deviceIndex);
            }
        }
    }
}