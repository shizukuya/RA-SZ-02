using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// 引数でもらったSceneへ遷移する
public class MoveScene : MonoBehaviour
{
    private float delayTime = 0.5f;
    public AudioSource audioSource;
    public AudioClip clip;

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneWithDelay(sceneName));
    }

    private IEnumerator LoadSceneWithDelay(string sceneName)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip);
            yield return new WaitForSecondsRealtime(delayTime);
        }
        
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
