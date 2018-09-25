using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController2D_SL : RayCastController {

    public float maxSlopeAngle = 10f; // maximum slope angle our player can climb
    
    public CollisionInfo collisionInfos;

    [HideInInspector]
    public Vector2 playerInput;

    public override void Start()
    {
        base.Start();
        collisionInfos.faceDir = 1;
    }

    /// <summary>
    /// Move the Game Object controlled by this script (Used by uncontrollable object)
    /// </summary>
    /// <param name="moveAmount"></param>
    /// <param name="standingOnPlatform"></param>
    public void Move(Vector2 moveAmount, bool standingOnPlatform)
    {
        Move(moveAmount, Vector2.zero, standingOnPlatform);
    }

    /// <summary>
    /// Move the Game Object controlled by this script
    /// </summary>
    /// <param name="moveAmount"></param>
    /// <param name="standingOnPlatform"></param>
    public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
    {
        UpdateRayCastOrigins();
        collisionInfos.Reset(); // reset all collision infos
        collisionInfos.moveAmountOld = moveAmount;
        playerInput = input;

        if (moveAmount.y < 0)
        {
            DescendSlope(ref moveAmount);
        }

        if (moveAmount.x != 0)
        {
            collisionInfos.faceDir = (int)Mathf.Sign(moveAmount.x);
        }

        HorizontalCollisions(ref moveAmount);

        if (moveAmount.y != 0) // Checking for vertical collision (y-axes)
        VerticalCollisions(ref moveAmount);

        transform.Translate(moveAmount); // Begin moving the character

        if (standingOnPlatform)
        {
            collisionInfos.below = true;
        }
    }

    /// <summary>
    /// Horizontal Collision
    /// </summary>
    /// <param name="moveAmount">moveAmount of the object</param>
    void HorizontalCollisions(ref Vector2 moveAmount)
    {
        float directionX = collisionInfos.faceDir; // 現在、どこに向いている
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth; // Rayの長さ

        if (Mathf.Abs(moveAmount.x) < skinWidth)
        {
            rayLength = 2 * skinWidth; // 1 skin width is to cast the ray to the edge of the collider, the other is cast the ray outside small enough to detect a wall;
        }

        for (int i = 0; i < horizontalRayCount; i++) // 水平なRayCountにループ
        {
            // Where do we want to start our ray
            Vector2 rayOrigin = (directionX == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            // Debugging the raycast
            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);

            if (hit) // if our collision hits
            {
                if (hit.distance == 0) continue; // addresing bug, where this object is colliding with the platform coming from above

                // Get the slope angle
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                
                if (i == 0 && slopeAngle <= maxSlopeAngle) // climbing the slope
                {
                    // Addressing Bug (descending and quickly ascending speed slowdown)
                    if (collisionInfos.descendingSlope)
                    {
                        collisionInfos.descendingSlope = false;
                        moveAmount = collisionInfos.moveAmountOld;
                    }
                    // Addressing Bug (climbing slope before the actual gameobject touches the slope)
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisionInfos.slopeAngleOld) // climbing a new slope
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
                    moveAmount.x += distanceToSlopeStart * directionX;
                }

                if (!collisionInfos.climbingSlope || slopeAngle > maxSlopeAngle)
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisionInfos.climbingSlope) // Addressing Bug (if there is an obstacle on the slope (x-axes), the character will begin to jitter)
                    {
                        moveAmount.y = Mathf.Tan(collisionInfos.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                    }
                    
                    collisionInfos.left = directionX == -1; // if we are moving left and collide. collisionInfos.left = true;
                    collisionInfos.right = directionX == 1; // if we are moving right and collide. collisionInfos.right = true;
                }
            }
        }
    }

    /// <summary>
    /// Vertical Collision
    /// </summary>
    /// <param name="moveAmount">moveAmount of the object</param>
    void VerticalCollisions(ref Vector2 moveAmount)
    {
        float directionY = Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            // Debugging the raycast
            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);

            if (hit) // if our collision hits
            {
                if (hit.collider.tag == "Through") // Jumping through a platform from below
                {
                    if (directionY == 1 || hit.distance == 0) // If we are jumping and the distance is 0
                    {
                        continue;
                    }
                    // Time frame for player to jump through the platform (for vertically moving platform going downward)
                    if (collisionInfos.fallingThroughPlatform) continue;
                    // Player jumps down from platform
                    if (playerInput.y == -1)
                    {
                        collisionInfos.fallingThroughPlatform = true;
                        Invoke("ResetFallingThroughPlatform", .25f);
                        continue;
                    }
                }

                moveAmount.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                if (collisionInfos.climbingSlope)
                {
                    moveAmount.x = (moveAmount.y / Mathf.Tan(collisionInfos.slopeAngle * Mathf.Deg2Rad)) * Mathf.Sign(moveAmount.x);
                }

                collisionInfos.below = directionY == -1; // if we are moving downward and collide, collisionInfos.below = true;
                collisionInfos.above = directionY == 1; // if we are moving upward and collide, collisionInfos.above = true;
            }
        }

        if (collisionInfos.climbingSlope) // Addressing Bug (moving on a curved slope (a new slope on a slope) makes the character jitter once)
        {
            float directionX = Mathf.Sign(moveAmount.x);
            rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.bottomRight) + Vector2.up * moveAmount.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisionInfos.slopeAngle)
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    collisionInfos.slopeAngle = slopeAngle;
                }
            }
        }
    }

    /// <summary>
    /// Climbing Slopes
    /// </summary>
    /// <param name="moveAmount">moveAmount of the object</param>
    /// <param name="slopeAngle">Angle of the slope we want to climb</param>
    void ClimbSlope (ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
    {
        float moveDistance = Mathf.Abs(moveAmount.x);
        float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        if (moveAmount.y <= climbmoveAmountY)
        { 
            moveAmount.y = climbmoveAmountY;
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
            collisionInfos.below = true;
            collisionInfos.climbingSlope = true;
            collisionInfos.slopeAngle = slopeAngle;
            collisionInfos.slopeNormal = slopeNormal;
        }
    }

    /// <summary>
    /// Descending Slopes
    /// </summary>
    /// <param name="moveAmount">moveAmount of the object</param>
    void DescendSlope (ref Vector2 moveAmount)
    {
        // Descending into unclimbable slope (player should slide down)
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(rayCastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(rayCastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        if (maxSlopeHitLeft ^ maxSlopeHitRight) // ^ exclusive or operator : means if one of them is true;
        {
            SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
            SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
        }

        if (!collisionInfos.slidingDownMaxSlope)
        {
            float directionX = Mathf.Sign(moveAmount.x);
            Vector2 rayOrigin = (directionX == -1) ? rayCastOrigins.bottomRight : rayCastOrigins.bottomLeft;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle) // not a flat surface or un-descendable slope
                {
                    if (Mathf.Sign(hit.normal.x) == directionX) // check if we are moving along with the slope
                    {
                        if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x)) // descend when we are close enough with the slope
                        {
                            float moveDistance = Mathf.Abs(moveAmount.x);
                            float descendmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                            moveAmount.y -= descendmoveAmountY;

                            collisionInfos.slopeAngle = slopeAngle;
                            collisionInfos.descendingSlope = true;
                            collisionInfos.below = true;
                            collisionInfos.slopeNormal = hit.normal;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Slide down from max slope angle
    /// </summary>
    /// <param name="hit"></param>
    /// <param name="moveAmount">the amount of movement player makes (velocity * deltatime)</param>
    void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
    {
        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle > maxSlopeAngle)
            {
                moveAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);
                collisionInfos.slopeAngle = slopeAngle;
                collisionInfos.slidingDownMaxSlope = true;
                collisionInfos.slopeNormal = hit.normal;
            }
        }
    }


    public void ResetFallingThroughPlatform()
    {
        collisionInfos.fallingThroughPlatform = false;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope, descendingSlope;
        public bool slidingDownMaxSlope;

        public float slopeAngle, slopeAngleOld;
        public Vector2 slopeNormal;
        public Vector2 moveAmountOld;
        public int faceDir;
        public bool fallingThroughPlatform;

        public void Reset()
        {
            above = below = false;
            right = left = false;
            climbingSlope = false;
            descendingSlope = false;
            slidingDownMaxSlope = false;
            slopeNormal = Vector2.zero;
            
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
}
