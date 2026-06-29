using System;
using UnityEngine;

[Serializable]
public struct WeaponPose
{
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEulerAngles;

    public Vector3 LocalPosition => localPosition;
    public Vector3 LocalEulerAngles => localEulerAngles;

    public void ApplyTo(Transform target)
    {
        if (target == null)
            return;

        target.localPosition = localPosition;
        target.localRotation = Quaternion.Euler(localEulerAngles);
        target.localScale = Vector3.one;
    }
}
