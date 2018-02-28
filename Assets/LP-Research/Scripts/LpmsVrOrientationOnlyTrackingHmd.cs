//======================================================================================================
// Copyright 2017, LP-Research Inc.
//======================================================================================================

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class LpmsVrOrientationOnlyTrackingHmd : MonoBehaviour
{
    [Tooltip("Typically, Unity has a latency of one frame from where the pose can last be updated.  Timing can be tuned by changing this value.")]
    public double nFramesAddedLatency = 1;

    [Tooltip("Sometimes the coordinate convention for the headset is different, this variable allows finding the correct convention.  "
            + "Cycle through this (values 0-11) when rotations go around the wrong axis.")]
    public int axisMismatchCorrection = 0;

    [Tooltip("Position of the head, i.e. the center of rotation.")]
    public Vector3 headPosition = new Vector3(0, 1, 0);

    [Tooltip("Connect to a specific sensor.")]
    public string sensorId = "";

    private LpmsVrInterface m_fusion = null;
    private object m_fusion_lock = new object();
    private bool m_initialPoseSet = false;


    void Start()
    {
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
        this.transform.localPosition = headPosition;
        if (!m_initialPoseSet) {
            m_initialPoseSet = true;
            m_fusion.setCurrentYaw(this.transform.localRotation);
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

    Quaternion MapOrientation(double [] q)
    {
        UnityEngine.Quaternion rot = convertFromLpsfQuaternion(q);
        // If necessary, add transformation here.
        return rot;
    }

    private UnityEngine.Quaternion convertFromLpsfQuaternion( [In] double[] q )
    {
        return new UnityEngine.Quaternion
        {
            w = (float)q[0],
            x = (float)q[1],
            y = (float)q[2],
            z = -(float)q[3]
        };
    }

    private double[] convertToLpsfQuaternion( UnityEngine.Quaternion q )
    {
        return new double []
         {
             q.w,
             q.x,
             q.y,
             -q.z
         };
    }
}
