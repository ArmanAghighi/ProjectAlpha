using TMPro;
using UnityEngine;
using System.Collections;
public class ErrorTextHandler : MonoBehaviour
{
    public static ErrorTextHandler Instance;
    [SerializeField] private TextMeshProUGUI errorText;

    private GameObject errorGameObject;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        errorGameObject = errorText.gameObject.transform.parent.gameObject;
        errorGameObject.SetActive(false);
    }

    public IEnumerator SetErrorText(string error, int time)
    {
        errorGameObject.SetActive(true);
        errorText.text = error;
        yield return new WaitForSeconds(time);
        errorText.text = string.Empty;
        errorGameObject.SetActive(false);
    }
}
