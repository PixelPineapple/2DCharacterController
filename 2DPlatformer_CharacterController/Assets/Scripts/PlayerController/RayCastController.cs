using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D))]
public class RayCastController : MonoBehaviour {

    public LayerMask collisionMask;
    const float dstBetweenRays = .25f; // to automate the number of rays needed to cast based on object width and height

    [HideInInspector]
    public float horizontalRaySpacing; // spacing between the horizontal ray
    [HideInInspector]
    public float verticalRaySpacing; // spacing between the vertical ray
    [HideInInspector]
    public int horizontalRayCount; // how many ray we fire horizontally
    [HideInInspector]
    public int verticalRayCount; // how many ray we fire vertically

    [HideInInspector]
    public BoxCollider2D boxCollider;
    public RayCastOrigins rayCastOrigins;

    public const float skinWidth = .015f; // cast the ray not from the outer side of the collider, but a little bit inside

    public virtual void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    public virtual void Start()
    {
        CalculateRaySpacing();
    }

    /// <summary>
    /// Make a Bounding Box
    /// </summary>
    public void UpdateRayCastOrigins()
    {
        Bounds bounds = boxCollider.bounds; // get the collider bounds
        bounds.Expand(skinWidth * -2); // casting the ray from slightly inside the box (multiple by - )
        rayCastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        rayCastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        rayCastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        rayCastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    public  void CalculateRaySpacing()
    {
        Bounds bounds = boxCollider.bounds;
        bounds.Expand(skinWidth * -2); // casting the ray from slightly inside the box (multiple by - )

        float boundsWidth = bounds.size.x;
        float boundsHeight = bounds.size.y;

        horizontalRayCount = Mathf.RoundToInt (boundsHeight / dstBetweenRays);
        verticalRayCount = Mathf.RoundToInt(boundsWidth / dstBetweenRays);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    public struct RayCastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }

}
