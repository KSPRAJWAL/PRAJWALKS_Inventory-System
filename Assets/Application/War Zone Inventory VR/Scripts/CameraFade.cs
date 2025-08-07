using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraFade : MonoBehaviour
{
    [SerializeField] float m_AnimTime = 1.0f;
    [SerializeField] Image m_FadeImage;
    float internalTimer;
    Color tempColor;
    Color targetColor;

    public void FadeToBlack()
    {
        internalTimer = 0;
        targetColor = Color.black;
        targetColor.a = 1.0f;
        StartCoroutine(FadeImage());
    }    

    public void FadeFromBlack()
    {
        internalTimer = 0;
        targetColor.a = 0.0f;
        StartCoroutine(FadeImage());
    }

    IEnumerator FadeImage()
    {
        while(internalTimer < m_AnimTime)
        {
            internalTimer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
            tempColor = Color.Lerp(tempColor, targetColor, internalTimer / m_AnimTime);
            m_FadeImage.color = tempColor;
        }
        m_FadeImage.color = targetColor;
    }
}
