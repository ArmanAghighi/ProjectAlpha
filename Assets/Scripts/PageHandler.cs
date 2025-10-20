using TMPro;
using UnityEngine;
using echo17.EndlessBook;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

public class PageHandler : MonoBehaviour
{
    const float transitionDuration = 0.5f;

    public Camera SceneCamera;
    public EndlessBook Book;
    public float TurnStopSpeed = 1.5f;
    public bool ReversePageIfNotMidway = true;
    public TextMeshProUGUI TextMessage;

    public static bool IsTouching = false;
    protected BoxCollider boxCollider;
    private BookInit bookinit;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        bookinit = Book.gameObject.GetComponent<BookInit>();
        if (SceneCamera == null)
            SceneCamera = Camera.main;
        Book.stateChanged += OnBookStateChanged;
    }

    void Update()
    {
        if (Input.touchCount == 1)
        {
            HandleTouch(Input.GetTouch(0));
        }
        // ادیتور یا دسکتاپ: موس
        else if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            HandleMouse();
        }
    }

    void UIDeactivator()
    {
        if (Book.GetPageData(Book.CurrentLeftPageNumber).hasUI || Book.GetPageData(Book.CurrentRightPageNumber).hasUI)
        {
            UIGenerator.Instance.ClearUI();
            Debug.Log("UI cleare start");
        }
    }
