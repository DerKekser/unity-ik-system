using System;
using UnityEngine;

namespace Kekser.IKSystem
{
    public class SmartFootIK : MonoBehaviour
    {
        public enum IkTypes
        {
            None,
            Simple,
            Complex
        }
        
        private Animator _animator;

        [SerializeField] 
        private IkTypes _currentIkType = IkTypes.Simple;
        [SerializeField] 
        private LayerMask _collisionMask = Physics.AllLayers;
        [SerializeField] 
        private float _stepHeight = 0.5f;
        [SerializeField] 
        private float _maxAvatarOffset = 0.5f;
        [SerializeField] 
        private bool _useAnimatorBottomHeight = true;
        [SerializeField] 
        private float _bottomHeight = 0.05f;

        private RaycastHit _leftFootHit;
        private RaycastHit _rightFootHit;

        public IkTypes CurrentIkType
        {
            get => _currentIkType;
            set => _currentIkType = value;
        }

        private float _lastLowestDelta = 0f;
        
        private void Awake()
        {
            if (!gameObject.TryGetComponent(out _animator))
            {
                Destroy(this);
            }
        }

        private float GetLeftBottomHeight(Animator animator)
        {
            return _useAnimatorBottomHeight ? animator.leftFeetBottomHeight : _bottomHeight;
        }

        private float GetRightBottomHeight(Animator animator)
        {
            return _useAnimatorBottomHeight ? animator.rightFeetBottomHeight : _bottomHeight;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            switch (_currentIkType)
            {
                case IkTypes.None:
                    break;
                case IkTypes.Simple:
                    SmartFootIKSimple();
                    break;
                case IkTypes.Complex:
                    SmartFootIKComplex();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SmartFootIKSimple()
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);

            Vector3 originBodyPosition = _animator.bodyPosition + Vector3.up * Mathf.Min(GetLeftBottomHeight(_animator), GetRightBottomHeight(_animator));
            Vector3 originLeftFootPosition = _animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up * GetLeftBottomHeight(_animator);
            Vector3 originRightFootPosition = _animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up * GetRightBottomHeight(_animator);
            Quaternion originLeftFootRotation = _animator.GetIKRotation(AvatarIKGoal.LeftFoot);
            Quaternion originRightFootRotation = _animator.GetIKRotation(AvatarIKGoal.RightFoot);

            Ray leftFootRay = new Ray(originLeftFootPosition + (Vector3.up * _stepHeight), Vector3.down);
            Vector3 targetLeftFootPosition = originLeftFootPosition;
            if (Physics.Raycast(leftFootRay, out _leftFootHit, GetLeftBottomHeight(_animator) + _stepHeight + _maxAvatarOffset, _collisionMask, QueryTriggerInteraction.Ignore))
                targetLeftFootPosition = _leftFootHit.point + Vector3.up * GetLeftBottomHeight(_animator);

            Ray rightFootRay = new Ray(originRightFootPosition + (Vector3.up * _stepHeight), Vector3.down);
            Vector3 targetRightFootPosition = originRightFootPosition;
            if (Physics.Raycast(rightFootRay, out _rightFootHit, GetRightBottomHeight(_animator) + _stepHeight + _maxAvatarOffset, _collisionMask, QueryTriggerInteraction.Ignore))
                targetRightFootPosition = _rightFootHit.point + Vector3.up * GetRightBottomHeight(_animator);

            float lowestDelta = 0;
            if (_leftFootHit.distance + _rightFootHit.distance == 0)
            {
                _lastLowestDelta = Mathf.LerpUnclamped(_lastLowestDelta, lowestDelta, 5 * Time.deltaTime);
                _animator.bodyPosition = originBodyPosition - (Vector3.up * _lastLowestDelta);
                _animator.SetIKPosition(AvatarIKGoal.LeftFoot, originLeftFootPosition - (Vector3.up * _lastLowestDelta));
                _animator.SetIKPosition(AvatarIKGoal.RightFoot, originRightFootPosition - (Vector3.up * _lastLowestDelta));
                return;
            }

            float bodyHeight = originBodyPosition.y;
            float leftFootOriginHeight = originLeftFootPosition.y;
            float rightFootOriginHeight = originRightFootPosition.y;
            float leftFootTargetHeight = targetLeftFootPosition.y;
            float rightFootTargetHeight = targetRightFootPosition.y;

            float lowestHeight =
                Mathf.Abs(bodyHeight - leftFootTargetHeight) > Mathf.Abs(bodyHeight - rightFootTargetHeight)
                    ? leftFootTargetHeight
                    : rightFootTargetHeight;

            float leftFootDelta = Mathf.Abs(lowestHeight - leftFootOriginHeight);
            float rightFootDelta = Mathf.Abs(lowestHeight - rightFootOriginHeight);

            lowestDelta = Mathf.Min(leftFootDelta, rightFootDelta);
            _lastLowestDelta = Mathf.LerpUnclamped(_lastLowestDelta, lowestDelta, 5 * Time.deltaTime);
            _animator.bodyPosition = originBodyPosition - (Vector3.up * _lastLowestDelta);

            float leftFootOriginDelta = Mathf.Abs(bodyHeight - leftFootOriginHeight);
            float rightFootOriginDelta = Mathf.Abs(bodyHeight - rightFootOriginHeight);
            float leftFootTargetDelta = Mathf.Abs((bodyHeight - _lastLowestDelta) - leftFootTargetHeight);
            float rightFootTargetDelta = Mathf.Abs((bodyHeight - _lastLowestDelta) - rightFootTargetHeight);

            float leftFootLowestDelta = Mathf.Min(leftFootOriginDelta, leftFootTargetDelta);
            if (_leftFootHit.normal == Vector3.zero)
                leftFootLowestDelta = Mathf.Max(leftFootOriginDelta, leftFootTargetDelta);
            float rightFootLowestDelta = Mathf.Min(rightFootOriginDelta, rightFootTargetDelta);
            if (_rightFootHit.normal == Vector3.zero)
                rightFootLowestDelta = Mathf.Max(rightFootOriginDelta, rightFootTargetDelta);

            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, _animator.bodyPosition - (Vector3.up * leftFootLowestDelta) + Vector3.Scale(Vector3.one - Vector3.up, originLeftFootPosition - originBodyPosition));
            if (_leftFootHit.normal != Vector3.zero)
            {
                Quaternion leftFootDeltaRotation = Quaternion.FromToRotation(Vector3.up, transform.InverseTransformDirection(_leftFootHit.normal));
                _animator.SetIKRotation(AvatarIKGoal.LeftFoot, originLeftFootRotation * leftFootDeltaRotation);
            }
            
