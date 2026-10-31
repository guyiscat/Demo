using UnityEngine;

namespace Seaside
{
    /// <summary>Fits the visible camera rectangle inside an axis-aligned background.</summary>
    public static class BackgroundCameraFrame
    {
        public static void Fit(Bounds bounds, Vector3 desiredPosition, float desiredSize,
            float displayAspect, Rect originalViewport, out Vector3 position,
            out float size, out Rect viewport)
        {
            displayAspect = Mathf.Max(.001f, displayAspect);
            float height = Mathf.Min(Mathf.Max(.002f, desiredSize * 2), bounds.size.y);
            float width = Mathf.Min(Mathf.Max(.002f, desiredSize * 2 * displayAspect), bounds.size.x);
            float viewAspect = width / height;
            viewport = originalViewport;
            if (viewAspect < displayAspect)
            {
                viewport.width *= viewAspect / displayAspect;
                viewport.x += (originalViewport.width - viewport.width) * .5f;
            }
            else
            {
                viewport.height *= displayAspect / viewAspect;
                viewport.y += (originalViewport.height - viewport.height) * .5f;
            }
            size = height * .5f;
            position = desiredPosition;
            position.x = bounds.size.x - width < .0001f ? bounds.center.x :
                Mathf.Clamp(position.x, bounds.min.x + width * .5f, bounds.max.x - width * .5f);
            position.y = bounds.size.y - height < .0001f ? bounds.center.y :
                Mathf.Clamp(position.y, bounds.min.y + height * .5f, bounds.max.y - height * .5f);
        }
    }
}
