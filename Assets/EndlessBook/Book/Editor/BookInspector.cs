using UnityEngine;
using UnityEditor;
using echo17.EndlessBook;

/// <summary>
/// Handles updating most values of the Book object
/// </summary>
[CustomEditor(typeof(EndlessBook))]
public class BookInspector : Editor
{
    /// <summary>
    /// Must have at least one page to turn
    /// </summary>
    protected const int MaxPagesTurningCountMin = 1;
    /// <summary>
    /// The width of the page name column
    /// </summary>
    protected const int PageNameWidth = 60;

    /// <summary>
    /// The width of the page buttons column
    /// </summary>
    protected const int PageButtonWidth = 80;

    /// <summary>
    /// The book object to update
    /// </summary>
    protected EndlessBook book;

    /// <summary>
    /// Used to fold the pages list in the inspector.
    /// Useful for hiding large numbers of pages
    /// </summary>
    protected bool showPages = true;

    /// <summary>
    /// The names of all the materials cached
    /// </summary>
    protected string[] materialNames;


    void OnEnable()
    {
        // set the book
        book = (EndlessBook)target;

        // make sure the settings are up to date
        book.CheckSettings();

        // cache the material names
        materialNames = System.Enum.GetNames(typeof(EndlessBook.MaterialEnum));

    }

