using UnityEngine;

namespace MidniteOilSoftware.Multiplayer.SpaceWar
{
    public class FighterVisuals : MonoBehaviour
    {

        [Header("Visual Effects")] 
        [SerializeField] Renderer _shipRenderer;
        [SerializeField] GameObject _exhaust;
        [SerializeField] Collider2D _collider;
        [SerializeField] Transform _muzzleTransform;

        public void EnableVisuals(bool enable)
        {
            Debug.Log($"FighterVisuals.EnableVisuals({enable}) called on {gameObject.name}. Renderer={(_shipRenderer ? "found" : "null")}, RendererEnabled={(_shipRenderer ? _shipRenderer.enabled.ToString() : "N/A")}", this);
        
            if (_shipRenderer) _shipRenderer.enabled = enable;
            if (_collider) _collider.enabled = enable;
            if (!enable) _exhaust?.SetActive(false);
        }
    
        public void ShowExhaust(bool show)
        {
            _exhaust?.SetActive(show && _shipRenderer.enabled);
        }
    
        public Transform MuzzleTransform => _muzzleTransform;
    }

}