            _animator.SetIKPosition(AvatarIKGoal.RightFoot, _animator.bodyPosition - (Vector3.up * rightFootLowestDelta) + Vector3.Scale(Vector3.one - Vector3.up, originRightFootPosition - originBodyPosition));
            if (_rightFootHit.normal != Vector3.zero)
            {
                Quaternion rightFootDeltaRotation = Quaternion.FromToRotation(Vector3.up, transform.InverseTransformDirection(_rightFootHit.normal));
                _animator.SetIKRotation(AvatarIKGoal.RightFoot, originRightFootRotation * rightFootDeltaRotation);
            }
        }

        private void SmartFootIKComplex()
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);

            Vector3 upDirection = transform.up;
            Vector3 inverseUp = Vector3.one - upDirection;

            Vector3 originBodyPosition = _animator.bodyPosition + upDirection * Mathf.Min(GetLeftBottomHeight(_animator), GetRightBottomHeight(_animator));
            Vector3 originLeftFootPosition = _animator.GetIKPosition(AvatarIKGoal.LeftFoot) + upDirection * GetLeftBottomHeight(_animator);
            Vector3 originRightFootPosition = _animator.GetIKPosition(AvatarIKGoal.RightFoot) + upDirection * GetRightBottomHeight(_animator);
            Quaternion originLeftFootRotation = _animator.GetIKRotation(AvatarIKGoal.LeftFoot);
            Quaternion originRightFootRotation = _animator.GetIKRotation(AvatarIKGoal.RightFoot);

            Ray leftFootRay = new Ray(originLeftFootPosition + (upDirection * _stepHeight), -upDirection);
            Vector3 targetLeftFootPosition = originLeftFootPosition;
            if (Physics.Raycast(leftFootRay, out _leftFootHit, GetLeftBottomHeight(_animator) + _stepHeight + _maxAvatarOffset, _collisionMask, QueryTriggerInteraction.Collide))
                targetLeftFootPosition = _leftFootHit.point + upDirection * GetLeftBottomHeight(_animator);

            Ray rightFootRay = new Ray(originRightFootPosition + (upDirection * _stepHeight), -upDirection);
            Vector3 targetRightFootPosition = originRightFootPosition;
            if (Physics.Raycast(rightFootRay, out _rightFootHit, GetRightBottomHeight(_animator) + _stepHeight + _maxAvatarOffset, _collisionMask, QueryTriggerInteraction.Ignore))
                targetRightFootPosition = _rightFootHit.point + upDirection * GetRightBottomHeight(_animator);

            float lowestDelta = 0;
            if (_leftFootHit.distance + _rightFootHit.distance == 0)
            {
                _lastLowestDelta = Mathf.LerpUnclamped(_lastLowestDelta, lowestDelta, 5 * Time.deltaTime);
                _animator.bodyPosition = originBodyPosition - (upDirection * _lastLowestDelta);
                _animator.SetIKPosition(AvatarIKGoal.LeftFoot, originLeftFootPosition - (upDirection * _lastLowestDelta));
                _animator.SetIKPosition(AvatarIKGoal.RightFoot, originRightFootPosition - (upDirection * _lastLowestDelta));
                return;
            }

            Vector3 bodyHeight = Vector3.Scale(upDirection, originBodyPosition);
            Vector3 leftFootOriginHeight = Vector3.Scale(upDirection, originLeftFootPosition);
            Vector3 rightFootOriginHeight = Vector3.Scale(upDirection, originRightFootPosition);
            Vector3 leftFootTargetHeight = Vector3.Scale(upDirection, targetLeftFootPosition);
            Vector3 rightFootTargetHeight = Vector3.Scale(upDirection, targetRightFootPosition);

            Vector3 lowestHeight =
                Vector3.Distance(bodyHeight, leftFootTargetHeight) > Vector3.Distance(bodyHeight, rightFootTargetHeight)
                    ? leftFootTargetHeight
                    : rightFootTargetHeight;

            float leftFootDelta = Vector3.Distance(lowestHeight, leftFootOriginHeight);
            float rightFootDelta = Vector3.Distance(lowestHeight, rightFootOriginHeight);

            lowestDelta = Mathf.Min(leftFootDelta, rightFootDelta);
            _lastLowestDelta = Mathf.LerpUnclamped(_lastLowestDelta, lowestDelta, 5 * Time.deltaTime);
            _animator.bodyPosition = originBodyPosition - (upDirection * _lastLowestDelta);

            float leftFootOriginDelta = Vector3.Distance(bodyHeight, leftFootOriginHeight);
            float rightFootOriginDelta = Vector3.Distance(bodyHeight, rightFootOriginHeight);
            float leftFootTargetDelta = Vector3.Distance(bodyHeight - (upDirection * _lastLowestDelta), leftFootTargetHeight);
            float rightFootTargetDelta = Vector3.Distance(bodyHeight - (upDirection * _lastLowestDelta), rightFootTargetHeight);

            float leftFootLowestDelta = Mathf.Min(leftFootOriginDelta, leftFootTargetDelta);
            if (_leftFootHit.normal == Vector3.zero)
                leftFootLowestDelta = Mathf.Max(leftFootOriginDelta, leftFootTargetDelta);
            float rightFootLowestDelta = Mathf.Min(rightFootOriginDelta, rightFootTargetDelta);
            if (_rightFootHit.normal == Vector3.zero)
                rightFootLowestDelta = Mathf.Max(rightFootOriginDelta, rightFootTargetDelta);

            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, _animator.bodyPosition - (upDirection * leftFootLowestDelta) + Vector3.Scale(inverseUp, originLeftFootPosition - originBodyPosition));
            if (_leftFootHit.normal != Vector3.zero)
            {
                Quaternion leftFootDeltaRotation = Quaternion.FromToRotation(upDirection, transform.InverseTransformDirection(_leftFootHit.normal));
                _animator.SetIKRotation(AvatarIKGoal.LeftFoot, originLeftFootRotation * leftFootDeltaRotation);
            }
            
            _animator.SetIKPosition(AvatarIKGoal.RightFoot, _animator.bodyPosition - (upDirection * rightFootLowestDelta) + Vector3.Scale(inverseUp, originRightFootPosition - originBodyPosition));
            if (_rightFootHit.normal != Vector3.zero)
            {
                Quaternion rightFootDeltaRotation = Quaternion.FromToRotation(upDirection, transform.InverseTransformDirection(_rightFootHit.normal));
                _animator.SetIKRotation(AvatarIKGoal.RightFoot, originRightFootRotation * rightFootDeltaRotation);
            }
        }
    }   
}
