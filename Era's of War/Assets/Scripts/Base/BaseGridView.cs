using System.Collections.Generic;
using ErasOfWar.Simulation.Base;
using UnityEngine;

namespace ErasOfWar.Unity.Base
{
    /// <summary>
    /// Draws the base from a <see cref="BaseState"/>: checkerboard ground, perimeter wall ring,
    /// buildings as colored squares with labels, the selection outline and the placement ghost.
    /// Tile (x, y) covers world [x, x+1) x [y, y+1). Display only: no rules here.
    /// </summary>
    public sealed class BaseGridView : MonoBehaviour
    {
        private const int OrderGround = 0, OrderPerimeter = 1, OrderSelection = 9, OrderBuilding = 10,
            OrderLabel = 11, OrderGhostEdge = 19, OrderGhost = 20, OrderGhostLabel = 21;
        private const float Inset = 0.06f; // gap between neighbouring buildings

        private sealed class Visual
        {
            public GameObject Root;
            public SpriteRenderer Body;
            public TextMesh Label;
            public int Level = -1;
        }

        private readonly Dictionary<int, Visual> _visuals = new Dictionary<int, Visual>();
        private SpriteRenderer _ground;
        private Texture2D _groundTex;
        private readonly List<SpriteRenderer> _perimeter = new List<SpriteRenderer>();
        private SpriteRenderer _selection;
        private Visual _ghost;
        private SpriteRenderer _ghostEdge;
        private int _drawnGridSize = -1;
        private int _drawnRevision = -1;
        private int _fadedId;

        /// <summary>Redraw whatever changed since the last call.</summary>
        public void Sync(BaseState state)
        {
            if (state.GridSize != _drawnGridSize) BuildGround(state.GridSize);
            if (state.Revision == _drawnRevision) return;
            _drawnRevision = state.Revision;

            var alive = new HashSet<int>();
            foreach (var b in state.Buildings)
            {
                alive.Add(b.Id);
                if (!_visuals.TryGetValue(b.Id, out var v))
                {
                    v = CreateVisual(b.Def, $"{b.Def.Name} #{b.Id}", OrderBuilding, OrderLabel);
                    _visuals.Add(b.Id, v);
                }
                v.Root.transform.localPosition = new Vector3(b.X, b.Y, 0);
                if (v.Level != b.Level)
                {
                    v.Level = b.Level;
                    v.Label.text = b.Def.IsWall ? "" : $"{PlaceholderArt.ShortName(b.Def)}\n{b.Level}";
                }
            }

            var dead = new List<int>();
            foreach (var kv in _visuals)
                if (!alive.Contains(kv.Key)) dead.Add(kv.Key);
            foreach (var id in dead)
            {
                Destroy(_visuals[id].Root);
                _visuals.Remove(id);
            }

            SetFaded(_fadedId);
        }

        /// <summary>Forget everything drawn (used when the whole base is replaced).</summary>
        public void Clear()
        {
            foreach (var v in _visuals.Values) Destroy(v.Root);
            _visuals.Clear();
            HideGhost();
            ShowSelection(null);
            _drawnRevision = -1;
            _drawnGridSize = -1;
            _fadedId = 0;
        }

        public void ShowSelection(PlacedBuilding b)
        {
            if (b == null)
            {
                if (_selection != null) _selection.enabled = false;
                return;
            }
            if (_selection == null) _selection = CreateQuad("Selection", OrderSelection, PlaceholderArt.Selection);
            _selection.enabled = true;
            const float pad = 0.12f;
            _selection.transform.localPosition = new Vector3(b.X - pad + Inset, b.Y - pad + Inset, 0);
            _selection.transform.localScale = new Vector3(b.Width + pad * 2 - Inset * 2, b.Height + pad * 2 - Inset * 2, 1);
        }

