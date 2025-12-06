using UnityEngine;

namespace MidniteOilSoftware.Multiplayer.SpaceWar
{
    public class ScreenWrapper : MonoBehaviour
    {
        Camera _mainCamera;
        Vector2 _screenBounds;
        float _objectWidth;
        float _objectHeight;

        void Start()
        {
            _mainCamera = Camera.main;

            if (!_mainCamera)
            {
                Debug.LogError("ScreenWrapper: No main camera found!", this);
                enabled = false;
                return;
            }

            CalculateScreenBounds();
            CalculateObjectSize();
        }

        void CalculateScreenBounds()
        {
            if (_mainCamera.orthographic)
            {
                _screenBounds = new Vector2(
                    _mainCamera.orthographicSize * _mainCamera.aspect,
                    _mainCamera.orthographicSize
                );
            }
            else
            {
                var distance = Mathf.Abs(_mainCamera.transform.position.z - transform.position.z);
                _screenBounds = new Vector2(
                    distance * Mathf.Tan(_mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * _mainCamera.aspect,
                    distance * Mathf.Tan(_mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad)
                );
            }
        }

        void CalculateObjectSize()
        {
            var fighterRenderer = GetComponentInChildren<Renderer>();
            if (fighterRenderer)
            {
                _objectWidth = fighterRenderer.bounds.extents.x;
                _objectHeight = fighterRenderer.bounds.extents.y;
            }
            else
            {
                _objectWidth = 0.5f;
                _objectHeight = 0.5f;
            }
        }

        void LateUpdate()
        {
            var position = transform.position;
            var wrapped = false;

            if (position.x > _screenBounds.x + _objectWidth)
            {
                position.x = -_screenBounds.x - _objectWidth;
                wrapped = true;
            }
            else if (position.x < -_screenBounds.x - _objectWidth)
            {
                position.x = _screenBounds.x + _objectWidth;
                wrapped = true;
            }

            if (position.y > _screenBounds.y + _objectHeight)
            {
                position.y = -_screenBounds.y - _objectHeight;
                wrapped = true;
            }
            else if (position.y < -_screenBounds.y - _objectHeight)
            {
                position.y = _screenBounds.y + _objectHeight;
                wrapped = true;
            }

            if (wrapped)
            {
                transform.position = position;
            }
        }
    }
}
