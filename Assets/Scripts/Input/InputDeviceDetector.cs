using UnityEngine;

/// <summary>
/// Détecte le dernier périphérique utilisé (clavier ou manette) avec l'ancien Input Manager.
/// Singleton auto-instancié, persistant entre les scènes. Émet OnDeviceChanged au changement.
/// </summary>
public class InputDeviceDetector : MonoBehaviour
{
    public static InputDeviceDetector Instance { get; private set; }

    public enum Device { Keyboard, Gamepad }

    public Device CurrentDevice { get; private set; } = Device.Keyboard;
    public event System.Action<Device> OnDeviceChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (FindObjectOfType<InputDeviceDetector>() != null) return;
        var go = new GameObject("InputDeviceDetector (auto)");
        go.AddComponent<InputDeviceDetector>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Si une manette est déjà branchée et aucun clavier utilisé, on part sur Gamepad.
        foreach (var name in Input.GetJoystickNames())
        {
            if (!string.IsNullOrEmpty(name))
            {
                SetDevice(Device.Gamepad);
                break;
            }
        }
    }

    void Update()
    {
        // Les boutons de manette sont des KeyCode JoystickButton* — testés en premier.
        if (AnyJoystickButtonDown())
        {
            SetDevice(Device.Gamepad);
        }
        else if (AnyKeyboardKeyDown())
        {
            SetDevice(Device.Keyboard);
        }
    }

    private void SetDevice(Device device)
    {
        if (device == CurrentDevice) return;
        CurrentDevice = device;
        OnDeviceChanged?.Invoke(device);
    }

    private static bool AnyJoystickButtonDown()
    {
        for (KeyCode k = KeyCode.JoystickButton0; k <= KeyCode.JoystickButton19; k++)
        {
            if (Input.GetKeyDown(k)) return true;
        }
        return false;
    }

    private static bool AnyKeyboardKeyDown()
    {
        if (!Input.anyKeyDown) return false;
        // anyKeyDown inclut souris et manette : on les exclut.
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            return false;
        }
        for (KeyCode k = KeyCode.JoystickButton0; k <= KeyCode.JoystickButton19; k++)
        {
            if (Input.GetKeyDown(k)) return false;
        }
        return true;
    }
}
