using UnityEngine;
#if ACS_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ACS
{
    [DefaultExecutionOrder(-100)]
    public class ACSInputController : MonoBehaviour
    {
        private static ACSInputController instance;
#if ACS_INPUT_SYSTEM
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string pointActionName = "Point";
        [SerializeField] private string clickActionName = "Click";

        private InputAction pointAction;
        private InputAction clickAction;
#endif

        public static Vector2 PointerPosition { get; private set; }

        private void Awake()
        {
            instance = this;

#if ACS_INPUT_SYSTEM
            if (inputActions == null)
            {
                Debug.LogError($"{nameof(ACSInputController)} is missing an InputActionAsset.", this);
                enabled = false;
                return;
            }

            pointAction = inputActions.FindAction(pointActionName, false);
            clickAction = inputActions.FindAction(clickActionName, false);

            if (pointAction == null || clickAction == null)
            {
                Debug.LogError($"Input actions '{pointActionName}' and/or '{clickActionName}' were not found in {inputActions.name}.", this);
                enabled = false;
                return;
            }

            pointAction.Enable();
            clickAction.Enable();
            PointerPosition = pointAction.ReadValue<Vector2>();
#elif ACS_INPUT_LEGACY
            PointerPosition = Input.mousePosition;
#endif
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
#if ACS_INPUT_SYSTEM
            if (pointAction != null)
            {
                PointerPosition = pointAction.ReadValue<Vector2>();
            }
#elif ACS_INPUT_LEGACY
            PointerPosition = Input.mousePosition;
#endif
        }

        public static bool IsClickPressedThisFrame()
        {
#if ACS_INPUT_SYSTEM
            return instance != null && instance.clickAction != null && instance.clickAction.WasPressedThisFrame();
#elif ACS_INPUT_LEGACY
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }
    }
}