        /// <summary>Show a see-through copy of <paramref name="def"/> at a tile, green if it can go there, red if not.</summary>
        public void ShowGhost(BuildingDef def, int x, int y, bool valid, int level = 1)
        {
            if (_ghost == null || _ghost.Root.name != "Ghost " + def.Id)
            {
                HideGhost();
                _ghost = CreateVisual(def, "Ghost " + def.Id, OrderGhost, OrderGhostLabel);
                var c = _ghost.Body.color;
                _ghost.Body.color = new Color(c.r, c.g, c.b, 0.75f);
                _ghostEdge = CreateQuad("GhostEdge", OrderGhostEdge, Color.white);
            }

            _ghost.Root.SetActive(true);
            _ghost.Root.transform.localPosition = new Vector3(x, y, 0);
            _ghost.Label.text = def.IsWall ? "" : $"{PlaceholderArt.ShortName(def)}\n{level}";
            _ghostEdge.enabled = true;
            _ghostEdge.color = valid ? PlaceholderArt.GhostValid : PlaceholderArt.GhostInvalid;
            const float pad = 0.25f;
            _ghostEdge.transform.localPosition = new Vector3(x - pad, y - pad, 0);
            _ghostEdge.transform.localScale = new Vector3(def.Width + pad * 2, def.Height + pad * 2, 1);
        }

        public void HideGhost()
        {
            if (_ghost != null) Destroy(_ghost.Root);
            if (_ghostEdge != null) Destroy(_ghostEdge.gameObject);
            _ghost = null;
            _ghostEdge = null;
        }

        /// <summary>Draw one building faded (the original while it is being moved). 0 = none.</summary>
        public void SetFaded(int id)
        {
            _fadedId = id;
            foreach (var kv in _visuals)
            {
                var c = kv.Value.Body.color;
                kv.Value.Body.color = new Color(c.r, c.g, c.b, kv.Key == id ? 0.3f : 1f);
            }
        }

        // ------------------------------------------------------------------ construction

        private void BuildGround(int size)
        {
            _drawnGridSize = size;

            // One pixel per tile, point-filtered: a whole checkerboard in a single sprite.
            if (_groundTex != null) Destroy(_groundTex);
            _groundTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Ground" };
            var px = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                px[y * size + x] = ((x + y) & 1) == 0 ? PlaceholderArt.GroundA : PlaceholderArt.GroundB;
            _groundTex.SetPixels(px);
            _groundTex.Apply();

            if (_ground == null)
            {
                var go = new GameObject("Ground");
                go.transform.SetParent(transform, false);
                _ground = go.AddComponent<SpriteRenderer>();
                _ground.sortingOrder = OrderGround;
            }
            if (_ground.sprite != null) Destroy(_ground.sprite);
            _ground.sprite = Sprite.Create(_groundTex, new Rect(0, 0, size, size), Vector2.zero, 1f);

            // Perimeter: a fixed 1-tile ring just outside the grid (not part of the state, can't be built on).
            foreach (var p in _perimeter) Destroy(p.gameObject);
            _perimeter.Clear();
            AddPerimeter(-1, -1, size + 2, 1);   // bottom
            AddPerimeter(-1, size, size + 2, 1); // top
            AddPerimeter(-1, 0, 1, size);        // left
            AddPerimeter(size, 0, 1, size);      // right
        }

        private void AddPerimeter(float x, float y, float w, float h)
        {
            var r = CreateQuad("Perimeter", OrderPerimeter, PlaceholderArt.Perimeter);
            r.transform.localPosition = new Vector3(x, y, 0);
            r.transform.localScale = new Vector3(w, h, 1);
            _perimeter.Add(r);
        }

        private Visual CreateVisual(BuildingDef def, string name, int bodyOrder, int labelOrder)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);

            var body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(root.transform, false);
            body.sprite = PlaceholderArt.WhiteSquare;
            body.color = PlaceholderArt.ColorFor(def);
            body.sortingOrder = bodyOrder;
            body.transform.localPosition = new Vector3(Inset, Inset, 0);
            body.transform.localScale = new Vector3(def.Width - Inset * 2, def.Height - Inset * 2, 1);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(def.Width / 2f, def.Height / 2f, 0);
            var label = labelGo.AddComponent<TextMesh>();
            label.font = PlaceholderArt.Font;
            label.fontSize = 48;
            label.characterSize = Mathf.Clamp(0.045f * Mathf.Min(def.Width, def.Height), 0.04f, 0.12f);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = PlaceholderArt.TextOn(body.color);
            var mr = labelGo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = PlaceholderArt.Font.material;
            mr.sortingOrder = labelOrder;

            return new Visual { Root = root, Body = body, Label = label };
        }

        private SpriteRenderer CreateQuad(string name, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = PlaceholderArt.WhiteSquare;
            r.color = color;
            r.sortingOrder = order;
            return r;
        }
    }
}
