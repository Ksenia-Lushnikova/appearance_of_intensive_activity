using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class OfficeOptions : MonoBehaviour
{
    public GameObject panel;
    public Slider volumeSlider;
    public Text volumeValue;
    public Text muteLabel;
    public Button muteButton;
    public Button closeButton;
    public Button openButton;
    public Button playButton;
    public Button menuButton;
    public Button quitButton;
    public OfficeGameController game;

    public bool IsOpen => panel.activeSelf;

    private float previousTimeScale;
    private float nextPreview;
    private GameObject previousSelection;

    private void Start()
    {
        OfficeAudio.Initialize();
        openButton?.onClick.AddListener(Toggle);
        closeButton.onClick.AddListener(Toggle);
        muteButton.onClick.AddListener(ToggleMute);
        volumeSlider.onValueChanged.AddListener(ChangeVolume);
        playButton?.onClick.AddListener(() => OfficeAudio.Play(OfficeAudio.Cue.Screen));
        quitButton?.onClick.AddListener(Quit);
        menuButton?.onClick.AddListener(ReturnToMenu);
        Refresh();
    }

    private void Update()
    {
        if (game == null && IsOpen && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            Toggle();
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            previousSelection = EventSystem.current?.currentSelectedGameObject;
            previousTimeScale = Time.timeScale;
            if (game != null)
                Time.timeScale = 0f;
            Refresh();
            panel.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(closeButton.gameObject);
        }
        OfficeAudio.Play(OfficeAudio.Cue.Click);
    }

    private void Close()
    {
        if (game != null)
            Time.timeScale = previousTimeScale;
        panel.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(previousSelection);
        PlayerPrefs.Save();
    }

    private void ReturnToMenu()
    {
        Close();
        game.ReturnToMenu();
    }

    private void Quit()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ChangeVolume(float value)
    {
        OfficeAudio.SetVolume(value / 100f);
        Refresh();
        if (Time.unscaledTime < nextPreview || OfficeAudio.Muted)
            return;

        nextPreview = Time.unscaledTime + 0.14f;
        OfficeAudio.Play(OfficeAudio.Cue.Click);
    }

    private void ToggleMute()
    {
        OfficeAudio.ToggleMute();
        Refresh();
        OfficeAudio.Play(OfficeAudio.Cue.Click);
    }

    private void Refresh()
    {
        volumeSlider.SetValueWithoutNotify(OfficeAudio.Volume * 100f);
        volumeValue.text = $"{volumeSlider.value:0}%";
        muteLabel.text = OfficeAudio.Muted ? "ВКЛЮЧИТЬ ЗВУК" : "ВЫКЛЮЧИТЬ ЗВУК";
    }

    private void OnDisable()
    {
        if (game != null && panel != null && IsOpen)
            Time.timeScale = previousTimeScale;
    }
}
