using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RayCastController {

    public LayerMask passengerMask;

    #region Platform Waypoints
    // Shows where the platform is going to move
    public Vector3[] localWaypoints; // Follows the object around (used in inspector)
    Vector3[] globalWaypoints; // Stand still to show the object movement (used in game mode)
    #endregion

    public float speed;
    public bool cyclic; // Whether the platform moves 1 -> 2 -> 3 -> 1 -> 2.. or moves 1 -> 2 -> 3 -> 2 -> 1..
    public float waitTime; // Wait time occurs between when we reaches each waypoints before moving to the next
    [Range(0, 2)]
    public float easeAmount; // easeAmount = 0 means no easing

    int fromWaypointIndex;
    float percentBetweenWaypoints; // Percentage between zero and one
    float nextMoveTime; // Time.time must be bigger than this in order for the platform to move to the next waypoint

    #region Passenger Movements
    List<PassengerMovement> passengerMovements;
    // Dictionary is used so that we do not call GetComponent<CharacterController2D_SL> every frame
    Dictionary<Transform, CharacterController2D_SL> passengerDictionary = new Dictionary<Transform, CharacterController2D_SL>();
    #endregion 

    // Use this for initialization
    public override void Start ()
    {
        base.Start();

        // Instantiate and copy localWayPoints content into the globalWayPoints
        globalWaypoints = new Vector3[localWaypoints.Length];
        for (int i = 0; i < localWaypoints.Length; i++)
        {
            globalWaypoints[i] = localWaypoints[i] + transform.position;
        }
	}

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update ()
    {
        UpdateRayCastOrigins();

        Vector3 velocity = CalculatePlatformMovement();

        CalculatePassengerMovement(velocity);

        MovePassengers(true);
        transform.Translate(velocity);
        MovePassengers(false);
	}

    float Ease (float x)
    {
        float a = easeAmount + 1; // +1 is because we got no easing if a = 1, but easeAmount started at 0
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    Vector3 CalculatePlatformMovement ()
    {
        // Make the platform stop after reaching the waypoint and wait before moving again to the next
        if (Time.time < nextMoveTime)
        {
            return Vector3.zero;
        }

        // % Make the waypoints reset after reaches the globalwaypoints.length
        fromWaypointIndex %= globalWaypoints.Length;
        int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]);
        percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints;
        // Clamping our waypoint
        percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);
        float easedPercentBetweenWaypoints = Ease(percentBetweenWaypoints);

        // Lerping between the two global waypoints (Using easedPercentage for easing)
        Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], easedPercentBetweenWaypoints);

        if (percentBetweenWaypoints >= 1) // Reach the next waypoint
        {
            percentBetweenWaypoints = 0;
            fromWaypointIndex++;

            if (!cyclic) // platform moves between waypoints 1 -> 2 -> 3 -> 2 -> 1..
            {
                if (fromWaypointIndex >= globalWaypoints.Length - 1)
                {
                    fromWaypointIndex = 0;
                    System.Array.Reverse(globalWaypoints);
                }
            }

            nextMoveTime = Time.time + waitTime;
        }

        return newPos - transform.position;
    }

    /// <summary>
    /// Moving the pasengers along with the velocity of the platform
    /// </summary>
    /// <param name="beforeMovePlatform">do we move the player before or after we move the platform</param>
    void MovePassengers(bool beforeMovePlatform)
    {
        foreach (PassengerMovement passenger in passengerMovements)
        {
            // Only call this once to register passenger into the dictionary
            if (!passengerDictionary.ContainsKey(passenger.transform))
                passengerDictionary.Add(passenger.transform, passenger.transform.GetComponent<CharacterController2D_SL>());
            
            if (passenger.moveBeforePlatform == beforeMovePlatform)
            {
                passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);
            }
        }
    }

    /// <summary>
    /// "Passengers" are refering to any controller2D (anything) that is affected by the platform 
    /// </summary>
    /// <param name="velocity"></param>
    void CalculatePassengerMovement(Vector3 velocity)
    {
        HashSet<Transform> movedPassengers = new HashSet<Transform>();
        passengerMovements = new List<PassengerMovement>(); // instantiate passenger movement's list
        
        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        // Vertically moving platform
        if (velocity.y != 0)
        {
            float rayLength = Mathf.Abs(velocity.y) + skinWidth;
            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = (directionY == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.topLeft;
                rayOrigin += Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = (directionY == 1) ? velocity.x : 0;
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;

                        passengerMovements.Add(new PassengerMovement(
                            hit.transform,
                            new Vector3(pushX, pushY),
                            directionY == 1,
                            true));
                    }
                }
            }
        }

        // Horizontally Moving Platform (passengers are pushed from the side by the platform)
        if (velocity.x != 0)
        {
            float rayLength = Mathf.Abs(velocity.x) + skinWidth; // Rayの長さ
            for (int i = 0; i < horizontalRayCount; i++) // 水平なRayCountにループ
            {
                // Where do we want to start our ray
                Vector2 rayOrigin = (directionX == -1) ? rayCastOrigins.bottomLeft : rayCastOrigins.bottomRight;
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        float pushY = -skinWidth;

                        passengerMovements.Add(new PassengerMovement(
                            hit.transform,
                            new Vector3(pushX, pushY),
                            false,
                            true));
                    }
                }
            }
        }

        // Passenger on top of a horizontally or downward moving platform
        if (velocity.y == 0 && velocity.x != 0 || directionY == -1)
        {
            float rayLength = skinWidth * 2;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = rayCastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x;
                        float pushY = velocity.y;

                        passengerMovements.Add(new PassengerMovement(
                            hit.transform,
                            new Vector3(pushX, pushY),
                            true,
                            false));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Struct for storing all the passenger's information
    /// 移動プラットフォームに乗っている旅客情報を保存するためのストラクチャー
    /// </summary>
    struct PassengerMovement
    {
        public Transform transform;
        public Vector3 velocity;
        public bool standingOnPlatform;
        public bool moveBeforePlatform; // set it to false if the platform are moving downward. (so we move the platform first)

        public PassengerMovement (Transform _transform, Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform)
        {
            transform = _transform;
            velocity = _velocity;
            standingOnPlatform = _standingOnPlatform;
            moveBeforePlatform = _moveBeforePlatform;
        }
    }


    private void OnDrawGizmos()
    {
        if (localWaypoints != null)
        {
            Gizmos.color = Color.red;
            float size = .3f;

            for (int i = 0; i < localWaypoints.Length; i++)
            {
                Vector3 globalWaypointPos = (Application.isPlaying) ? globalWaypoints[i] : localWaypoints[i] + transform.position;
                Gizmos.DrawLine(globalWaypointPos - Vector3.up * size, globalWaypointPos + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPos - Vector3.left * size, globalWaypointPos + Vector3.left * size);
            }
        }
    }
}
