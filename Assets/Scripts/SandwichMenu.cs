using TMPro;
using UnityEngine;
using WebARFoundation;

public class SandwichMenu : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public TextMeshProUGUI resultText;
    public GameObject debugPanel;
    public MindARImageTrackingManager mind;

    void Start()
    {
        dropdown.onValueChanged.AddListener(OnOptionSelected);
        OnOptionSelected(dropdown.value);

    }
    
    
    void OnOptionSelected(int index)
    {
        switch (index)
        {
            case 0:
                mind.StartAR();
                break;
            case 1:
                mind.StopAR();
                break;
            case 2:
                //mind.reset.onClick.Invoke();
                break;
            case 3:
                debugPanel.gameObject.SetActive(!debugPanel.activeSelf);
                break;
            default:
                break;
        }
        dropdown.SetValueWithoutNotify(0);
    }
}
