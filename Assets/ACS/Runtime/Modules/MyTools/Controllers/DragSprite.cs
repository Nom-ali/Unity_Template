using MyBox;
using UnityEngine;

public class DragSprite : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private float yOffset = 1.5f;
    [SerializeField] private bool _isDragging = false;

    [ReadOnly, SerializeField] private bool DragThisObject = false;
    void Start()
    {
        // Get the main camera and sprite renderer component
        _camera = Camera.main;
    }

    public void StartDragging(bool isDragging)
    {
        DragThisObject = isDragging;
    }

    void Update()
    {
                                   
            // If the mouse button is pressed, start dragging
            if (Input.GetMouseButtonDown(0)) // Left mouse button
            {
                Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -_camera.transform.position.z));
                mouseWorldPosition.z = 0; // Ensure z position is 0 for 2D
                mouseWorldPosition.y += 1.5f; // Ensure z position is 0 for 2D

                // Apply offset to the sprite position
                transform.position = mouseWorldPosition;
                _isDragging = true;

            }

            // While dragging, update the sprite's position
            if (_isDragging)
            {
                // Convert the mouse position to world space, again with the fixed Z position

                Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -_camera.transform.position.z));
                mouseWorldPosition.z = 0; // Ensure z position is 0 for 2D
                mouseWorldPosition.y += yOffset; // Ensure z position is 0 for 2D

                // Apply offset to the sprite position
                transform.position = mouseWorldPosition;
            }

            // When the mouse button is released, stop dragging
       
        else if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }
    }
}




