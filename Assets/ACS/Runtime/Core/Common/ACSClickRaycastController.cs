using System;
using UnityEngine;

namespace ACS
{
    [DefaultExecutionOrder(-100)]
    public class ACSClickRaycastController : MonoBehaviour
    {
        [SerializeField] private bool isActive = true;
        [SerializeField] private LayerMask raycastMask = ~0;

        public event Action<RaycastHit> OnRaycastHit3D;
        public event Action<Vector2> OnRaycastHit2D;

        public void EnableInput()
        {
            isActive = true;
        }

        public void DisableInput()
        {
            isActive = false;
        }

        private void Update()
        {
            if (!isActive || !ACSInputController.IsClickPressedThisFrame())
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Ray ray = camera.ScreenPointToRay(ACSInputController.PointerPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, raycastMask))
            {
                OnRaycastHit3D?.Invoke(hit);
                return;
            }

            Vector2 point = ray.origin;
            if (Physics2D.OverlapPoint(point, raycastMask) != null)
            {
                OnRaycastHit2D?.Invoke(point);
            }
        }
    }
}
