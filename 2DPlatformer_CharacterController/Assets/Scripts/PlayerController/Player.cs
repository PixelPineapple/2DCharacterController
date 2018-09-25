using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (CharacterController2D_SL))]
public class Player : MonoBehaviour {

    #region Jumping
    public float maxJumpHeight = 4f; // How far we want our character to jump (in units)
    public float minJumpHeight = 1f; // Minimum jump height
    public float timeToJumpApex = .4f; // How fast we want our character to reach the maximum height
    #endregion

    #region Moving Left and Right
    float accelerationTimeAirborne = .1f; // manuever while jumping
    float accelerationTimeGrounded = .05f; // manuever while grounded
    float moveSpeed = 6f; // player move speed
    #endregion

    #region wall jumps
    public Vector2 wallJumpClimb; // Hopping above on the same wall
    public Vector2 wallJumpOff; // Without providing any velocity X input, jumping off the wall
    public Vector2 wallLeap; // Jump to another wall on the opposite side
    public float wallSlideSpeedMax = 3;
    public float wallStickTime = .25f;
    float timeToWallUnstick;
    #endregion

    #region Animation
    Animator animatorController;
    #endregion

    float gravity; // Gravity 重力
    float maxJumpVelocity; // player maximum jump force
    float minJumpVelocity; // if player release the jump button early, this value will be used instead
    Vector3 velocity;
    float velocityXSmoothing;

    CharacterController2D_SL controller;

    Vector2 directionalInput;
    bool wallSliding;
    int wallDirX;

	// Use this for initialization
	void Start ()
    {
        controller = GetComponent<CharacterController2D_SL>();

        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);

        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        print("Gravity " + gravity + " Jump velocity " + maxJumpVelocity);

        animatorController = GetComponent<Animator>();
	}

    // Update is called once per frame
    void Update()
    {
        CalculateVelocity();
        HandleWallSliding();

        //// Trying to make the player holding the button to keep sliding ( still fail ) 
        //if ((!controller.collisionInfos.right || !controller.collisionInfos.left) && wallSliding)
        //{
        //    wallSliding = false;
        //    controller.collisionInfos.left = false;
        //    controller.collisionInfos.right = false;
        //}
        //print("Left : " + controller.collisionInfos.left + " Right : " + controller.collisionInfos.right);

        controller.Move(velocity * Time.deltaTime, directionalInput);

        if (controller.collisionInfos.above || controller.collisionInfos.below)
        {
            if (controller.collisionInfos.slidingDownMaxSlope)
            {
                // different slope angle will made the character goes faster or slower when slides down.
                velocity.y += controller.collisionInfos.slopeNormal.y *  -gravity * Time.deltaTime;
            }
            else
            {
                velocity.y = 0; // stopping the player from accumulating gravity
            }
        }

        AnimatePlayer();
    }


    /// <summary>
    /// Give the player a directional input
    /// </summary>
    /// <param name="input">The input axes</param>
    public void SetDirectionalInput (Vector2 input)
    {
        directionalInput = input;
    }
    
    /// <summary>
    /// Jumping is on the ground, wall climbing if near a wall
    /// </summary>
    public void OnJumpInputDown()
    {
        //Jumping on the wall
        if (wallSliding)
        {
            // perform wall hop
            if (wallDirX == directionalInput.x)
            {
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
            }
            // without providing any input. just dropdown from the wall
            else if (directionalInput.x == 0)
            {
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
            }
            // perform wall leap to the opposite of the wall
            else
            {
                velocity.x = -wallDirX * wallLeap.x;
                velocity.y = wallLeap.y;
            }
        }

        // Jumping on the ground
        if (controller.collisionInfos.below)
        {
            if (controller.collisionInfos.slidingDownMaxSlope)
            {
                if (directionalInput.x != -Mathf.Sign(controller.collisionInfos.slopeNormal.x))
                {
                    velocity.y = maxJumpVelocity * controller.collisionInfos.slopeNormal.y;
                    velocity.x = maxJumpVelocity * controller.collisionInfos.slopeNormal.x; // this value needs to be reworked for great distance leaping
                }
            }
            else
            {
                velocity.y = maxJumpVelocity;
            }
        }
    }

    /// <summary>
    /// Stopping the jump when jump button is released (before reaching maximum height)
    /// </summary>
    public void OnJumpInputUp()
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }
    
    /// <summary>
    /// Wall Sliding
    /// </summary>
    void HandleWallSliding()
    {
        // Wall direction (-1 means wall is in the left, 1 means wall is in the right)
        wallDirX = (controller.collisionInfos.left) ? -1 : 1;
        wallSliding = false;
        // Player detects a collision on the right or on the left, NOT in the ground, and currently falling
        if ((controller.collisionInfos.right || controller.collisionInfos.left) && !controller.collisionInfos.below && velocity.y < 0)
        {
            // begin wall sliding
            wallSliding = true;
            if (velocity.y < -wallSlideSpeedMax) // Reducing the falling speed while sliding
            {
                velocity.y = -wallSlideSpeedMax;
            }

            // providing player a short time frame to perform wall leap
            if (timeToWallUnstick > 0)
            {
                velocity.x = 0; // This code is used when we want to remain stick on the wall
                velocityXSmoothing = 0;
                if (directionalInput.x != wallDirX && directionalInput.x != 0)
                {
                    timeToWallUnstick -= Time.deltaTime;
                }
                else
                {
                    timeToWallUnstick = wallStickTime;
                }
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }
        }
    }

    /// <summary>
    /// Calculate player velocity
    /// プレイヤーの速度を計算
    /// </summary>
    void CalculateVelocity()
    {
        // Moved from below to this position in order for the wall slide stick to work
        // add smoothing to x-movement so changing direction is not so abrupt
        float targetVelocityX = directionalInput.x * moveSpeed;
        // Mathf.SmoothDamp = Gradually changes a value towards a desired goal over time
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisionInfos.below) ? accelerationTimeGrounded : accelerationTimeAirborne); // check whether we are on the ground or on the air
        velocity.y += gravity * Time.deltaTime; // Applying gravity
    }

    void AnimatePlayer()
    {
        // moving the player
        animatorController.SetFloat("velocityX", Mathf.Abs(directionalInput.x));

        if (controller.collisionInfos.faceDir == -1)
        {
            GetComponent<Transform>().localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            GetComponent<Transform>().localScale = new Vector3(1, 1, 1);
        }
    }
}
