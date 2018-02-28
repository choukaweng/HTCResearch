//======================================================================================================
// Copyright 2016, NaturalPoint Inc.
//======================================================================================================

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class LpmsVrWithOptitrackHmd : MonoBehaviour
{
    public OptitrackStreamingClient StreamingClient;
    public Int32 RigidBodyId;

    [Tooltip("Typically, Unity has a latency of one frame from where the pose can last be updated.  Timing can be tuned by changing this value")]
    public double nFramesAddedLatency = 1;
    [Tooltip("Sometimes the coordinate convention for the headset is different, this variable allows finding the correct convention.  "
            + "Cycle through this (values 0-11) when rotations go around the wrong axis")]
    public int axisMismatchCorrection = 0;

    [Tooltip("If not set, don't use Optitrack, only IMU orientation data.  This is for testing only.")]
    public bool useOptitrack = false;

    [Tooltip("Enable drift correction.  Disable if using recorded Motive data.")]
    public bool enableDriftCorrection = true;

    [Tooltip("Connect to a specific sensor.")]
    public string sensorId = "";

    private LpmsVrInterface m_fusion = null;
    private object m_fusion_lock = new object();
    private OptitrackHiResTimer.Timestamp m_lastTimestamp;
    private bool m_initialPoseSet = false;


    void Start()
    {
        if (useOptitrack)
        {
            // If the user didn't explicitly associate a client, find a suitable default.
            if ( this.StreamingClient == null )
            {
                this.StreamingClient = OptitrackStreamingClient.FindDefaultClient();

                // If we still couldn't find one, disable this component.
                if ( this.StreamingClient == null )
                {
                    Debug.LogError( GetType().FullName + ": Streaming client not set, and no " + typeof( OptitrackStreamingClient ).FullName + " components found in scene; disabling this component.", this );
                    this.enabled = false;
                    return;
                }
            }
        }

        if ( UnityEngine.VR.VRDevice.isPresent )
        {
            string vrDeviceFamily = UnityEngine.VR.VRSettings.loadedDeviceName;
            string vrDeviceModel = UnityEngine.VR.VRDevice.model;
            bool isOpenVR = String.Equals( vrDeviceFamily, "OpenVR", StringComparison.CurrentCultureIgnoreCase);

            if (isOpenVR)
            {
                Debug.Log(GetType().FullName + ": found OpenVR headset\"" + vrDeviceModel + "\".", this);
            }
            else
            {
                Debug.LogWarning( GetType().FullName + ": Unsupported HMD type (\"" + vrDeviceFamily + "\", \"" + vrDeviceModel + "\").", this );
            }
        }
        else
        {
            Debug.LogWarning( GetType().FullName + ": No VRDevice present.", this );
        }

        // Cache a reference to the gameobject containing the HMD Camera.
        Camera hmdCamera = this.GetComponentInChildren<Camera>();
        if ( hmdCamera == null )
        {
            Debug.LogError( GetType().FullName + ": Couldn't locate HMD-driven Camera component in children.", this );
        }
    }

    void OnEnable()
    {
        this.enabled = TryInit();
    }

    bool TryInit()
    {
        lock(m_fusion_lock) {
            if (m_fusion == null) {
                m_fusion = LpmsVrInterface.factory(sensorId);
            }

            if (m_fusion == null) {
                Debug.LogError( "SensorFusion did not start", this);
                return false;
            }

            float fps = UnityEngine.VR.VRDevice.refreshRate;
            m_fusion.setLatencyTuning(nFramesAddedLatency / fps);

            if (axisMismatchCorrection >= 0) {
                m_fusion.setAssemblyRotation(axisMismatchCorrection);
            }
        }
        Camera.onPreCull += OnCameraPreCull;

        return true;
    }

    void OnDisable()
    {
        lock(m_fusion_lock) {
            if (m_fusion != null)
            {
                Camera.onPreCull -= OnCameraPreCull;

                Debug.Log("Stopping lpSensorfusion", this);
                m_fusion = null;
            }
        }
    }

    void Update()
    {
        if (!useOptitrack) {
            this.transform.localPosition = new Vector3(0, 0, 0);
            if (!m_initialPoseSet) {
                m_initialPoseSet = true;
                m_fusion.setCurrentOrientation(this.transform.localRotation);
            }
            return;
        }

        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState( RigidBodyId );
        if ( rbState != null && rbState.DeliveryTimestamp.AgeSeconds < 1.0f )
        {
            Debug.Log("new position", this);
            // Update position.
            this.transform.localPosition = rbState.Pose.Position;

            // Update drift correction.
            if (m_lastTimestamp.AgeSeconds != rbState.DeliveryTimestamp.AgeSeconds) {
                // If we're coming here the first time: reset to optical pose.  Otherwise
                // we're using the optical orientation for drift correction.
                if (!m_initialPoseSet) {
                    m_initialPoseSet = true;
                    m_fusion.setCurrentYaw(rbState.Pose.Orientation);
                }
                else if (enableDriftCorrection) {
                    m_fusion.setCurrentYawSoft(rbState.Pose.Orientation);
                }
                m_lastTimestamp = rbState.DeliveryTimestamp;
            }
        }
    }
    void UpdatePose()
    {
        if (m_fusion == null)
            return;

        // Try to get the remaining time in the frame from the compositor, otherwise
        // default back to 90 fps.  If the scene renders in reasonable time, the time
        // will be slightly larger than just the inverse of the framerate.  If the
        // value becomes negative, as can happen when the image processing falls
        // behind, we force it to zero.
        float frameTimeRemaining = 1/90.0f;
        var vr = SteamVR.instance;
        if (vr != null)
        {
            frameTimeRemaining = vr.compositor.GetFrameTimeRemaining();
            frameTimeRemaining = Math.Max(frameTimeRemaining, 0.0f);
        }

        lock(m_fusion_lock)
        {
            if (m_fusion != null) {
                Quaternion newOrientation = m_fusion.getOrientation(frameTimeRemaining);
                this.transform.localRotation = newOrientation;
            }
        }
    }


    void OnCameraPreCull(Camera cam)
    {
        // Only update poses on the first camera per frame.
        if (Time.frameCount != lastFrameCount)
        {
            lastFrameCount = Time.frameCount;
            UpdatePose();
        }
    }
    static int lastFrameCount = -1;

    private enum NpHmdResult
    {
        OK = 0,
        InvalidArgument
    }


    private struct NpHmdQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public NpHmdQuaternion( UnityEngine.Quaternion other )
        {
            this.x = other.x;
            this.y = other.y;
            this.z = other.z;
            this.w = other.w;
        }

        public static implicit operator UnityEngine.Quaternion( NpHmdQuaternion nphmdQuat )
        {
            return new UnityEngine.Quaternion
            {
                w = nphmdQuat.w,
                x = nphmdQuat.x,
                y = nphmdQuat.y,
                z = nphmdQuat.z
            };
        }
    }


    private static class NativeMethods
    {
        public const string NpHmdDllBaseName = "HmdDriftCorrection";
        public const CallingConvention NpHmdDllCallingConvention = CallingConvention.Cdecl;

        [DllImport( NpHmdDllBaseName, CallingConvention = NpHmdDllCallingConvention )]
        public static extern NpHmdResult NpHmd_UnityInit();

        [DllImport( NpHmdDllBaseName, CallingConvention = NpHmdDllCallingConvention )]
        public static extern NpHmdResult NpHmd_Create( out IntPtr hmdHandle );

        [DllImport( NpHmdDllBaseName, CallingConvention = NpHmdDllCallingConvention )]
        public static extern NpHmdResult NpHmd_Destroy( IntPtr hmdHandle );

        [DllImport( NpHmdDllBaseName, CallingConvention = NpHmdDllCallingConvention )]
        public static extern NpHmdResult NpHmd_MeasurementUpdate( IntPtr hmdHandle, ref NpHmdQuaternion opticalOrientation, ref NpHmdQuaternion inertialOrientation, float deltaTimeSec );

        [DllImport( NpHmdDllBaseName, CallingConvention = NpHmdDllCallingConvention )]
        public static extern NpHmdResult NpHmd_GetOrientationCorrection( IntPtr hmdHandle, out NpHmdQuaternion correction );


        public const string OvrPluginDllBaseName = "OVRPlugin";
        public const CallingConvention OvrPluginDllCallingConvention = CallingConvention.Cdecl;

        [DllImport( OvrPluginDllBaseName, CallingConvention = OvrPluginDllCallingConvention )]
        public static extern Int32 ovrp_GetCaps();

        [DllImport( OvrPluginDllBaseName, CallingConvention = OvrPluginDllCallingConvention )]
        public static extern Int32 ovrp_SetCaps( Int32 caps );

        [DllImport( OvrPluginDllBaseName, CallingConvention = OvrPluginDllCallingConvention )]
        public static extern Int32 ovrp_SetTrackingIPDEnabled( Int32 value );
    }


    private bool TryDisableOvrPositionTracking()
    {
        try
        {
            const Int32 kCapsPositionBit = (1 << 5);
            Int32 oldCaps = NativeMethods.ovrp_GetCaps();
            bool bSucceeded = NativeMethods.ovrp_SetCaps( oldCaps & ~kCapsPositionBit ) != 0;

            try
            {
                NativeMethods.ovrp_SetTrackingIPDEnabled( 1 );
            }
            catch ( Exception ex )
            {
                Debug.LogError( GetType().FullName + ": ovrp_SetTrackingIPDEnabled failed. OVRPlugin too old?", this );
                Debug.LogException( ex, this );
                bSucceeded = false;
            }

            return bSucceeded;
        }
        catch ( Exception ex )
        {
            Debug.LogException( ex, this );
            return false;
        }
    }
}
