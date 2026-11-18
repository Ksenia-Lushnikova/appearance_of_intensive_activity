using UnityEngine;

public class ExitGameButton : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Кнопка выхода нажата! В готовой .exe игре окно закроется.");
        Application.Quit();
    }
}
