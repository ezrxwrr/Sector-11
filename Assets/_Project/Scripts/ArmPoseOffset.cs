using System.Collections.Generic;
using UnityEngine;

public class ArmPoseOffset : MonoBehaviour
{
    [Header("Right Arm & Wrist (Degrees)")]
    [SerializeField] private Vector3 rightUpperArmOffset = Vector3.zero;
    [SerializeField] private Vector3 rightForeArmOffset = Vector3.zero;
    [SerializeField] private Vector3 rightHandOffset = Vector3.zero;

    [Header("Right Hand Grip (Degrees)")]
    [SerializeField] private Vector3 rightFingerCurl = Vector3.zero;
    [SerializeField] private Vector3 rightThumbCurl = Vector3.zero;

    private Animator _animator;
    private Transform _rightUpperArm;
    private Transform _rightForeArm;
    private Transform _rightHand;
    private readonly List<Transform> _rightFingers = new List<Transform>();
    private readonly List<Transform> _rightThumbs = new List<Transform>();

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null) return;

        _rightUpperArm = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _rightForeArm = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        _rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);

        if (_rightHand != null)
        {
            CollectFingerBones(_rightHand);
        }
    }

    private void CollectFingerBones(Transform parent)
    {
        foreach (Transform child in parent)
        {
            string boneName = child.name.ToLower();
            if (boneName.Contains("weaponholder")) continue;

            if (boneName.Contains("thumb"))
                _rightThumbs.Add(child);
            else if (boneName.Contains("index") || boneName.Contains("middle") || 
                     boneName.Contains("ring") || boneName.Contains("pinky"))
                _rightFingers.Add(child);

            CollectFingerBones(child);
        }
    }

    private void LateUpdate()
    {
        if (_rightUpperArm != null)
            _rightUpperArm.localRotation *= Quaternion.Euler(rightUpperArmOffset);

        if (_rightForeArm != null)
            _rightForeArm.localRotation *= Quaternion.Euler(rightForeArmOffset);

        if (_rightHand != null)
            _rightHand.localRotation *= Quaternion.Euler(rightHandOffset);

        Quaternion fingerRot = Quaternion.Euler(rightFingerCurl);
        for (int i = 0; i < _rightFingers.Count; i++)
            _rightFingers[i].localRotation *= fingerRot;

        Quaternion thumbRot = Quaternion.Euler(rightThumbCurl);
        for (int i = 0; i < _rightThumbs.Count; i++)
            _rightThumbs[i].localRotation *= thumbRot;
    }
}