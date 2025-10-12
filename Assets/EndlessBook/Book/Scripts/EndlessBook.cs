namespace echo17.EndlessBook
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    public delegate void StateChangedDelegate(EndlessBook.StateEnum fromState,
                                                EndlessBook.StateEnum toState,
                                                int pageNumber);

    public delegate void PageTurnDelegate(Page page,
                                            int pageNumberFront,
                                            int pageNumberBack,
                                            int pageNumberFirstVisible,
                                            int pageNumberLastVisible,
                                            Page.TurnDirectionEnum turnDirection);

    public delegate void TurnPageDragCompleted(int leftPageNumber, int rightPageNumber);

    public class EndlessBook : MonoBehaviour
    {
        public StateChangedDelegate stateChanged;
        public enum TextDirection
        {
            LTR,
            RTL
        }

        public TextDirection textDirection;
        // public Canvas leftPageCanvas;
        // public Canvas rightPageCanvas;
        [Serializable]
        public class MaterialMapping
        {
            public MaterialEnum materialType;
            public List<MaterialRendererMapping> mappings;
        }

        [Serializable]
        public struct MaterialRendererMapping
        {
            public GameObject parentGameObject;
            public Renderer renderer;
            public int index;
        }

        public struct TurnToPageData
        {
            public int pageNumber;
            public float time;
            public float delayBetweenPageTurns;
            public float delayTimeRemaining;
            public float pageTurnTime;
            public int pagesLeftToTurn;
            public int pagesLeftToComplete;
            public int lastActivePageIndex;
            public int lastPageNumber;
            public int farPageNumber;
            public Page.TurnDirectionEnum turnDirection;
            public PageTurnTimeTypeEnum turnTimeType;
            public StateChangedDelegate onCompleted;
            public PageTurnDelegate onPageTurnStart;
            public PageTurnDelegate onPageTurnEnd;
        }

        protected struct StandinQualityQueue
        {
            public StateEnum state;
            public StandinQualityEnum quality;
        }

        protected enum AnimationClipName
        {
            ClosedFrontToOpenMiddle,
            OpenMiddleToClosedBack,
            ClosedFrontToOpenFront,
            OpenFrontToOpenMiddle,
            OpenMiddleToOpenBack,
            OpenBackToClosedBack,
            ClosedFrontToClosedBack,
            OpenFrontToClosedBack,
            OpenFrontToOpenBack,
            ClosedFrontToOpenBack
        }

        protected bool hasInitialized = false;
        protected float[] animationClipLengths;
        protected StateEnum newState;
        protected StateChangedDelegate onCompletedAction;
        protected TurnToPageData turnToPage;
        public bool isChangingState;
        protected bool isTurningPages;
        protected bool isDraggingPage;
        protected int queueMaxPagesTurning;
        protected bool queueStandingQuality;
        protected StandinQualityQueue queueStandinQualityData;
        protected int AnimationSpeedHash = Animator.StringToHash("AnimationSpeed");
        [SerializeField]
        protected Animator bookController = null;
        protected SkinnedMeshRenderer skinnedMeshRenderer = null;
        [SerializeField]
        protected StateEnum currentState;
        [SerializeField]
        protected Transform standinsTransform = null;
        [SerializeField]
        protected Transform pagesTransform = null;
        [SerializeField]
        protected StandinQualityEnum[] standinQualities;
        [SerializeField]
        public GameObject[] standins;
        [SerializeField]
        protected Material[] materials;
        [SerializeField]
        public List<MaterialMapping> materialMappings;
        [SerializeField]
        protected List<PageData> pageData = new List<PageData>();
        [SerializeField]
        protected List<Page> pages = null;
        [SerializeField]
        protected int currentPageNumber;
        [SerializeField]
        protected int maxPagesTurningCount = 5;
        [SerializeField] protected DeltaTimeEnum deltaTime;
        [SerializeField]
        protected Material pageFillerMaterial;
        protected Page.TurnDirectionEnum turnPageDragDirection;
        protected int turnPageDragFinalPage;
        protected float turnPageDragNormalizedTime;
        protected TurnPageDragCompleted turnPageDragCompleted;
        public const string BookPageRightMaterialName = "BookPageRight";
        public const string BookPageLeftMaterialName = "BookPageLeft";
        public enum StateEnum
        {
            ClosedFront = 0,
            OpenFront = 1,
            OpenMiddle = 2,
            OpenBack = 3,
            ClosedBack = 4
        }

        public enum StandinQualityEnum
        {
            High = 0,
            Low = 1,
            Medium = 2
        }

        public enum MaterialEnum
        {
            BookCover = 0,
            BookPageBack = 1,
            BookPageFront = 2,
            BookPageLeft = 3,
            BookPageRight = 4
        }

        public enum PageTurnEnum
        {
            Forward,
            Backward
        }

        public enum PageTurnTimeTypeEnum
        {
            TotalTurnTime,
            TimePerPage
        }

        public enum DeltaTimeEnum
        {
            deltaTime,
            unscaledDeltaTime
        }

        public DeltaTimeEnum DeltaTime { get { return deltaTime; } set { deltaTime = value; } }

        public StateEnum CurrentState { get { return currentState; } }

        public int CurrentPageNumber { get { return currentPageNumber; } }

        public int RTLCurrentPageNumber { get { return LastPageNumber - currentPageNumber + 1; } }

        public int CurrentLeftPageNumber { get { return LeftPageNumber(currentPageNumber); } }

        public int CurrentRightPageNumber { get { return RightPageNumber(currentPageNumber); } }

        public bool IsFirstPageGroup { get { return CurrentLeftPageNumber == 1; } }

        public bool IsLastPageGroup { get { return CurrentRightPageNumber == RightPageNumber(pageData.Count); } }

        public int LastPageNumber { get { return pageData.Count; } }

        public int MaxPagesTurningCount { get { return maxPagesTurningCount; } }

        public Material PageFillerMaterial { get { return pageFillerMaterial; } }

        public bool IsTurningPages { get { return isTurningPages; } }

        public bool IsDraggingPage { get { return isDraggingPage; } }

        public float TurnPageDragNormalizedTime
        {
            get
            {
                return turnPageDragNormalizedTime;
            }
        }


        #region Protected and Hidden Public
        private bool isOnServerMode = true;
        void Awake()
        {
            if(!isOnServerMode)
                Initialize();
        }

        protected virtual void Initialize()
        {
            // cache the animation clip lengths
            if (bookController != null)
            {
                animationClipLengths = new float[System.Enum.GetNames(typeof(AnimationClipName)).Length];

                var ac = bookController.runtimeAnimatorController;
                for (var i = 0; i < ac.animationClips.Length; i++)
                {
                    var index = (int)(AnimationClipName)System.Enum.Parse(typeof(AnimationClipName), ac.animationClips[i].name);
                    animationClipLengths[index] = ac.animationClips[i].length;
                }

                // cache the skinned mesh renderer
                skinnedMeshRenderer = bookController.GetComponentInChildren<SkinnedMeshRenderer>();
            }

            // Set up the turning page index and handlers
            for (var i = 0; i < pages.Count; i++)
            {
                pages[i].Index = i;
                pages[i].pageTurnCompleted = PageTurnCompleted;
            }

            
            hasInitialized = true;
        }

        /// <summary>
        /// The state has completed being set
        /// </summary>
        public virtual void BookAnimationCompleted()
        {
            var oldState = currentState;
            currentState = newState;

            stateChanged?.Invoke(oldState,newState,LastPageNumber - currentPageNumber + 1);
            // turn off the animated book
            if (bookController != null)
            {
                bookController.gameObject.SetActive(false);
            }

            // turn on the standin
            if (standins[(int)currentState] != null)
            {
                standins[(int)currentState].SetActive(true);
            }

            isChangingState = false;

            // if the standin quality has been queued, go ahead and swap out the objects now
            if (queueStandingQuality)
            {
                queueStandingQuality = false;
                SetStandinQuality(queueStandinQualityData.state, queueStandinQualityData.quality);
            }

            // fire the completion handler
            if (onCompletedAction != null) { onCompletedAction(oldState, currentState, currentPageNumber); }
        }

        /// <summary>
        /// Make sure the settings are valid
        /// </summary>
        public virtual void CheckSettings()
        {
            // make sure the standin qualities are the same as the quality enum

            var stateNames = System.Enum.GetNames(typeof(StateEnum));

            if (standinQualities == null)
            {
                standinQualities = new StandinQualityEnum[stateNames.Length];
            }
            else if (standinQualities.Length != stateNames.Length)
            {
                var list = standinQualities.ToList();
                if (list.Count > stateNames.Length)
                {
                    list.RemoveRange(stateNames.Length, list.Count - stateNames.Length);
                }
                else
                {
                    for (var i = list.Count; i < stateNames.Length; i++)
                    {
                        list.Add(StandinQualityEnum.High);
                    }
                }
                standinQualities = list.ToArray();
            }

            // make sure the materials are the same as the material enum

            var materialNames = System.Enum.GetNames(typeof(MaterialEnum));

            if (materials == null)
            {
                materials = new Material[materialNames.Length];
            }
            else if (materials.Length != materialNames.Length)
            {
                var list = materials.ToList();
                if (list.Count > materialNames.Length)
                {
                    list.RemoveRange(materialNames.Length, list.Count - materialNames.Length);
                }
                else
                {
                    for (var i = list.Count; i < materialNames.Length; i++)
                    {
                        list.Add(null);
                    }
                }
                materials = list.ToArray();
            }
        }

        /// <summary>
        /// Returns the standin quality of a particular state
        /// </summary>
        /// <param name="state">The state to check</param>
        /// <returns></returns>
        public virtual StandinQualityEnum GetStandinQuality(StateEnum state)
        {
            return standinQualities[(int)state];
        }

        /// <summary>
        /// Returns the material based on the material enum
        /// </summary>
        /// <param name="mat">Material enum</param>
        /// <returns></returns>
        public virtual Material GetMaterial(MaterialEnum mat)
        {
            return materials[(int)mat];
        }

        public virtual Material[] GetAllMaterials()
        {
            return materials;
        }

        /// <summary>
        /// Loads a standing from the resources folder
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        protected virtual GameObject LoadStandin(StateEnum state)
        {
            var goName = string.Format("BookStandin{0}_{1}", state.ToString(), standinQualities[(int)state].ToString());
            var go = GameObject.Instantiate(Resources.Load<GameObject>(goName), standinsTransform);
            go.name = goName;
            var layer = standinsTransform.gameObject.layer;
            foreach (var t in go.GetComponentsInChildren<Transform>())
            {
                t.gameObject.layer = layer;
            }
            return go;
        }

        /// <summary>
        /// This calculates the turnToPage data
        /// </summary>
        /// <param name="oldState">The state turning from</param>
        /// <param name="newState">The state turning to</param>
        /// <param name="newPageNumber">The new page number to turn to</param>
        protected virtual void TurnToPageInternal(StateEnum oldState, StateEnum newState, int newPageNumber)
        {
            // Reset the page turn completions (in case we manually turned the page before this)
            for (var i = 0; i < pages.Count; i++)
            {
                pages[i].Index = i;
                pages[i].pageTurnCompleted = PageTurnCompleted;
            }

            var currentRightPageNumber = RightPageNumber(newPageNumber);

            // get the page count between the current page and the page to turn to
            var pageDiff = RightPageNumber(turnToPage.pageNumber) - currentRightPageNumber;
            var pageCount = Mathf.Abs(pageDiff);

            // the actual physical pages to turn will be half the page difference since each page has a front and back
            turnToPage.turnDirection = Mathf.Sign(pageDiff) == 1f ? Page.TurnDirectionEnum.TurnForward : Page.TurnDirectionEnum.TurnBackward;
            turnToPage.pagesLeftToTurn = pageCount / 2;
            turnToPage.pagesLeftToComplete = turnToPage.pagesLeftToTurn;

            // calculate turn time
            float totalTurnTime = 1f;
            float timePerPage = 1f;
            switch (turnToPage.turnTimeType)
            {
                case PageTurnTimeTypeEnum.TimePerPage:

                    timePerPage = turnToPage.time;
                    totalTurnTime = timePerPage * ((((float)turnToPage.pagesLeftToTurn - 1f) / (float)maxPagesTurningCount) + 1f);

                    break;

                case PageTurnTimeTypeEnum.TotalTurnTime:

                    totalTurnTime = turnToPage.time;
                    timePerPage = (turnToPage.turnTimeType == PageTurnTimeTypeEnum.TimePerPage ? turnToPage.time : turnToPage.time / ((((float)turnToPage.pagesLeftToTurn - 1f) / (float)maxPagesTurningCount) + 1f));

                    break;
            }

            // calculate the delay between each page turn and the speed to play the turn animation
            turnToPage.delayBetweenPageTurns = (totalTurnTime - timePerPage) / ((float)turnToPage.pagesLeftToTurn - 1f);
            turnToPage.pageTurnTime = timePerPage;

            // set up the initial page values
            turnToPage.lastActivePageIndex = -1;
            turnToPage.farPageNumber = turnToPage.turnDirection == Page.TurnDirectionEnum.TurnForward ? LeftPageNumber(currentPageNumber) : currentRightPageNumber;
            turnToPage.lastPageNumber = currentRightPageNumber;
            turnToPage.delayTimeRemaining = 0;

            isTurningPages = true;
        }

        /// <summary>
        /// This starts the page to turn by dragging
        /// </summary>
        /// <param name="direction">The direction of the turn</param>
        public virtual bool TurnPageDragStart(Page.TurnDirectionEnum direction)
        {
            isDraggingPage = true;

            // cache the direction
            turnPageDragDirection = direction;

            int bookLeftPage;
            int bookRightPage;
            int pageFrontPage;
            int pageBackPage;

            if (direction == Page.TurnDirectionEnum.TurnForward)
            {
                // exit the turning if we are turning forward and already on the last page group of the book
                if (IsLastPageGroup)
                {
                    // turn dragging was not successful
                    isDraggingPage = false;
                    return false;
                }

                // get the book's left and right page numbers
                bookLeftPage = LeftPageNumber(currentPageNumber);
                bookRightPage = RightPageNumber(currentPageNumber + 2);

                // get the turning page's front and back page numbers
                pageFrontPage = RightPageNumber(currentPageNumber);
                pageBackPage = LeftPageNumber(currentPageNumber + 2);
            }
            else
            {
                // exit the turning if we are turning backward and already on the first page group of the book
                if (IsFirstPageGroup)
                {
                    // turn dragging was not successful
                    isDraggingPage = false;
                    return false;
                }

                // get the book's left and right page numbers
                bookLeftPage = LeftPageNumber(currentPageNumber - 2);
                bookRightPage = RightPageNumber(currentPageNumber);

                // get the turning page's front and back page numbers
                pageFrontPage = RightPageNumber(currentPageNumber - 2);
                pageBackPage = LeftPageNumber(currentPageNumber);
            }

            // set the materials of the book's pages
            SetMaterial(MaterialEnum.BookPageLeft, GetPageMaterial(bookLeftPage));
            SetMaterial(MaterialEnum.BookPageRight, GetPageMaterial(bookRightPage));

            // activate a turning page and tell it to begin turning with zero speed
            pages[0].gameObject.SetActive(true);
            pages[0].pageTurnCompleted = null;
            pages[0].Turn(direction, 0, GetPageMaterial(pageFrontPage), GetPageMaterial(pageBackPage));

            // turn dragging was successful
            return true;
        }

        /// <summary>
        /// This drags the page manually. Only call this after calling TurnPageDragStart
        /// </summary>
        /// <param name="normalizedTime">The normalized time of the page turn animation</param>
        public virtual void TurnPageDrag(float normalizedTime)
        {
            // if the turn direction is forward, reverse the normalized time
            turnPageDragNormalizedTime = turnPageDragDirection == Page.TurnDirectionEnum.TurnForward ? 1f - normalizedTime : normalizedTime;

            // set the turning page's normalized time
            pages[0].SetPageNormalizedTime(turnPageDragNormalizedTime);
        }

        /// <summary>
        /// This stops the turn page dragging. Only call this after calling TurnPageDragStart
        /// </summary>
        /// <param name="stopSpeed">The speed of the animation after the page is allowed to animate to its final position</param>
        public virtual void TurnPageDragStop(float stopSpeed, TurnPageDragCompleted turnPageDragCompleted, bool reverse = false)
        {
            this.turnPageDragCompleted = turnPageDragCompleted;

            // calculate the final page of the book after the turn is completed
            if (reverse)
            {
                turnPageDragFinalPage = currentPageNumber;
            }
            else
            {
                turnPageDragFinalPage = currentPageNumber + (turnPageDragDirection == Page.TurnDirectionEnum.TurnForward ? 2 : -2);
            }

            // if the page is turned at least a little
            if (turnPageDragNormalizedTime > 0)
            {
                // set the page turn completion action
                pages[0].pageTurnCompleted = TurnPageDragTurnCompleted;

                // tell the turn page to complete its animation.
                pages[0].PlayRemainder(stopSpeed, reverse);
            }
            else
            {
                // we already completed the turn, so no final animation is necessary.
                // just call the completed action.
                TurnPageDragTurnCompleted(pages[0]);
            }
        }

        /// <summary>
        /// This is called when the turn page completes its final animation
        /// </summary>
        /// <param name="page">The page that completes the animation</param>
        protected virtual void TurnPageDragTurnCompleted(Page page)
        {
            isDraggingPage = false;

            // set the final book page number
            SetPageNumber(turnPageDragFinalPage);

            if (turnPageDragCompleted != null)
            {
                // fire the turn completed event
                turnPageDragCompleted(CurrentLeftPageNumber, CurrentRightPageNumber);
            }
        }

        /// <summary>
        /// Called when a page has completed its turn animation
        /// </summary>
        /// <param name="page"></param>
        public virtual void PageTurnCompleted(Page page)
        {
            // set the new far page number
            turnToPage.farPageNumber += (turnToPage.turnDirection == Page.TurnDirectionEnum.TurnForward ? 1 : -1) * 2;
            turnToPage.pagesLeftToComplete--;

            // if the handler is available, call it
            if (turnToPage.onPageTurnEnd != null)
            {
                if (turnToPage.turnDirection == Page.TurnDirectionEnum.TurnForward)
                {
                    turnToPage.onPageTurnEnd(page, turnToPage.farPageNumber - 1, turnToPage.farPageNumber, turnToPage.farPageNumber, turnToPage.lastPageNumber, turnToPage.turnDirection);
                }
                else
                {
                    turnToPage.onPageTurnEnd(page, turnToPage.farPageNumber, turnToPage.farPageNumber + 1, LeftPageNumber(turnToPage.lastPageNumber), turnToPage.farPageNumber, turnToPage.turnDirection);
                }
            }

            // if have completed all page turns
            if (turnToPage.pagesLeftToComplete == 0)
            {
                isTurningPages = false;

                // if we queued the maximum number of pages that could turn,
                // we can now update the pages.
                if (queueMaxPagesTurning > 0)
                {
                    SetMaxPagesTurningCount(queueMaxPagesTurning);
                    queueMaxPagesTurning = 0;
                }

                // set the current page and its materials
                SetPageNumber(turnToPage.pageNumber);

                // call the completed delegate if necessary
                if (turnToPage.onCompleted != null) turnToPage.onCompleted(StateEnum.OpenMiddle, StateEnum.OpenMiddle, currentPageNumber);
            }
            else
            {
                // still have pages to turn
                // Update the left (first visible) and right (last visible) pages of the book.

                switch (turnToPage.turnDirection)
                {
                    case Page.TurnDirectionEnum.TurnForward: SetMaterial(MaterialEnum.BookPageLeft, page.PageBackMaterial); break;
                    case Page.TurnDirectionEnum.TurnBackward: SetMaterial(MaterialEnum.BookPageRight, page.PageFrontMaterial); break;
                }
            }
        }

        /// <summary>
        /// called every frame
        /// </summary>
        protected virtual void Update()
        {
            // check if turning pages
            if (isTurningPages)
            {
                // if there are still pages left to turn
                if (turnToPage.pagesLeftToTurn > 0)
                {
                    // count down the delay between page turns
                    switch (deltaTime)
                    {
                        case DeltaTimeEnum.deltaTime:
                            turnToPage.delayTimeRemaining -= Time.deltaTime;
                            break;

                        case DeltaTimeEnum.unscaledDeltaTime:
                            turnToPage.delayTimeRemaining -= Time.unscaledDeltaTime;
                            break;
                    }

                    // if the delay timer is zero
                    if (turnToPage.delayTimeRemaining <= 0)
                    {
                        // increment the page index, looping around if we
                        // are at the end to recycle.
                        turnToPage.lastActivePageIndex++;
                        if (turnToPage.lastActivePageIndex > maxPagesTurningCount)
                        {
                            turnToPage.lastActivePageIndex = 0;
                        }

                        // call the TurnPage method on the last page
                        TurnPage(pages[turnToPage.lastActivePageIndex]);
                    }
                }
            }
        }

        /// <summary>
        /// Turns one page
        /// </summary>
        /// <param name="page">The page component to turn</param>
        protected virtual void TurnPage(Page page)
        {
            // decrement the number of pages left to turn
            turnToPage.pagesLeftToTurn--;

            // reset the delay timer
            turnToPage.delayTimeRemaining = turnToPage.delayBetweenPageTurns;

            // set the last page number now that this page is turning
            turnToPage.lastPageNumber += (turnToPage.turnDirection == Page.TurnDirectionEnum.TurnForward ? 1 : -1) * 2;

            // set the front and back page numbers depending on the turn direction.
            // also set the first and last visible page numbers of the book.

            int pageNumberFront = -1;
            int pageNumberBack = -1;
            int pageNumberFirstVisible = -1;
            int pageNumberLastVisible = -1;

            switch (turnToPage.turnDirection)
            {
                case Page.TurnDirectionEnum.TurnForward:

                    pageNumberFront = turnToPage.lastPageNumber - 2;
                    pageNumberBack = turnToPage.lastPageNumber - 1;
                    pageNumberFirstVisible = turnToPage.farPageNumber;
                    pageNumberLastVisible = turnToPage.lastPageNumber;

                    break;

                case Page.TurnDirectionEnum.TurnBackward:

                    pageNumberFront = turnToPage.lastPageNumber;
                    pageNumberBack = turnToPage.lastPageNumber + 1;
                    pageNumberFirstVisible = turnToPage.lastPageNumber - 1;
                    pageNumberLastVisible = turnToPage.farPageNumber;

                    break;
            }

            // call the turn start handler if necessary
            if (turnToPage.onPageTurnStart != null)
            {
                turnToPage.onPageTurnStart(page, pageNumberFront, pageNumberBack, pageNumberFirstVisible, pageNumberLastVisible, turnToPage.turnDirection);
            }

            // set the materials and start the animation
            page.Turn(turnToPage.turnDirection, turnToPage.pageTurnTime, GetPageMaterial(pageNumberFront), GetPageMaterial(pageNumberBack));

            // set the book's left (first visible) and right (last visible) page materials
            switch (turnToPage.turnDirection)
            {
                case Page.TurnDirectionEnum.TurnForward:

                    SetMaterial(MaterialEnum.BookPageRight, GetPageMaterial(pageNumberLastVisible));

                    break;

                case Page.TurnDirectionEnum.TurnBackward:

                    SetMaterial(MaterialEnum.BookPageLeft, GetPageMaterial(pageNumberFirstVisible));

                    break;
            }
        }

        /// <summary>
        /// Creates a page when the max page count is changed
        /// </summary>
        protected virtual Page CreatePage()
        {
            var page = GameObject.Instantiate(Resources.Load<Page>("Page"), pagesTransform);
            page.name = "Page";
            page.pageTurnCompleted = PageTurnCompleted;
            var layer = pagesTransform.gameObject.layer;
            foreach (var t in page.GetComponentsInChildren<Transform>())
            {
                t.gameObject.layer = layer;
            }
            page.gameObject.SetActive(false);
            pages.Add(page);

            return page;
        }

        /// <summary>
        /// Gets the material of the pageData. If none is set
        /// it returns the filler material.
        /// </summary>
        /// <param name="pageNumber">The page number to access</param>
        /// <returns></returns>
        protected virtual Material GetPageMaterial(int pageNumber)
        {
            if (pageNumber <= pageData.Count && pageData.Count > 0)
            {
                return pageData[pageNumber - 1].material;
            }
            else
            {
                return pageFillerMaterial;
            }
        }

        /// <summary>
        /// The right hand page of the group that includes the page number
        /// </summary>
        /// <param name="pageNumber">The page number</param>
        /// <returns></returns>
        protected virtual int RightPageNumber(int pageNumber)
        {
            return (pageNumber % 2 == 0 ? pageNumber : pageNumber + 1);
        }

        /// <summary>
        /// The left hand page of the group that includes the page number
        /// </summary>
        /// <param name="pageNumber">The page number</param>
        /// <returns></returns>
        protected virtual int LeftPageNumber(int pageNumber)
        {
            return (pageNumber % 2 == 1 ? pageNumber : pageNumber - 1);
        }

        /// <summary>
        /// Whether the page number is in the currently visible group of pages
        /// </summary>
        /// <param name="pageNumber">The page number</param>
        /// <returns></returns>
        protected virtual bool IsInCurrentPageGroup(int pageNumber)
        {
            return RightPageNumber(pageNumber) == RightPageNumber(currentPageNumber);
        }

        /// <summary>
        /// Warning log for when an invalid page number is used
        /// </summary>
        protected virtual void LogInvalidPageNumber()
        {
            Debug.LogWarning("Invalid page number. Must be in the range [1.." + pageData.Count + "]");
        }

        /// <summary>
        /// Reset all the material mappings
        /// </summary>
        public virtual void RemapMaterials()
        {
            materialMappings.ForEach(m => m.mappings.Clear());

            if (bookController != null)
            {
                RemapGameObjectMaterials(bookController.gameObject);
            }

            for (var i = 0; i < standins.Length; i++)
            {
                RemapGameObjectMaterials(standins[i]);
            }
        }

        /// <summary>
        /// Remaps the materials for a particular game object (animated or standin)
        /// </summary>
        /// <param name="go"></param>
        protected virtual void RemapGameObjectMaterials(GameObject go)
        {
            if (go == null) return;

            bool wasActive = go.activeSelf;
            go.gameObject.SetActive(true);

            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                AddMaterialMappings(r.transform.parent.gameObject);
            }

            go.SetActive(wasActive);
        }

        /// <summary>
        /// Adds material mappings for a gameobject
        /// </summary>
        /// <param name="go"></param>
        protected virtual void AddMaterialMappings(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;

                for (var j = 0; j < materials.Length; j++)
                {
                    var materialIndex = (int)Enum.Parse(typeof(MaterialEnum), materials[j].name);

                    if (!materialMappings[materialIndex].mappings.Any(x => x.parentGameObject == go && x.renderer == renderer && x.index == j))
                    {
                        var mapping = new MaterialRendererMapping()
                        {
                            parentGameObject = go,
                            renderer = renderer,
                            index = j
                        };

                        materialMappings[materialIndex].mappings.Add(mapping);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public virtual void SetState(StateEnum state,
            float animationTime = 1f,
            StateChangedDelegate onCompleted = null,
            bool stopTurningPages = false
            )
        {
            if (!hasInitialized)
            {
                Initialize();
            }

            if (stopTurningPages)
            {
                StopTurningPages();
            }

            if (state == currentState || isChangingState || isTurningPages) return;

            StartCoroutine(_SetState(state, animationTime, onCompleted));
        }

        protected virtual IEnumerator _SetState(
            StateEnum state,
            float animationTime = 1f,
            StateChangedDelegate onCompleted = null
            )
        {
            isChangingState = true;
            
            newState = state;

            onCompletedAction = onCompleted;

            if (animationTime <= 0)
            {
                standins[(int)currentState].SetActive(false);

                BookAnimationCompleted();
            }
            else
            {
                bookController.gameObject.SetActive(true);

                skinnedMeshRenderer.enabled = false;

                bookController.SetTrigger("Current" + currentState.ToString());
                bookController.SetTrigger("New" + newState.ToString());

                AnimationClipName clipName = AnimationClipName.ClosedFrontToOpenMiddle;
                switch (currentState)
                {
                    case StateEnum.ClosedFront:

                        switch (newState)
                        {
                            case StateEnum.OpenFront: clipName = AnimationClipName.ClosedFrontToOpenFront; break;
                            case StateEnum.OpenMiddle: clipName = AnimationClipName.ClosedFrontToOpenMiddle; break;
                            case StateEnum.OpenBack: clipName = AnimationClipName.ClosedFrontToOpenBack; break;
                            case StateEnum.ClosedBack: clipName = AnimationClipName.ClosedFrontToClosedBack; break;
                        }

                        break;

                    case StateEnum.OpenFront:

                        switch (newState)
                        {
                            case StateEnum.ClosedFront: clipName = AnimationClipName.ClosedFrontToOpenFront; break;
                            case StateEnum.OpenMiddle: clipName = AnimationClipName.OpenFrontToOpenMiddle; break;
                            case StateEnum.OpenBack: clipName = AnimationClipName.OpenFrontToOpenBack; break;
                            case StateEnum.ClosedBack: clipName = AnimationClipName.OpenFrontToClosedBack; break;
                        }

                        break;

                    case StateEnum.OpenMiddle:

                        switch (newState)
                        {
                            case StateEnum.ClosedFront: clipName = AnimationClipName.ClosedFrontToOpenMiddle; break;
                            case StateEnum.OpenFront: clipName = AnimationClipName.OpenFrontToOpenMiddle; break;
                            case StateEnum.OpenBack: clipName = AnimationClipName.OpenMiddleToOpenBack; break;
                            case StateEnum.ClosedBack: clipName = AnimationClipName.OpenMiddleToClosedBack; break;
                        }

                        break;

                    case StateEnum.OpenBack:

                        switch (newState)
                        {
                            case StateEnum.ClosedFront: clipName = AnimationClipName.ClosedFrontToOpenBack; break;
                            case StateEnum.OpenFront: clipName = AnimationClipName.OpenFrontToOpenBack; break;
                            case StateEnum.OpenMiddle: clipName = AnimationClipName.OpenMiddleToOpenBack; break;
                            case StateEnum.ClosedBack: clipName = AnimationClipName.OpenBackToClosedBack; break;
                        }

                        break;

                    case StateEnum.ClosedBack:

                        switch (newState)
                        {
                            case StateEnum.ClosedFront: clipName = AnimationClipName.ClosedFrontToClosedBack; break;
                            case StateEnum.OpenFront: clipName = AnimationClipName.OpenFrontToClosedBack; break;
                            case StateEnum.OpenMiddle: clipName = AnimationClipName.OpenMiddleToClosedBack; break;
                            case StateEnum.OpenBack: clipName = AnimationClipName.OpenBackToClosedBack; break;
                        }

                        break;
                }

                bookController.SetFloat(AnimationSpeedHash, animationClipLengths[(int)clipName] / animationTime);
                yield return null;
                standins[(int)currentState].SetActive(false);
                skinnedMeshRenderer.enabled = true;
                yield return null;
            }
        }

        public virtual void SetPageNumber(int pageNumber)
        {
            currentPageNumber = pageNumber;

            SetMaterial(MaterialEnum.BookPageLeft, GetPageMaterial(LeftPageNumber(pageNumber)));
            SetMaterial(MaterialEnum.BookPageRight, GetPageMaterial(RightPageNumber(pageNumber)));
        }

        public virtual void TurnForward(float time,
            StateChangedDelegate onCompleted = null,
            PageTurnDelegate onPageTurnStart = null,
            PageTurnDelegate onPageTurnEnd = null
            )
        {
            if (currentState != StateEnum.OpenMiddle || IsLastPageGroup) return;

            TurnToPage(CurrentLeftPageNumber + 2, PageTurnTimeTypeEnum.TimePerPage, time, onCompleted: onCompleted, onPageTurnStart: onPageTurnStart, onPageTurnEnd: onPageTurnEnd);
        }

        public virtual void TurnBackward(float time,
            StateChangedDelegate onCompleted = null,
            PageTurnDelegate onPageTurnStart = null,
            PageTurnDelegate onPageTurnEnd = null
            )
        {
            if (currentState != StateEnum.OpenMiddle || IsFirstPageGroup) return;

            TurnToPage(CurrentLeftPageNumber - 2, PageTurnTimeTypeEnum.TimePerPage, time, onCompleted: onCompleted, onPageTurnStart: onPageTurnStart, onPageTurnEnd: onPageTurnEnd);
        }

        public virtual void TurnToPage(int pageNumber, PageTurnTimeTypeEnum turnType, float time,
            float openTime = 1f,
            StateChangedDelegate onCompleted = null,
            PageTurnDelegate onPageTurnStart = null,
            PageTurnDelegate onPageTurnEnd = null
            )
        {
            if (isTurningPages || isChangingState || IsDraggingPage) return;

            if (currentState == StateEnum.OpenMiddle && IsInCurrentPageGroup(pageNumber))
            {
                if (pageNumber == currentPageNumber) return;
                SetPageNumber(pageNumber);
                return;
            }

            if (pageNumber < 1 || pageNumber > pageData.Count)
            {
                LogInvalidPageNumber();
                return;
            }

            turnToPage = new TurnToPageData()
            {
                pageNumber = pageNumber,
                turnTimeType = turnType,
                time = time,
                onCompleted = onCompleted,
                onPageTurnStart = onPageTurnStart,
                onPageTurnEnd = onPageTurnEnd
            };

            if (currentState != StateEnum.OpenMiddle)
            {
                SetState(StateEnum.OpenMiddle, openTime, (IsInCurrentPageGroup(pageNumber) ? onCompleted : TurnToPageInternal));

                return;
            }
            if (IsInCurrentPageGroup(pageNumber))
            {
                SetPageNumber(pageNumber);
                return;
            }

            TurnToPageInternal(StateEnum.OpenMiddle, StateEnum.OpenMiddle, currentPageNumber);
        }

        public virtual void StopTurningPages()
        {
            if (!isTurningPages) return;

            isTurningPages = false;

            for (var i = 0; i < pages.Count; i++)
            {
                pages[i].gameObject.SetActive(false);
            }

            SetPageNumber(turnToPage.pageNumber);
        }

        public virtual void SetStandinQuality(StateEnum state, StandinQualityEnum quality)
        {
            
            if (state != StateEnum.OpenMiddle) return;

            var i = (int)state;

            if (isChangingState)
            {
                queueStandingQuality = true;
                queueStandinQualityData = new StandinQualityQueue()
                {
                    state = state,
                    quality = quality
                };

                return;
            }

            standinQualities[i] = quality;


            materialMappings.ForEach(s => s.mappings.RemoveAll(x => x.parentGameObject == standins[i]));

            if (Application.isPlaying)
            {
                Destroy(standins[i]);
            }
            else
            {
                DestroyImmediate(standins[i]);
            }

            standins[i] = LoadStandin(state);
            AddMaterialMappings(standins[i]);

            standins[i].SetActive(currentState == state);

            var materialNames = System.Enum.GetNames(typeof(MaterialEnum));
            for (var j = 0; j < materialNames.Length; j++)
            {
                if (materialNames[j] != BookPageRightMaterialName && materialNames[j] != BookPageLeftMaterialName)
                {
                    SetMaterial((MaterialEnum)j, materials[j]);
                }
            }

            if (state == StateEnum.OpenMiddle)
            {
                SetPageNumber(currentPageNumber);
            }
        }

        public virtual void SetMaterial(MaterialEnum materialType, Material material)
        {
            // update the materials list.
            materials[(int)materialType] = material;

            foreach (var mapping in materialMappings[(int)materialType].mappings)
            {
                if (mapping.renderer != null)
                {
                    var sharedMaterials = mapping.renderer.sharedMaterials;
                    sharedMaterials[mapping.index] = material;
                    mapping.renderer.sharedMaterials = sharedMaterials;
                }
            }
        }

        public void GetMaterialsFromServer(List<Material> materials)
        {

        }


        public virtual void SetMaxPagesTurningCount(int newCount)
        {
            if (isTurningPages)
            {
                queueMaxPagesTurning = newCount;
                return;
            }
            for (var i = pagesTransform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(pagesTransform.GetChild(i).gameObject);
                }
                else
                {
                    DestroyImmediate(pagesTransform.GetChild(i).gameObject);
                }
            }
            pages.Clear();

            for (var i = 0; i < newCount + 1; i++)
            {
                var page = CreatePage();
                page.Index = i;
            }

            maxPagesTurningCount = newCount;
        }

        public virtual void SetPageFillerMaterial(Material material)
        {
            pageFillerMaterial = material;

            SetPageNumber(currentPageNumber);
        }

        public virtual PageData GetPageData(int pageNumber)
        {
            if (pageNumber < 1 || pageNumber > pageData.Count)
            {
                LogInvalidPageNumber();
                return PageData.Default();
            }

            return pageData[pageNumber - 1];
        }

        public virtual PageData AddPageData(Material newMaterial = null)
        {
            var newPage = new PageData()
            {
                material = (newMaterial == null) ? pageFillerMaterial : newMaterial
            };

            pageData.Add(newPage);

            if (IsInCurrentPageGroup(pageData.Count))
            {
                SetPageNumber(currentPageNumber);
            }
            return newPage;
        }

        public virtual PageData InsertPageData(int pageNumber, Material newMaterial = null)
        {
            // create page data
            var newPage = new PageData()
            {
                material = (newMaterial == null) ? pageFillerMaterial : newMaterial
            };

            pageData.Insert(pageNumber - 1, newPage);

            if (IsInCurrentPageGroup(pageNumber))
            {
                SetPageNumber(currentPageNumber);
            }
            return newPage;
        }

        public virtual void SetPageData(int pageNumber, PageData data)
        {
            if (pageNumber < 1 || pageNumber > pageData.Count)
            {
                LogInvalidPageNumber();
                return;
            }
            pageData[pageNumber - 1] = data;

            if (IsInCurrentPageGroup(pageNumber))
            {
                SetPageNumber(currentPageNumber);
            }
        }

        public virtual void RemovePageData(int pageNumber)
        {
            if (pageNumber < 1 || pageNumber > pageData.Count)
            {
                LogInvalidPageNumber();
                return;
            }
            pageData.RemoveAt(pageNumber - 1);

            if (currentPageNumber > pageData.Count)
            {
                SetPageNumber(currentPageNumber - 1);
            }
        }

        public virtual void MovePageData(int pageNumber, int direction)
        {
            direction = Math.Sign(direction);
            if (pageNumber < 1 || pageNumber > LastPageNumber)
            {
                LogInvalidPageNumber();
                return;
            }
            if (direction == 0 || (pageNumber == 1 && direction == -1) || (pageNumber == LastPageNumber && direction == 1)) return;

            var data = pageData[pageNumber - 1 + direction];
            pageData[pageNumber - 1 + direction] = pageData[pageNumber - 1];
            pageData[pageNumber - 1] = data;

            SetPageNumber(CurrentPageNumber);
        }

        #endregion
    }
}
