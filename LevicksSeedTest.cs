using BepInEx;
using BepInEx.Logging;
using RWCustom;
using System;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace LevicksSeedTest; 

[BepInPlugin("Levick.SeedTest", "SeedTest", "1.0.0")]
public partial class LevicksSeedTest : BaseUnityPlugin
{
    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
    }

    private bool IsInit;
    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        if (IsInit) return;

        try
        {
            IsInit = true;

            SetInit(base.Logger);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

}
