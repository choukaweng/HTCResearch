using System;
using System.Runtime.InteropServices;

class LpmsVrInterface
{
    private IntPtr m_sfs;

    private LpmsVrInterface(IntPtr sfs)
    {
        m_sfs = sfs;
    }

    public static LpmsVrInterface factory(string sensorId = "")
    {
        System.Text.ASCIIEncoding str = new System.Text.ASCIIEncoding();
        IntPtr sfs = lpStartSensorFusion(str.GetBytes(sensorId));
        if (sfs == IntPtr.Zero)
        {
            return null;
        }
        return new LpmsVrInterface(sfs);
    }

    ~LpmsVrInterface()
    {
        lpStopSensorFusion(m_sfs);
    }

    public void setLatencyTuning(double time)
    {
        lpSetLatencyTuning(m_sfs, time);
    }

    public void setCurrentOrientation(UnityEngine.Quaternion q)
    {
        lpSetCurrentOrientation(m_sfs, convertToLpsfQuaternion(q));
    }

    public void setCurrentOrientationSoft(UnityEngine.Quaternion q)
    {
        lpSetCurrentOrientationSoft(m_sfs, convertToLpsfQuaternion(q));
    }

    public void setCurrentYaw(UnityEngine.Quaternion q)
    {
        lpSetCurrentYaw(m_sfs, convertToLpsfQuaternion(q));
    }

    public void setCurrentYawSoft(UnityEngine.Quaternion q)
    {
        lpSetCurrentYawSoft(m_sfs, convertToLpsfQuaternion(q));
    }

    public UnityEngine.Quaternion getOrientation(double whenFromNow)
    {
        double[] q = new double[] { 1, 0, 0, 0 };
        lpGetOrientation(m_sfs, whenFromNow, q);
        return MapOrientation(q);
    }

    public void setAssemblyRotation(int idx)
    {
        double invsq2 = 1 / Math.Sqrt(2);
        double[][] assemblyRots = {
                new double[] { 1, 0, 0, 0 },
                new double[] { 0.5, -0.5, -0.5, 0.5 },
                new double[] { 0.5, -0.5, 0.5, -0.5 },
                new double[] { 0.5, 0.5, 0.5, -0.5 },
                new double[] { invsq2, 0, invsq2, 0 },
                new double[] { invsq2, invsq2, 0, 0 },
                new double[] { invsq2, 0, 0, invsq2 },
                new double[] { invsq2, -invsq2, 0, 0 },
                new double[] { invsq2, 0, 0, -invsq2 },
                new double[] { 0, invsq2, invsq2, 0 },
                new double[] { 0, invsq2, 0, invsq2 },
                new double[] { 0, 0, invsq2, invsq2 } };

        idx = idx < 0 ? 0 : (idx > assemblyRots.Length ? assemblyRots.Length : idx);
        lpSetAssemblyRotation(m_sfs, assemblyRots[idx]);
    }

    UnityEngine.Quaternion MapOrientation(double[] q)
    {
        UnityEngine.Quaternion rot = convertFromLpsfQuaternion(q);
        // If necessary, add transformation here.
        return rot;
    }
    static UnityEngine.Quaternion convertFromLpsfQuaternion([In] double[] q)
    {
        return new UnityEngine.Quaternion
        {
            w = (float)q[0],
            x = (float)q[1],
            y = (float)q[2],
            z = -(float)q[3]
        };
    }

    static double[] convertToLpsfQuaternion(UnityEngine.Quaternion q)
    {
        return new double[]
        {
                q.w,
                q.x,
                q.y,
                -q.z
        };
    }

    private const string dllName = "LPMS-VR";
    private const CallingConvention cc = CallingConvention.Winapi;

    [DllImport(dllName, CallingConvention = cc)]
    private static extern IntPtr lpStartSensorFusion(byte[] str);

    [DllImport(dllName, CallingConvention = cc)]
    private static extern IntPtr lpStopSensorFusion(IntPtr sfs);


    [DllImport(dllName, CallingConvention = cc)]
    private static extern IntPtr lpSetLatencyTuning(IntPtr sfs, [In] double time);

    [DllImport(dllName, CallingConvention = cc)]
    private static extern void lpSetAssemblyRotation(IntPtr sfs, [In] double[] q);

    [DllImport(dllName, CallingConvention = cc)]
    public static extern IntPtr lpSetCurrentOrientation(IntPtr sfs, [In] double[] q);

    [DllImport(dllName, CallingConvention = cc)]
    public static extern IntPtr lpSetCurrentYaw(IntPtr sfs, [In] double[] q);

    [DllImport(dllName, CallingConvention = cc)]
    public static extern IntPtr lpSetCurrentOrientationSoft(IntPtr sfs, [In] double[] q);

    [DllImport(dllName, CallingConvention = cc)]
    public static extern IntPtr lpSetCurrentYawSoft(IntPtr sfs, [In] double[] q);

    [DllImport(dllName, CallingConvention = cc)]
    private static extern IntPtr lpGetOrientation(IntPtr sfs, double whenFromNow, [Out] double[] q);
}