bool IsPointerOverUI()
{
    PointerEventData eventData = new PointerEventData(EventSystem.current);
    eventData.position = Input.mousePosition;

    List<RaycastResult> results = new List<RaycastResult>();
    EventSystem.current.RaycastAll(eventData, results);

    foreach (var r in results)
    {
        if (r.gameObject.CompareTag("UI")) // هر چیزی که نباید ورق بزنه
            return true;
    }

    return false;
}
    void HandleTouch(Touch touch)
    {
        if (InteractiveManagment.CurrentState == InteractionState.OnZoom)
        {
            StartCoroutine(ShowMessage("Restore to default zoom value to turn page", 2));
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        switch (touch.phase)
        {
            case TouchPhase.Began: HandleTouchDown(touch.position); break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary: HandleTouchDrag(touch.position); break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled: HandleTouchUp(); break;
        }
    }
    void HandleMouse()
    {
        if (InteractiveManagment.CurrentState == InteractionState.OnZoom)
        {
            StartCoroutine(ShowMessage("Restore to default zoom value to turn page", 2));
            return;
        }

        Vector2 mousePos = Input.mousePosition;

        // 🚀 اگر روی دکمه‌ی UI کلیک شد، ورق زدن بی‌خیال
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0)) HandleTouchDown(mousePos);
        if (Input.GetMouseButton(0)) HandleTouchDrag(mousePos);
        if (Input.GetMouseButtonUp(0)) HandleTouchUp();
    }

    void HandleTouchDown(Vector2 screenPos)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject() || IsPointerOverUI())
            return;
        if (Book.IsTurningPages || Book.IsDraggingPage) return;
        
        Ray ray = SceneCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        float normalizedTime = GetNormalizedTime(screenPos);
        var direction = normalizedTime > 0.5f ? Page.TurnDirectionEnum.TurnForward : Page.TurnDirectionEnum.TurnBackward;

        if (Book.isChangingState) return;

        bookinit.leftPlayButton.gameObject.SetActive(false);
        bookinit.rightPlayButton.gameObject.SetActive(false);
        foreach (var vp in bookinit.GetVPlayers())
            vp.Pause();



        switch (Book.CurrentState)
        {
            case EndlessBook.StateEnum.ClosedFront:
                if (direction == Page.TurnDirectionEnum.TurnForward)
                    Book.SetState(EndlessBook.StateEnum.OpenFront, transitionDuration);
                return;

            case EndlessBook.StateEnum.OpenFront:
                if (direction == Page.TurnDirectionEnum.TurnForward)
                {
                    Book.SetState(EndlessBook.StateEnum.OpenMiddle, transitionDuration);
                }
                else
                    Book.SetState(EndlessBook.StateEnum.ClosedFront, transitionDuration);
                return;

            case EndlessBook.StateEnum.OpenMiddle:
                if (direction == Page.TurnDirectionEnum.TurnForward && Book.CurrentRightPageNumber == Book.LastPageNumber)
                    Book.SetState(EndlessBook.StateEnum.OpenBack, transitionDuration);
                else if (direction == Page.TurnDirectionEnum.TurnBackward && Book.CurrentLeftPageNumber <= 1)
                    Book.SetState(EndlessBook.StateEnum.OpenFront, transitionDuration);
                else if (Book.TurnPageDragStart(direction))
                {
                    IsTouching = true;
                    InteractiveManagment.SetInteractionState(InteractionState.TurningPage);
                }
                UIDeactivator();
                return;
            case EndlessBook.StateEnum.OpenBack:
                if (direction == Page.TurnDirectionEnum.TurnBackward)
                {
                    Book.SetState(EndlessBook.StateEnum.OpenMiddle, transitionDuration);
                }
                else
                    Book.SetState(EndlessBook.StateEnum.ClosedBack, transitionDuration);

                return;

            case EndlessBook.StateEnum.ClosedBack:
                if (direction == Page.TurnDirectionEnum.TurnBackward)
                    Book.SetState(EndlessBook.StateEnum.OpenBack, transitionDuration);
                return;
        }
    }

    void HandleTouchDrag(Vector2 screenPos)
    {
        if (!IsTouching || Book.IsTurningPages || !Book.IsDraggingPage) return;

        float normalizedTime = GetNormalizedTime(screenPos);
        Book.TurnPageDrag(Mathf.Clamp01(normalizedTime));
        InteractiveManagment.SetInteractionState(InteractionState.TurningPage);
    }

    void HandleTouchUp()
    {
        if (!IsTouching || Book.IsTurningPages || !Book.IsDraggingPage) return;

        Book.TurnPageDragStop(
            TurnStopSpeed,
            OnPageTurnCompleted,
            reverse: ReversePageIfNotMidway ? (Book.TurnPageDragNormalizedTime < 0.05f) : false
        );
        IsTouching = false;
    }

    void OnPageTurnCompleted(int leftPageNumber, int rightPageNumber)
    {
        InteractiveManagment.SetInteractionState(InteractionState.Ready);

        if (Book.CurrentState == EndlessBook.StateEnum.OpenBack && rightPageNumber < Book.LastPageNumber)
            Book.SetState(EndlessBook.StateEnum.OpenMiddle, transitionDuration);
        else if (Book.CurrentState == EndlessBook.StateEnum.OpenFront && leftPageNumber > 1)
            Book.SetState(EndlessBook.StateEnum.OpenMiddle, transitionDuration);

        if (Book.GetPageData(Book.CurrentLeftPageNumber).hasUI)
            SetGeneratedUIToPage(false);

        if (Book.GetPageData(Book.CurrentRightPageNumber).hasUI)
            SetGeneratedUIToPage(true);
    }

    private void OnBookStateChanged(EndlessBook.StateEnum from, EndlessBook.StateEnum to, int pageNumber)
    {
        if ((from == EndlessBook.StateEnum.OpenBack || from == EndlessBook.StateEnum.OpenFront) && (to == EndlessBook.StateEnum.OpenMiddle))
        {
            if (Book.GetPageData(Book.CurrentLeftPageNumber).hasUI)
                SetGeneratedUIToPage(false);;
            if (Book.GetPageData(Book.CurrentRightPageNumber).hasUI)
                SetGeneratedUIToPage(true);
        }
    }

    float GetNormalizedTime(Vector2 screenPosition)
    {
        Ray ray = SceneCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 localPoint = transform.InverseTransformPoint(hit.point);
            return Mathf.Clamp01(localPoint.x / boxCollider.size.x + 0.5f);
        }

        Vector3 viewportPoint = SceneCamera.ScreenToViewportPoint(screenPosition);
        return Mathf.Clamp01(viewportPoint.x);
    }

    public IEnumerator ShowMessage(string txt, int timeToShow)
    {
        if (TextMessage == null) yield break;

        TextMessage.gameObject.SetActive(true);
        TextMessage.text = txt;
        yield return new WaitForSeconds(timeToShow);
        TextMessage.gameObject.SetActive(false);
    }

    private void SetGeneratedUIToPage(bool pageSide)
    {

        int pageNumber;
        if (!pageSide)
            pageNumber = Book.CurrentLeftPageNumber;
        else
            pageNumber = Book.CurrentRightPageNumber;

        UISO pageUI = Book.GetPageData(pageNumber).UI;

        for (int i = 0; i < pageUI.AudioInfo.Count; i++)
        {
            UIGenerator.Instance.Generator(UI.Audio,
            pageUI,
            pageUI.AudioInfo[i].Position,
            pageSide);

        }
                
        if (Book.GetPageData(Book.CurrentLeftPageNumber).UI.VideoInfo != null)
        {
            bookinit.leftPlayButton.gameObject.SetActive(true);
        }
        if (Book.GetPageData(Book.CurrentRightPageNumber).UI.VideoInfo != null)
        {
            bookinit.rightPlayButton.gameObject.SetActive(true);
        }
    }
}
