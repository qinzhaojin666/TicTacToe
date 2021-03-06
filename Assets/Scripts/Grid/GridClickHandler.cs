using UnityEngine;
using UnityEngine.EventSystems;

public class GridClickHandler : MonoBehaviour {

    public bool isMovementEnabled = true;
    public bool isZoomEnabled = true;

    protected const float zoomSpeed = 0.03f;
    protected const float minOrthSize = 4f;
    protected const float maxOrthSize = 15f;
    
    protected TTTGameLogic gameLogic;

    protected float fingerMoveMin; // How much the finger needs to move in pixels in order for the camera to be moved
    protected bool zooming = false;
    protected bool currentTouchOverUI = false;

    public virtual void Start() {
        gameLogic = FindObjectOfType<TTTGameLogic>();

        fingerMoveMin = Camera.main.pixelHeight * 0.01f;
    }

    Vector2 moveAmount;
    Vector3 fingerPrevPos;

    public virtual void Update() {
        // Ended zooming
        if (Input.touchCount == 0 && zooming) zooming = false;

        if (Input.touchCount == 2 && isZoomEnabled) { // Zooming
            zooming = true;

            // Store both touches.
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Find the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Find the magnitude of the vector (the distance) between the touches in each frame.
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Find the difference in the distances between each frame.
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            // Change the orthographic size based on the change in distance between the touches.
            Camera.main.orthographicSize += deltaMagnitudeDiff * zoomSpeed;

            // Make sure the orthographic size stays between the given numbers
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minOrthSize, maxOrthSize);
        } else if (Input.touchCount == 1 && !zooming) {
            Touch touch = new Touch();
            touch = Input.GetTouch(0);

            if (!currentTouchOverUI && IsPointerOverUIObject()) currentTouchOverUI = true;
            
            if (touch.phase == TouchPhase.Began) {
                moveAmount.x = 0; moveAmount.y = 0;
                fingerPrevPos = Camera.main.ScreenToViewportPoint(touch.position);

                // Move grid
            } else if (touch.phase == TouchPhase.Moved && Input.touchCount == 1) {
                moveAmount += new Vector2(Mathf.Abs(touch.deltaPosition.x), Mathf.Abs(touch.deltaPosition.y));

                if ((moveAmount.x > fingerMoveMin || moveAmount.y > fingerMoveMin) && isMovementEnabled && !currentTouchOverUI) {
                    // Set finger pos in viewport coords
                    Vector3 fingerPos = Camera.main.ScreenToViewportPoint(touch.position);
                    Vector3 fingerDelta = Camera.main.ViewportToWorldPoint(fingerPos) - Camera.main.ViewportToWorldPoint(fingerPrevPos);

                    Camera.main.transform.position -= fingerDelta;

                    // Set finger's prev pos in viewport coords
                    fingerPrevPos = Camera.main.ScreenToViewportPoint(touch.position);
                }
            } else if (Input.touchCount == 1 && touch.phase == TouchPhase.Ended) {
                // Not moved finger
                if (moveAmount.x <= fingerMoveMin && moveAmount.y <= fingerMoveMin && !currentTouchOverUI) {
                    Vector2 clickPos = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);

                    ClickedAt(clickPos);
                }

                currentTouchOverUI = false;
            }
        }

#if UNITY_STANDALONE || UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) {
            Vector2 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            ClickedAt(clickPos);
        }
#endif
    }

    public virtual void ClickedAt(Vector2 clickPos) {
        gameLogic.WantToPlaceAt(clickPos);
    }

    public static bool IsPointerOverUIObject() {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount == 0) return false;

        return EventSystem.current.IsPointerOverGameObject(0);
#endif

#if UNITY_EDITOR || UNITY_STANDALONE
        if (!Input.GetMouseButton(0)) return false;

        return EventSystem.current.IsPointerOverGameObject();
#endif
    }
}
