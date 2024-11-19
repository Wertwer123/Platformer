using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(Stats))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour 
    {
        [SerializeField] private float gravity =  9.81f;
        [SerializeField] private float jumpForce =  10.81f;
        [SerializeField] private float maxJumpTime =  0.5f;
        [SerializeField, Min(1.0f)] private float dashDistance = 200.0f;
        [SerializeField, Min(1.0f)] private float dashSpeed = 2.0f;
        [SerializeField, Min(0.0f)] private float coyoteTime = 0.1f;
        [SerializeField, Range(0, 1.0f)] private float groundCheckOffset =  0.2f;
        [SerializeField, Range(0, 2.0f)] private float timeUntilMaxGravity =  0.8f;
        [SerializeField, Min(1.0f)] private float maxFallSpeed = 0.0f;
        [SerializeField, Min(1)] private int maxJumps = 2;
        [SerializeField] private string groundTag = "Ground";
        [SerializeField] private LayerMask collisionLayer;

        private bool isDashing = false;
        private bool wasGroundedLastFrame = false;
        private bool isGrounded = false;
        private bool isMoving = true;
        private bool applyGravity = false;
        private bool isJumping = false;
        private int timesJumped = 0;
        private float timeJumping = 0.0f;
        private Stats stats = null;
        private Rigidbody2D rb;
        private BoxCollider2D collider = null;
        private RaycastHit2D currentGroundHit;
        private Vector2 velocity = Vector2.zero;
        private Vector2 currentMovementInput = Vector2.zero;

        private Coroutine coyoteTimeRoutine = null;
        private Coroutine dashRoutine = null;
        private void Awake()
        {
            stats = GetComponent<Stats>();
            collider = GetComponent<BoxCollider2D>();
            rb = GetComponent<Rigidbody2D>();

            //If we arent grounded at start apply air time and gravity
            if (!IsGrounded())
            {
                applyGravity = true;
            }
        }

        private void Update()
        {
            ApplyMoveForce();
            ApplyJumpForce();
            ApplyGravity();
            AllignMoveForceToGround();
        }

        private void FixedUpdate()
        {
            PerformFixedUpdate();
        }
        

        public void Move(InputAction.CallbackContext context)
        {
            Vector2 movementInput = context.ReadValue<Vector2>();

            if (context.started)
            {
                isMoving = true;
                currentMovementInput = movementInput;
            }
            else if (context.canceled)
            {
                isMoving = false;
                currentMovementInput = movementInput;
            }
        }

        public void Jump(InputAction.CallbackContext context)
        {
            if (context.started && timesJumped < maxJumps)
            {
                if (coyoteTimeRoutine != null)
                {
                    StopCoroutine(coyoteTimeRoutine);
                    coyoteTimeRoutine = null;
                }

                timesJumped++;
                isJumping = true;
            }
            else if (context.canceled)
            {
                isJumping = false;
                timeJumping = 0.0f;
            }
        }

        void PerformFixedUpdate()
        {
            if (isDashing)
            {
                return;
            }
            
            wasGroundedLastFrame = isGrounded;
            isGrounded = IsGrounded(); 
            applyGravity = !isGrounded;
            
            rb.MovePosition(rb.position + velocity * Time.deltaTime);
        }
        void ApplyMoveForce()
        {
            if (isDashing)
            {
                return;
            }
            if (isMoving)
            {
                velocity.x += currentMovementInput.x * stats.GetMoveSpeed() * stats.GetAcceleration() * Time.deltaTime;
                velocity.x = Mathf.Clamp(velocity.x, -stats.GetMaxSpeed(), stats.GetMaxSpeed());
            }
            else
            {
                velocity.x = 0;
            }
        }

        void AllignMoveForceToGround()
        {
            if (isGrounded && !isJumping)
            {
                velocity = Vector3.ProjectOnPlane(velocity, currentGroundHit.normal);
            }
        }
        void ApplyJumpForce()
        {
            if (isDashing)
            {
                return;
            }
            
            if (!CanJump() && coyoteTimeRoutine == null)
            {
                isJumping = false;
            }
            else if (isJumping && timesJumped <= maxJumps)
            {
                timeJumping += Time.deltaTime;
                velocity.y = jumpForce;
            }
        }
        public void ApplyGravity()
        {
            if (isGrounded || isDashing)
            {
                return;
            }
            
            if (!isJumping && applyGravity)
            {
                velocity.y -= gravity;
                velocity.y = Mathf.Clamp(velocity.y, -maxFallSpeed, float.MaxValue);
            }
        }
        
        bool IsFalling()
        {
            return velocity.y < 0;
        }
        bool CanJump()
        {
            return timeJumping < maxJumpTime;
        }
        bool IsGrounded()
        {
            Vector2 rayStart = transform.position;
            Vector2 rayDirection = Vector2.down;
            //Ground tag and layer should be named the same
            LayerMask groundLayer = LayerMask.GetMask(groundTag);
            currentGroundHit = Physics2D.BoxCast(
                rayStart,
                new Vector2(collider.bounds.extents.x, collider.bounds.extents.y * 0.5f),
                0, 
                rayDirection,
                collider.bounds.extents.y + groundCheckOffset,
                groundLayer);

            return currentGroundHit;
        }

        Direction GetDirectionOfHitObject(GameObject hitObject)
        {
            Vector2 colliderExtents = collider.bounds.extents;
            Vector2 colliderCenter = collider.bounds.center;
            Vector2 rayStart = colliderCenter;
            
            Vector2 rayDirectiontRight = Vector2.right;
            Vector2 rayDirectiontLeft = Vector2.left;

            float velocityMagnitude = velocity.magnitude * Time.deltaTime;
            
            Vector2 rayDirectionDown = Vector2.down;
            Vector2 rayDirectionUp = Vector2.up;
            
            
            RaycastHit2D hitUpWards =  Physics2D.Raycast(rayStart, rayDirectionUp, velocityMagnitude + colliderExtents.y, collisionLayer);
            RaycastHit2D hitDownWards =  Physics2D.Raycast(rayStart, rayDirectionDown, velocityMagnitude + colliderExtents.y, collisionLayer);
            RaycastHit2D hitRight =  Physics2D.Raycast(rayStart, rayDirectiontRight, velocityMagnitude + colliderExtents.x, collisionLayer);
            RaycastHit2D hitLeft =  Physics2D.Raycast(rayStart, rayDirectiontLeft, velocityMagnitude + colliderExtents.x, collisionLayer);
            
            if (hitRight && hitRight.collider.gameObject == hitObject)
            {
                Debug.Log("hit object right from player");
                return Direction.Right;
            }
            else if (hitLeft && hitLeft.collider.gameObject == hitObject)
            {
                Debug.Log("hit object left from player");
                return Direction.Left;
            }
            else if (hitUpWards && hitUpWards.collider.gameObject == hitObject)
            {
                Debug.Log("hit object on top of player");
                return Direction.Up;
            }
            else if (hitDownWards && hitDownWards.collider.gameObject == hitObject)
            {
                Debug.Log("hit object on bottom of player");
                return Direction.Down;
            }

            return Direction.None;
        }
        IEnumerator CoyoteTimer()
        {
            applyGravity = false;
          
            yield return new WaitForSeconds(coyoteTime);
          
            coyoteTimeRoutine = null;
            applyGravity = true;
        }

        public void StartDash(InputAction.CallbackContext context)
        {
            if (!context.started || isDashing)
            {
                return;
            }
                
            Debug.Log("Started dashing");
            isDashing = true;
            Debug.Log(currentMovementInput);
            dashRoutine = StartCoroutine(Dash(currentMovementInput));
        }

        void EndDash()
        {
            isDashing = false;
            velocity.y = 0;
            
            if (dashRoutine != null)
            {
                StopCoroutine(dashRoutine);
            }
        }
        IEnumerator Dash(Vector2 direction)
        {
            if (direction == Vector2.zero)
            {
                EndDash();
                yield break;
            }
            
            float alreadyDashedDistance = 0.0f;

            while (alreadyDashedDistance < dashDistance)
            {
                //get the position before dashing one frame
                Vector2 lastPosition = rb.position;
                //get the position after dashing one frame
                Vector2 targetPosition = rb.position + direction * (dashSpeed * Time.deltaTime);
                
                //now calculate the distance between these two and add it to dashed distance
                float dashedDistance = Vector2.Distance(targetPosition, lastPosition);
                
                alreadyDashedDistance += dashedDistance;
               
                //If we overshoot then move in the backwards direction with overshoot
                if (alreadyDashedDistance >= dashDistance)
                {
                    float overshoot = alreadyDashedDistance - dashDistance;
                    Debug.Log("overshot the dash distance by : " + overshoot);

                    Vector2 correctOvershoot = -direction * overshoot;
                    targetPosition += correctOvershoot;
                    
                    Debug.Log("moved the player in the opposite direction by :" + correctOvershoot);
                }
                
                rb.MovePosition(targetPosition);
                Debug.Log("Dashing");
                yield return new WaitForFixedUpdate();
            }
            
            EndDash();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag(groundTag))
            {
                if (GetDirectionOfHitObject(other.gameObject) == Direction.Down)
                {
                    //because if we just switch grounds we dont want to reset the velocity
                   
                    velocity.y = 0;
                    
                    
                    timeJumping = 0.0f;
                    timesJumped = 0;
                    applyGravity = false;
                    
                    if (coyoteTimeRoutine != null)
                    {
                        StopCoroutine(coyoteTimeRoutine);
                        coyoteTimeRoutine = null;
                    }
                }
            }
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (other.gameObject.CompareTag(groundTag))
            {
                if (GetDirectionOfHitObject(other.gameObject) == Direction.Down)
                {
                   
                    Debug.Log("Grounded");
                    if (coyoteTimeRoutine == null && !isJumping)
                    {
                        coyoteTimeRoutine = StartCoroutine(CoyoteTimer());
                    }
                    
                    if (coyoteTimeRoutine != null)
                    {
                        StopCoroutine(coyoteTimeRoutine);
                        coyoteTimeRoutine = null;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)velocity);

            if (!collider)
            {
                return;
            }
            
            Vector2 colliderExtents = collider.bounds.extents;
            Vector2 colliderCenter = collider.bounds.center;
            Vector2 rayStartLeft = colliderCenter - new Vector2(colliderExtents.x, 0);
            Vector2 rayStartRight = colliderCenter + new Vector2(colliderExtents.x, 0);
            Vector2 rayStartTop = colliderCenter + new Vector2(0, colliderExtents.y);
            Vector2 rayStartBottom = colliderCenter - new Vector2(0, colliderExtents.y);
            
            Gizmos.DrawSphere(rayStartLeft, 0.05f);
            Gizmos.DrawSphere(rayStartRight, 0.05f);
            Gizmos.DrawSphere(rayStartTop, 0.05f);
            Gizmos.DrawSphere(rayStartBottom, 0.05f);
        }
    }
}
