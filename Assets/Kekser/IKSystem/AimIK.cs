using System;
using UnityEngine;

namespace Kekser.IKSystem
{
    public class AimIK : MonoBehaviour
    {
        [Serializable]
        private struct BoneInfo
        {
            [SerializeField] 
            private Transform[] _boneTransforms;
            [SerializeField, Range(0f, 1f)] 
            private float _weight;
            [SerializeField] 
            private Vector3 _rotationOffset;

            public Transform[] BoneTransforms => _boneTransforms;
            public float Weight => _weight;
            public Vector3 RotationOffset => _rotationOffset;
        }

        [SerializeField] 
        private BoneInfo[] _bones;
        [SerializeField] 
        private Transform _headRig;
        [SerializeField] 
        private Transform _headTarget;
        
        private Animator _animator;

        private void Awake()
        {
            if (!gameObject.TryGetComponent(out _animator))
            {
                Destroy(this);
            }
        }

        private void LateUpdate()
        {
            float maxBlend = 0;
            float maxWeight = 0;
            foreach (BoneInfo bone in _bones)
            {
                maxBlend = Mathf.Max(maxBlend, bone.Weight);
                maxWeight += bone.Weight;
            }

            if (maxWeight == 0)
                return;

            foreach (BoneInfo bone in _bones)
            {
                foreach (Transform boneTransform in bone.BoneTransforms)
                {
                    Quaternion deltaRotation = Quaternion.FromToRotation(
                        Quaternion.Inverse(_headRig.rotation * Quaternion.Euler(bone.RotationOffset)) * boneTransform.forward, 
                        Quaternion.Inverse(_headRig.rotation * Quaternion.Euler(bone.RotationOffset)) * boneTransform.TransformDirection(
                            Quaternion.Inverse(_headRig.rotation) * (_headTarget.position - _headRig.position)
                        ).normalized
                    );
                    
                    Quaternion target = boneTransform.rotation * deltaRotation;
                    boneTransform.rotation = Quaternion.Lerp(boneTransform.rotation, target, (bone.Weight / maxWeight) * maxBlend);
                }
            }
        }
    }
}