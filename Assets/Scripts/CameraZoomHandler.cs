using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CameraZoomHandler : MonoBehaviour
{
    private Transform bookTransform;

    private bool isZooming = false;
    private float initialDistance;
    private Vector3 originalScale;
    private Vector3 defaultScale;

    public float MaxScale;
    public float MinScale;
    public Slider ZoomSlider;
    public Button DefaultZoomButton;//enable it in editor hierarchy
    public TextMeshProUGUI ZoomMessage;
    public bool ForceStopCameraAutoFocus = false;
    
    private void Awake()
    {
        //var camera = GetComponent<ARCameraManager>();
        var camera = Camera.main;
        bookTransform = transform;
        originalScale = bookTransform.localScale;
        defaultScale = bookTransform.localScale;
        ZoomSlider.value = defaultScale.x;
        
        // if(ForceStopCameraAutoFocus)
        //     camera.autoFocusRequested = false;
    }

    private void OnDisable() => ForceStopCameraAutoFocus = false;


    void Update()
    {
        if (InteractiveManagment.CurrentState == InteractionState.TurningPage) return;

        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (!isZooming)
            {
                initialDistance = Vector2.Distance(t0.position, t1.position);
                originalScale = bookTransform.localScale;
                isZooming = true;
            }
            else
            {
                float currentDistance = Vector2.Distance(t0.position, t1.position);
                float scaleFactor = currentDistance / initialDistance;
                Vector3 targetScale = originalScale * scaleFactor;

                Vector3 minVector = defaultScale * MinScale;
                Vector3 maxVector = defaultScale * MaxScale;

                targetScale = ClampVector3(targetScale, minVector, maxVector);
                bookTransform.localScale = targetScale;

                ZoomSlider.value = bookTransform.localScale.x;
                ZoomMessage.text = $"{GetIntFromFloatNum(bookTransform.localScale.x)} X";
            }
            InteractiveManagment.SetInteractionState(InteractionState.OnZoom);
        }
        else
        {
            isZooming = false;
            if (Mathf.Abs(bookTransform.localScale.x - defaultScale.x) < 0.05f)
                InteractiveManagment.SetInteractionState(InteractionState.Ready);
        }        
    }

    Vector3 ClampVector3(Vector3 value, Vector3 min, Vector3 max)
    {
        return new Vector3(
            Mathf.Clamp(value.x, min.x, max.x),
            Mathf.Clamp(value.y, min.y, max.y),
            Mathf.Clamp(value.z, min.z, max.z)
        );
    }

    public void OnDefaultZoomButtonClicked() // I've set it on defaultButton , make Button activc to use this function
    {
        ZoomSlider.value = defaultScale.x;
        bookTransform.localScale = defaultScale;
        originalScale = defaultScale;
        ZoomMessage.text = $"{GetIntFromFloatNum(bookTransform.localScale.x)} X";
        InteractiveManagment.SetInteractionState(InteractionState.Ready);
    }

    string GetIntFromFloatNum(float num)
    {
        return num.ToString("0.0");
    }

    public void OnSliderValueChage() // I've set it on slider handler , make slider intractable to use this function
    {
        bookTransform.localScale = new Vector3(ZoomSlider.value, ZoomSlider.value, ZoomSlider.value);
        ZoomMessage.text = $"{GetIntFromFloatNum(ZoomSlider.value)} X";
        if (Mathf.Abs(bookTransform.localScale.x - defaultScale.x) < 0.05f)
            InteractiveManagment.SetInteractionState(InteractionState.Ready);
        else
            InteractiveManagment.SetInteractionState(InteractionState.OnZoom);
    }
}
