
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneControls : MonoBehaviour
{
    public static short numScenes = 6;

    public int sceneIndex = 0;
    public TMP_Text numEntitiesText;
    public TestManager testManager;

    void Update()
    {
        numEntitiesText.text = $"{testManager.GetNumEntities()} /\n{testManager.GetTargetNumEntities()}\nEntities";
    }

    public void IncreaseEntities()
    {
        // get magnitude
        int target = testManager.GetTargetNumEntities();
        int mag = 0;
        while (target > 9)
        {
            target /= 10;
            mag++;
        }

        // get increment amount
        int inc = (int)math.pow(10, mag) / 2;
        testManager.SetTargetNumEntities(testManager.GetTargetNumEntities() + inc);
    }

    public void DecreaseEntities()
    {
        // get magnitude
        int target = testManager.GetTargetNumEntities();
        int mag = 0;
        while (target > 9)
        {
            target /= 10;
            mag++;
        }
        if (target == 1)
            mag--;

        // get decrement amount
        int dec = (int)math.pow(10, mag) / 2;
        testManager.SetTargetNumEntities(testManager.GetTargetNumEntities() - dec);
    }

    public void NextScene()
    {
        SceneManager.LoadScene(CapSceneIndex(sceneIndex + 1));
    }

    public void PreviousScene()
    {
        SceneManager.LoadScene(CapSceneIndex(sceneIndex - 1));
    }

    public int CapSceneIndex(int i)
    {
        if (i < 0)
            i = numScenes - 1;
        if (i >= numScenes)
            i = 0;
        return i;
    }
}