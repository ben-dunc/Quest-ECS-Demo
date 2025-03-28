using com.cyborgAssets.inspectorButtonPro;
using UnityEngine;
using UnityEngine.Events;

public class SimpleButton : MonoBehaviour
{
    public Material buttonMaterial;
    public UnityEvent onClick = new();
    bool pressed = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !pressed)
        {
            onClick.Invoke();
            pressed = true;
            buttonMaterial.SetInt("_ButtonLit", 1);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player" && pressed)
        {
            pressed = false;
            buttonMaterial.SetInt("_ButtonLit", 0);
        }
    }

    [ProButton]
    void InvokeButton()
    {
        onClick.Invoke();
    }
}
