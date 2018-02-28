using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class UnsetForceFade : MonoBehaviour {
    // Since SteamVR settings can end up in various places, it's easiest to reset this setting by a script.
    // Note that the setting seems to only take effect the next time SteamVR is started, so make sure to
    // restart SteamVR after running this script.
    void Start () {
        var error = EVRSettingsError.None;
        if (OpenVR.Settings.GetBool(OpenVR.k_pch_SteamVR_Section, OpenVR.k_pch_SteamVR_ForceFadeOnBadTracking_Bool, ref error))
        {
            OpenVR.Settings.SetBool(OpenVR.k_pch_SteamVR_Section, OpenVR.k_pch_SteamVR_ForceFadeOnBadTracking_Bool, false, ref error);
            OpenVR.Settings.Sync(true, ref error);
        }
    }
}
