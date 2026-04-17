using Common.Runtime.Components;

using Core;

using UnityEngine;

[RequireComponent(typeof(Camera), typeof(ScreenShake))]
public class CameraManager : MonoBehaviour
{
    private Camera _camera;
    private ScreenShake _screenShake { get; set; }
    
    
    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _screenShake = GetComponent<ScreenShake>();
        
        // Self-registration with G service locator
        if (G.Camera != null && G.Camera != this)
        {
            Debug.LogWarning("[Camera] Multiple CameraManager instances detected. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        G.Camera = this;
        G.EnsureSystem("Camera", G.Camera);
    }
    

    private void OnDestroy()
    {
        // Unregister from G
        if (G.Camera == this)
            G.Camera = null;
    }
    
    public void UpdateScreenShakeSetting(float newSetting) => _screenShake.UpdateScreenShakeSetting(newSetting);
}