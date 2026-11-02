using UnityEngine;

namespace ErasOfWar.Unity.Base
{
    /// <summary>
    /// Orthographic camera for a portrait phone: fits the whole grid (plus perimeter) to the screen width,
    /// leaves room for the HUD bars, and supports pan and zoom. World units = grid tiles.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BaseCameraRig : MonoBehaviour
    {
        // Fraction of the screen height covered by HUD at the top / bottom (matches BaseHud layout).
        public const float TopHudFraction = 0.08f;
        public const float BottomHudFraction = 0.24f;
        private const float Margin = 1.5f; // tiles of empty space around the perimeter

        private Camera _cam;
        private int _gridSize;
        private float _fitSize;
        private Vector2Int _lastScreen;

        public Camera Camera => _cam != null ? _cam : (_cam = GetComponent<Camera>());

        private void Awake()
        {
            Camera.orthographic = true;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = PlaceholderArt.Background;
        }

        private void LateUpdate()
        {
            // Re-fit when the screen size or orientation changes (e.g. switching the Game view resolution).
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen != _lastScreen && _gridSize > 0)
            {
                _lastScreen = screen;
                Fit(_gridSize);
            }
        }

        /// <summary>Show the whole grid. Called on start and whenever the grid size changes.</summary>
        public void Fit(int gridSize)
        {
            _gridSize = gridSize;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            var aspect = Mathf.Max(0.1f, Camera.aspect);
            var span = gridSize + 2f + Margin * 2f;                 // grid + perimeter ring + margin
            var usableHeight = 1f - TopHudFraction - BottomHudFraction;

            var sizeForWidth = span / aspect / 2f;
            var sizeForHeight = span / usableHeight / 2f;
            _fitSize = Mathf.Max(sizeForWidth, sizeForHeight);
            Camera.orthographicSize = _fitSize;

            // Centre the grid in the area between the HUD bars.
            var shift = (BottomHudFraction - TopHudFraction) * _fitSize; // world units
            transform.position = new Vector3(gridSize / 2f, gridSize / 2f - shift, -10f);
        }

        /// <summary>Move the view by a screen-space finger delta (content follows the finger).</summary>
        public void Pan(Vector2 screenDelta)
        {
            var unitsPerPixel = Camera.orthographicSize * 2f / Mathf.Max(1, Screen.height);
            var p = transform.position - (Vector3)(screenDelta * unitsPerPixel);
            transform.position = Clamp(p);
        }

        /// <summary>Zoom by a factor (&gt;1 zooms in) keeping the world point under <paramref name="screenPivot"/> fixed.</summary>
        public void Zoom(float factor, Vector2 screenPivot)
        {
            if (factor <= 0f) return;
            var before = ScreenToWorld(screenPivot);
            var minSize = 4f;
            var maxSize = Mathf.Max(minSize, _fitSize * 1.25f);
            Camera.orthographicSize = Mathf.Clamp(Camera.orthographicSize / factor, minSize, maxSize);
            var after = ScreenToWorld(screenPivot);
            transform.position = Clamp(transform.position + (Vector3)(before - after));
        }

        public Vector2 ScreenToWorld(Vector2 screen)
        {
            var w = Camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -transform.position.z));
            return new Vector2(w.x, w.y);
        }

        public Vector2Int ScreenToTile(Vector2 screen)
        {
            var w = ScreenToWorld(screen);
            return new Vector2Int(Mathf.FloorToInt(w.x), Mathf.FloorToInt(w.y));
        }

        /// <summary>The tile at the centre of the visible play area.</summary>
        public Vector2Int CenterTile()
        {
            var screenCenter = new Vector2(Screen.width / 2f,
                Screen.height * (BottomHudFraction + (1f - TopHudFraction - BottomHudFraction) / 2f));
            return ScreenToTile(screenCenter);
        }

        private Vector3 Clamp(Vector3 p)
        {
            // Keep the grid centre reachable: never pan further than half a grid past the edges.
            var lo = -_gridSize * 0.25f;
            var hi = _gridSize * 1.25f;
            return new Vector3(Mathf.Clamp(p.x, lo, hi), Mathf.Clamp(p.y, lo, hi), p.z);
        }
    }
}
