
using TMPro;
using UnityEngine;

public class SceneControls : MonoBehaviour
{
    public static short numScenes = 0;

    public int sceneIndex = 0;
    public TMP_Text numEntitiesText;
    public TestManager testManager;

    public void IncreaseEntities()
    {
        Debug.Log("IncreaseEntities!");
    }

    public void DecreaseEntities()
    {
        Debug.Log("DecreaseEntities!");
    }

    public void NextScene()
    {
        Debug.Log("NextScene!");
    }

    public void PreviousScene()
    {
        Debug.Log("PreviousScene!");
    }
}