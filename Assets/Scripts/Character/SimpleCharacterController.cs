using System.Collections.Generic;
using UnityEngine;
using Scripts.Level;

namespace Scripts.Character
{
    public class SimpleCharacterController : MonoBehaviour
    {
        public float moveSpeed = 5f;

        public float jumpSpeed = 8f;
        
        public float rotationSpeed = 720f;

        public float gravity = -25f;

        public CharacterMover mover;

        public GroundDetector groundDetector;

        private const float minVerticalSpeed = -12f;

        // Allowed time before the character is set to ungrounded from the last time he was safely grounded.
        private const float timeBeforeUngrounded = 0.1f;

        // Speed along the character local up direction.
        private float verticalSpeed = 0f;

        // Time after which the character should be considered ungrounded.
        private float nextUngroundedTime = -1f;
        
        private List<MoveContact> moveContacts = new List<MoveContact>(10);


        private float GroundClampSpeed => -Mathf.Tan(Mathf.Deg2Rad * mover.maxFloorAngle) * moveSpeed;


        private void Update()
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            Vector3 moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;

            UpdateMovement(moveDirection, Time.deltaTime);
        }

        private void UpdateMovement(Vector3 moveDirection, float deltaTime)
        {
            Vector3 velocity = moveSpeed * moveDirection;
            PlatformDisplacement? platformDisplacement = null;

            bool groundDetected = groundDetector.DetectGround(out GroundInfo groundInfo);

            if (IsSafelyGrounded(groundDetected, groundInfo.isOnFloor))
                nextUngroundedTime = Time.time + timeBeforeUngrounded;

            bool isGrounded = Time.time < nextUngroundedTime;

            if (isGrounded && Input.GetButtonDown("Jump"))
            {
                verticalSpeed = jumpSpeed;
                nextUngroundedTime = -1f;
                isGrounded = false;
            }

            if (isGrounded)
            {
                mover.preventMovingUpSteepSlope = true;
                mover.canClimbSteps = true;

                verticalSpeed = 0f;
                velocity += GroundClampSpeed * transform.up;

                if (groundDetected && IsOnMovingPlatform(groundInfo.collider, out MovingPlatform movingPlatform))
                    platformDisplacement = GetPlatformDisplacementAtPoint(movingPlatform, groundInfo.point);
            }
            else
            {
                mover.preventMovingUpSteepSlope = false;
                mover.canClimbSteps = false;

                BounceDownIfTouchedCeiling();

                verticalSpeed += gravity * deltaTime;

                if (verticalSpeed < minVerticalSpeed)
                    verticalSpeed = minVerticalSpeed;

                velocity += verticalSpeed * transform.up;
            }

            mover.Move(velocity * deltaTime, moveContacts);

            if (platformDisplacement.HasValue)
                ApplyPlatformDisplacement(platformDisplacement.Value);
        }
                
        private bool IsSafelyGrounded(bool groundDetected, bool isOnFloor)
        {
            return groundDetected && isOnFloor && verticalSpeed < 0.1f;
        }

        private bool IsOnMovingPlatform(Collider groundCollider, out MovingPlatform platform)
        {
            return groundCollider.TryGetComponent(out platform);
        }

        private PlatformDisplacement GetPlatformDisplacementAtPoint(MovingPlatform platform, Vector3 point)
        {
            platform.GetDisplacement(out Vector3 platformDeltaPosition, out Quaternion platformDeltaRotation);
            Vector3 localPosition = point - platform.transform.position;
            Vector3 deltaPosition = platformDeltaPosition + platformDeltaRotation * localPosition - localPosition;

            platformDeltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            angle *= Mathf.Sign(Vector3.Dot(axis, transform.up));

            return new PlatformDisplacement()
            {
                deltaPosition = deltaPosition,
                deltaUpRotation = angle
            };
        }
        
        private void BounceDownIfTouchedCeiling()
        {
            for (int i = 0; i < moveContacts.Count; i++)
            {
                if (Vector3.Dot(moveContacts[i].normal, transform.up) < -0.7f)
                {
                    verticalSpeed = -0.25f * verticalSpeed;
                    break;
                }
            }
        }

        private void ApplyPlatformDisplacement(PlatformDisplacement platformDisplacement)
        {
            transform.Translate(platformDisplacement.deltaPosition, Space.World);
            transform.Rotate(0f, platformDisplacement.deltaUpRotation, 0f, Space.Self);
        }

        private struct PlatformDisplacement
        {
            public Vector3 deltaPosition;
            public float deltaUpRotation;
        }
    }
}
