﻿using ABI_RC.Core.IO;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.UI;
using ABI_RC.Systems.GameEventSystem;
using JetBrains.Annotations;
using NAK.Stickers.Networking;
using NAK.Stickers.Utilities;
using System.EnterpriseServices;
using UnityEngine;
using MelonLoader;
using UnityEngine.ProBuilder.MeshOperations;
using NAK.Stickers.Integrations;

namespace NAK.Stickers;

public partial class StickerSystem
{
    #region Singleton

    public static bool RestrictedInstance = false;

    public static StickerSystem Instance { get; private set; }

    public static void Initialize()
    {
        if (Instance != null)
            return;
        
        Instance = new StickerSystem();
        
        // configure decalery
        DecalManager.SetPreferredMode(DecalUtils.Mode.GPU, false, 0);
        
        // ensure cache folder exists
        EnsureStickersFolderExists();
        
        // listen for game events
        CVRGameEventSystem.Initialization.OnPlayerSetupStart.AddListener(Instance.OnPlayerSetupStart);

}
    
    #endregion Singleton

    #region Callback Registration
    
    private void OnPlayerSetupStart()
    {
        CVRGameEventSystem.World.OnUnload.AddListener(_ => OnWorldUnload());
        CVRGameEventSystem.World.OnLoad.AddListener(_ => OnWorldLoad());
        CVRGameEventSystem.Instance.OnConnected.AddListener((_) => { if (!Instances.IsReconnecting) OnInitialConnection(); });
        
        CVRGameEventSystem.Player.OnJoinEntity.AddListener(Instance.OnPlayerJoined);
        CVRGameEventSystem.Player.OnLeaveEntity.AddListener(Instance.OnPlayerLeft);
        SchedulerSystem.AddJob(Instance.OnUpdate, 10f, -1);
        LoadAllImagesAtStartup();
    }

    #endregion Callback Registration
    
    #region Game Events
    
    private void OnInitialConnection()
    {
        OnWorldLoad(); //Checks the world again in case the bundle updated.
        ClearStickersSelf(); // clear stickers on remotes just in case we rejoined
        ModNetwork.Reset(); // reset network buffers and metadata
    }

    private void OnWorldLoad()
    {
        GameObject StickerWorldRestriction = GameObject.Find("[DisableStickers]");
        if (StickerWorldRestriction != null)
            {
                RestrictedInstance = true;
                MelonLogger.Msg("This is a Restricted Instance");
            }
        else
            {
                MelonLogger.Msg("This is NOT a Restricted Instance");
            }
        BTKUIAddon.UpdateStickerMenu();
    }

    private void OnWorldUnload()
    {
        RestrictedInstance = false;
        CleanupAllButSelf(); // release all stickers except for self
    }
    
    #endregion Game Events

    #region Data

    // private bool _isEnabled = true;
    //
    // public bool IsEnabled
    // {
    //     get => _isEnabled;
    //     set
    //     {
    //         if (_isEnabled == value) 
    //             return;
    //         
    //         _isEnabled = value;
    //         if (!_isEnabled) ClearAllStickers();
    //         ModNetwork.IsEnabled = _isEnabled;
    //     }
    // }
    
    private string SelectedStickerName => ModSettings.Hidden_SelectedStickerNames.Value[_selectedStickerSlot];

    private const float StickerKillTime = 30f;
    private const float StickerCooldown = 0.2f;
    private readonly Dictionary<string, StickerData> _playerStickers = new();
    internal const string PlayerLocalId = "_PLAYERLOCAL";
    
    private int _selectedStickerSlot;
    public int SelectedStickerSlot
    {
        get => _selectedStickerSlot;
        set
        {
            _selectedStickerSlot = value < 0 ? ModSettings.MaxStickerSlots - 1 : value % ModSettings.MaxStickerSlots;
            IsInStickerMode = IsInStickerMode; // refresh sticker mode
        }
    }
    
    private bool _isInStickerMode;
    public bool IsInStickerMode 
    {
        get => _isInStickerMode;
        set
        {
            _isInStickerMode = value;
            if (_isInStickerMode)
            {
                CohtmlHud.Instance.SelectPropToSpawn(
                StickerCache.GetCohtmlResourcesPath(SelectedStickerName),
                Path.GetFileNameWithoutExtension(SelectedStickerName), 
                "Sticker selected for stickering:");
            }
            else
            {
                CohtmlHud.Instance.ClearPropToSpawn();
                ClearStickerPreview();
            }
        }
    }
    
    #endregion Data
}