    /// <summary>
    /// Called when the inspector is updated
    /// </summary>
    public override void OnInspectorGUI()
    {
        // EditorGUILayout.LabelField("Canvas Page of Pages", EditorStyles.boldLabel);
        // book.rightPageCanvas = (Canvas)EditorGUILayout.ObjectField("Right Canvas", book.rightPageCanvas, typeof(Canvas), true);
        // book.leftPageCanvas = (Canvas)EditorGUILayout.ObjectField("Left Canvas", book.leftPageCanvas, typeof(Canvas), true);


        EditorGUILayout.LabelField("Text Direction", EditorStyles.boldLabel);

        var lastTextDirection = book.textDirection;

        book.textDirection = (EndlessBook.TextDirection)EditorGUILayout.EnumPopup("Direction", book.textDirection);

        if (lastTextDirection != book.textDirection)
        {
            if (book.textDirection == EndlessBook.TextDirection.LTR)
            {
                book.SetState(EndlessBook.StateEnum.ClosedFront, 0, null);
                book.SetPageNumber(1);
            }
            else
            {
                book.SetState(EndlessBook.StateEnum.ClosedBack, 0, null);
                book.SetPageNumber(book.LastPageNumber);
            }
        }


        // update the current state
        var newCurrentState = (EndlessBook.StateEnum)EditorGUILayout.EnumPopup("Current State", book.CurrentState);
        if (newCurrentState != book.CurrentState)
        {
            Undo.RecordObject(target, "Current State Changed");
            book.SetState(newCurrentState, 0, null);
        }

        // update the OpenMiddle quality
        var state = EndlessBook.StateEnum.OpenMiddle;
        var currentQuality = book.GetStandinQuality(state);
        var newQuality = (EndlessBook.StandinQualityEnum)EditorGUILayout.EnumPopup("Open Middle Quality", currentQuality);
        if (newQuality != currentQuality)
        {
            Undo.RecordObject(target, "Standin Quality " + state.ToString() + " changed");
            book.SetStandinQuality(state, (EndlessBook.StandinQualityEnum)newQuality);
        }

        EditorGUILayout.Space();

        for (var i = 0; i < materialNames.Length; i++)
        {
            if (materialNames[i] != EndlessBook.BookPageRightMaterialName && materialNames[i] != EndlessBook.BookPageLeftMaterialName)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(materialNames[i]);

                var material = (EndlessBook.MaterialEnum)i;
                var currentMaterial = book.GetMaterial(material);
                var newMaterial = (Material)EditorGUILayout.ObjectField(currentMaterial, typeof(Material), false);
                if (newMaterial != currentMaterial)
                {
                    Undo.RecordObject(target, "Material " + material.ToString() + " changed");
                    book.SetMaterial(material, newMaterial);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();

        // set the maximum number of pages that can turn
        var newMaxPagesTurning = EditorGUILayout.IntSlider("Max Pages Turning", book.MaxPagesTurningCount, 1, 10);
        if (newMaxPagesTurning != book.MaxPagesTurningCount)
        {
            if (newMaxPagesTurning < MaxPagesTurningCountMin)
            {
                newMaxPagesTurning = MaxPagesTurningCountMin;
            }

            if (newMaxPagesTurning != book.MaxPagesTurningCount)
            {
                Undo.RecordObject(target, "Max Pages Turning changed");
                book.SetMaxPagesTurningCount(newMaxPagesTurning);
            }
        }

        EditorGUILayout.Space();

        // set the time delta method
        var newDeltaTime = (EndlessBook.DeltaTimeEnum)EditorGUILayout.EnumPopup("Delta Time Method", book.DeltaTime);
        if (newDeltaTime != book.DeltaTime)
        {
            Undo.RecordObject(target, "Delta Time Changed");
            book.DeltaTime = newDeltaTime;
        }

        EditorGUILayout.Space();

        // set the current page number
        var newCurrentPageNumber = EditorGUILayout.IntSlider("Current Page", book.CurrentPageNumber, 1, book.LastPageNumber);
        if (newCurrentPageNumber != book.CurrentPageNumber)
        {
            if (newCurrentPageNumber < 1)
            {
                newCurrentPageNumber = 1;
            }

            if (newCurrentPageNumber > book.LastPageNumber)
            {
                newCurrentPageNumber = book.LastPageNumber;
            }

            if (newCurrentPageNumber != book.CurrentPageNumber)
            {
                Undo.RecordObject(target, "Current Page changed");
                book.SetPageNumber(newCurrentPageNumber);
            }
        }

        EditorGUILayout.Space();

        // update filler material
        var newLastPageFillerMaterial = (Material)EditorGUILayout.ObjectField("Page Filler Material", book.PageFillerMaterial, typeof(Material), false);
        if (newLastPageFillerMaterial != book.PageFillerMaterial)
        {
            Undo.RecordObject(target, "Last Page Filler Material changed");
            book.SetPageFillerMaterial(newLastPageFillerMaterial);
        }

        EditorGUILayout.Space();

        // set the page data (materials)
        showPages = EditorGUILayout.Foldout(showPages, "Pages");
        if (showPages)
        {
            for (var i = 0; i < book.LastPageNumber; i++)
    {
        EditorGUILayout.BeginHorizontal();

        var pageData = book.GetPageData(i + 1);

        EditorGUILayout.LabelField("Page " + (i + 1).ToString(), GUILayout.Width(PageNameWidth));

    
        var newMaterial = (Material)EditorGUILayout.ObjectField(pageData.material, typeof(Material), false, GUILayout.Width(125));
        if (newMaterial != pageData.material)
        {
            Undo.RecordObject(target, "Page " + (i + 1).ToString() + " changed (material)");
            pageData.material = newMaterial;
            book.SetPageData(i + 1, pageData);
        }

        // 🔁 Toggle per-page
        bool newToggle = GUILayout.Toggle(pageData.hasUI, "Has UI", GUI.skin.toggle, GUILayout.Width(CalcTextWidth("Has UI")));
        if (newToggle != pageData.hasUI)
        {
            Undo.RecordObject(target, "Page " + (i + 1).ToString() + " changed (hasUI)");
            pageData.hasUI = newToggle;
            book.SetPageData(i + 1, pageData);
        }

        if (pageData.hasUI)
        {
            UISO newSO = (UISO)EditorGUILayout.ObjectField(pageData.UI,typeof(UISO),false);

            if (newSO != pageData.UI)
            {
                Undo.RecordObject(target, "Change UI Scriptable");
                pageData.UI = newSO;
                EditorUtility.SetDirty(target);
            }
        }

        // دکمه‌ها
        EditorGUILayout.BeginHorizontal(GUILayout.Width(PageButtonWidth));

        if (GUILayout.Button(new GUIContent("+", "Insert Page"), EditorStyles.miniButtonLeft))
        {
            Undo.RecordObject(target, "Page " + (i + 1).ToString() + " inserted");
            book.InsertPageData(i + 1);
        }

        if (GUILayout.Button(new GUIContent("-", "Remove Page"), EditorStyles.miniButtonMid))
        {
            Undo.RecordObject(target, "Page " + (i + 1).ToString() + " removed");
            book.RemovePageData(i + 1);
            continue;
        }

        if (i > 0 && GUILayout.Button(new GUIContent("▲", "Move Page Up"), EditorStyles.miniButtonMid))
        {
            Undo.RecordObject(target, "Page " + (i + 1).ToString() + " moved up");
            book.MovePageData(i + 1, -1);
        }

        if (i < book.LastPageNumber - 1 && GUILayout.Button(new GUIContent("▼", "Move Page Down"), EditorStyles.miniButtonRight))
        {
            Undo.RecordObject(target, "Page " + (i + 1).ToString() + " moved down");
            book.MovePageData(i + 1, 1);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("");

            // add page to the end
            if (GUILayout.Button("Add Page"))
            {
                Undo.RecordObject(target, "Page added");
                book.AddPageData();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }
    }
    float CalcTextWidth(string text)
    {
        GUIStyle style = GUI.skin.toggle;
        return style.CalcSize(new GUIContent(text)).x + 24f; 
    }
}
