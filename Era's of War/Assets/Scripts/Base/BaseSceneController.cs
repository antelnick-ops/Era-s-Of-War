using System.Collections.Generic;
using ErasOfWar.Simulation.Base;
using ErasOfWar.Simulation.Data;
using ErasOfWar.Unity.Boot;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ErasOfWar.Unity.Base
{
    /// <summary>
    /// Runs the base screen: turns touches into requests on <see cref="BaseState"/> (the Simulation
    /// library decides what is allowed), redraws, and saves after every change.
    /// Put this on one GameObject in the Base scene; it creates the view and HUD itself.
    /// </summary>
    public sealed class BaseSceneController : MonoBehaviour
    {
        private enum Mode { Idle, Selected, Placing, Moving, Walls }
        private enum Gesture { None, OverUi, Pending, Pan, GhostDrag, WallPaint, Pinch }

        private BuildingCatalog _catalog;
        private BaseState _state;
        private BaseGridView _view;
        private BaseCameraRig _rig;
        private BaseHud _hud;

        private Mode _mode = Mode.Idle;
        private int _selectedId;
        private BuildingDef _ghostDef;
        private Vector2Int _ghostPos;
        private int _fittedGrid = -1;
        private int _hudRevision = -1;
        private int _hudCc = -1;

        private Gesture _gesture;
        private Vector2 _pressPos, _lastPos;
        private Vector2Int _dragOffset, _lastPaintTile;
        private float _lastPinchDistance;
        private string _lastPaintError;

        // ------------------------------------------------------------------ lifecycle

        private void Start()
        {
            Application.targetFrameRate = 60;

            var data = GameDataBootstrap.Data;
            if (data == null)
            {
                Debug.LogError("[Eras of War] Game data not loaded; base screen disabled. See earlier errors.");
                enabled = false;
                return;
            }

            try
            {
                _catalog = BuildingCatalog.FromGameData(data);
            }
            catch (GameDataException e)
            {
                Debug.LogError("[Eras of War] Building data is invalid: " + e.Message);
                enabled = false;
                return;
            }
            foreach (var w in _catalog.Warnings) Debug.LogWarning("[Eras of War] Data: " + w);

            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                cam.tag = "MainCamera";
            }
            // Explicit check: Unity's "fake null" from GetComponent doesn't work with ??.
            _rig = cam.GetComponent<BaseCameraRig>();
            if (_rig == null) _rig = cam.gameObject.AddComponent<BaseCameraRig>();

            _view = new GameObject("Base View").AddComponent<BaseGridView>();
            _hud = gameObject.AddComponent<BaseHud>();
            _hud.Build(_catalog);
            _hud.BuildItemTapped += OnBuildItem;
            _hud.ActionTapped += OnAction;

            var loaded = BaseSaveStore.Load(_catalog);
            _state = loaded?.State ?? new BaseState(_catalog);
            if (loaded != null && !loaded.Ok)
                _hud.Toast("Save file could not be read. Starting a new base.", true);
            else if (loaded != null && loaded.Dropped.Count > 0)
                _hud.Toast($"{loaded.Dropped.Count} saved building(s) could not be restored.", true);

            Debug.Log($"[Eras of War] Base ready: {_state.Buildings.Count} buildings, CC {_state.CcLevel}, " +
                      $"grid {_state.GridSize}x{_state.GridSize}. Save file: {BaseSaveStore.SavePath}" +
                      (loaded == null ? " (none yet)" : " (loaded)"));
        }

        private void Update()
        {
            HandleInput();

            if (_state.GridSize != _fittedGrid)
            {
                _fittedGrid = _state.GridSize;
                _rig.Fit(_fittedGrid);
            }

            _view.Sync(_state);
            UpdateGhostAndSelection();
            UpdateHud();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();

        private void Save()
        {
            if (_state != null) BaseSaveStore.Save(_state);
        }

        // ------------------------------------------------------------------ HUD commands

        private void OnBuildItem(BuildingDef def)
        {
            if (_mode == Mode.Placing || _mode == Mode.Moving || _mode == Mode.Walls) SetMode(Mode.Idle);

            // Locked / limit errors come before any footprint check, so an off-grid probe tells us.
            var probe = _state.CanPlace(def.Id, -1000, -1000);
            if (probe.Error == BaseError.LockedByCc || probe.Error == BaseError.CountLimitReached ||
                probe.Error == BaseError.UnknownType)
            {
                _hud.Toast(probe.Message, true);
                return;
            }

            if (def.IsWall)
            {
                _lastPaintError = null;
                SetMode(Mode.Walls);
                return;
            }

            _ghostDef = def;
            _ghostPos = FindSpotNear(def, _rig.CenterTile());
            SetMode(Mode.Placing);
        }

        private void OnAction(string act)
        {
            var selected = _state.Get(_selectedId);
            switch (act)
            {
                case BaseHud.ActPlace when _mode == Mode.Placing:
                {
                    var r = _state.Place(_ghostDef.Id, _ghostPos.x, _ghostPos.y);
                    Report(r);
                    if (r.Ok)
                    {
                        Save();
                        Select(r.BuildingId);
                    }
                    break;
                }
                case BaseHud.ActPlace when _mode == Mode.Moving && selected != null:
                {
                    var r = _state.Move(selected.Id, _ghostPos.x, _ghostPos.y);
                    Report(r);
                    if (r.Ok)
                    {
                        Save();
                        Select(selected.Id);
                    }
                    break;
                }
                case BaseHud.ActCancel:
                    if (_mode == Mode.Moving && selected != null) Select(selected.Id);
                    else SetMode(Mode.Idle);
                    break;
                case BaseHud.ActDone:
                    SetMode(Mode.Idle);
                    break;
                case BaseHud.ActMove when selected != null:
                    _ghostDef = selected.Def;
                    _ghostPos = new Vector2Int(selected.X, selected.Y);
                    SetMode(Mode.Moving);
                    break;
                case BaseHud.ActUpgrade when selected != null:
                {
                    var r = _state.Upgrade(selected.Id);
                    Report(r);
                    if (r.Ok) Save();
                    break;
                }
                case BaseHud.ActRemove when selected != null:
                {
                    var r = _state.Remove(selected.Id);
                    Report(r);
                    if (r.Ok)
                    {
                        Save();
                        SetMode(Mode.Idle);
                    }
                    break;
                }
                case BaseHud.ActClose:
                    SetMode(Mode.Idle);
                    break;
                case BaseHud.ActReset:
                    BaseSaveStore.Delete();
                    _state = new BaseState(_catalog);
                    _view.Clear();
                    SetMode(Mode.Idle);
                    _hud.Toast("Base cleared.");
                    break;
            }
        }

        private void Report(BaseResult r) => _hud.Toast(r.Message, !r.Ok);

        private void Select(int id)
        {
            _selectedId = id;
            SetMode(Mode.Selected);
        }

        private void SetMode(Mode mode)
        {
            _mode = mode;
            if (mode != Mode.Placing && mode != Mode.Moving)
            {
                _ghostDef = null;
                _view.HideGhost();
            }
            if (mode == Mode.Idle) _selectedId = 0;
            _view.SetFaded(mode == Mode.Moving ? _selectedId : 0);
            _gesture = Gesture.None;
        }

        // ------------------------------------------------------------------ input

        private float DragThreshold => Screen.dpi > 0 ? Screen.dpi * 0.08f : 12f;

        private void HandleInput()
        {
            // Mouse wheel zoom (editor / desktop)
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var scroll = mouse.scroll.ReadValue().y;
                var mp = mouse.position.ReadValue();
                if (Mathf.Abs(scroll) > 0.01f && !_hud.IsOverUi(mp))
                    _rig.Zoom(scroll > 0 ? 1.12f : 1f / 1.12f, mp);
            }

            // Two-finger pinch zoom (phone)
            var ts = Touchscreen.current;
            if (ts != null)
            {
                int n = 0;
                Vector2 a = default, b = default;
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue;
                    if (n == 0) a = t.position.ReadValue();
                    else if (n == 1) b = t.position.ReadValue();
                    n++;
                }
                if (n >= 2)
                {
                    var d = Vector2.Distance(a, b);
                    if (_gesture != Gesture.Pinch)
                    {
                        _gesture = Gesture.Pinch;
                        _lastPinchDistance = d;
                    }
                    else if (_lastPinchDistance > 1f && d > 1f)
                    {
                        _rig.Zoom(d / _lastPinchDistance, (a + b) / 2f);
                        _lastPinchDistance = d;
                    }
                    return;
                }
            }

            // One pointer: mouse or first finger
            var pointer = Pointer.current;
            if (pointer == null) return;
            var pos = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame) BeginGesture(pos);
            else if (pointer.press.isPressed && _gesture != Gesture.None) ContinueGesture(pos);

            if (pointer.press.wasReleasedThisFrame) EndGesture(pos);
            else if (!pointer.press.isPressed && _gesture != Gesture.None) _gesture = Gesture.None;
        }

        private void BeginGesture(Vector2 pos)
        {
            if (_hud.IsOverUi(pos))
            {
                _gesture = Gesture.OverUi;
                return;
            }

            _pressPos = _lastPos = pos;
            var tile = _rig.ScreenToTile(pos);

            if ((_mode == Mode.Placing || _mode == Mode.Moving) && _ghostDef != null && NearGhost(tile))
            {
                _gesture = Gesture.GhostDrag;
                _dragOffset = tile - _ghostPos;
            }
            else if (_mode == Mode.Walls)
            {
                _gesture = Gesture.WallPaint;
                _lastPaintTile = tile;
                PaintWall(tile);
            }
            else
            {
                _gesture = Gesture.Pending;
            }
        }

        private void ContinueGesture(Vector2 pos)
        {
            switch (_gesture)
            {
                case Gesture.Pending:
                    if ((pos - _pressPos).magnitude > DragThreshold)
                    {
                        _gesture = Gesture.Pan;
                        _rig.Pan(pos - _lastPos);
                    }
                    break;
                case Gesture.Pan:
                    _rig.Pan(pos - _lastPos);
                    break;
                case Gesture.GhostDrag:
                    _ghostPos = ClampToGrid(_ghostDef, _rig.ScreenToTile(pos) - _dragOffset);
                    break;
                case Gesture.WallPaint:
                {
                    var tile = _rig.ScreenToTile(pos);
                    if (tile != _lastPaintTile)
                    {
                        foreach (var t in Line(_lastPaintTile, tile)) PaintWall(t);
                        _lastPaintTile = tile;
                    }
                    break;
                }
            }
            _lastPos = pos;
        }

        private void EndGesture(Vector2 pos)
        {
            if (_gesture == Gesture.Pending) Tap(_rig.ScreenToTile(pos));
            _gesture = Gesture.None;
        }

        private void Tap(Vector2Int tile)
        {
            switch (_mode)
            {
                case Mode.Idle:
                case Mode.Selected:
                    var b = _state.BuildingAt(tile.x, tile.y);
                    if (b != null) Select(b.Id);
                    else SetMode(Mode.Idle);
                    break;
                case Mode.Placing:
                case Mode.Moving:
                    // Tap elsewhere: jump the ghost there (centred on the finger).
                    _ghostPos = ClampToGrid(_ghostDef,
                        tile - new Vector2Int((_ghostDef.Width - 1) / 2, (_ghostDef.Height - 1) / 2));
                    break;
            }
        }

        private void PaintWall(Vector2Int tile)
        {
            if (!_state.InGrid(tile.x, tile.y) || _state.BuildingAt(tile.x, tile.y) != null) return;
            var r = _state.Place(_catalog.WallTile.Id, tile.x, tile.y);
            if (r.Ok)
            {
                Save();
            }
            else if (r.Message != _lastPaintError)
            {
                _lastPaintError = r.Message; // don't repeat the same toast for every tile of a drag
                _hud.Toast(r.Message, true);
            }
        }

        private bool NearGhost(Vector2Int tile) =>
            tile.x >= _ghostPos.x - 1 && tile.x <= _ghostPos.x + _ghostDef.Width &&
            tile.y >= _ghostPos.y - 1 && tile.y <= _ghostPos.y + _ghostDef.Height;

        // ------------------------------------------------------------------ drawing / HUD

        private void UpdateGhostAndSelection()
        {
            if ((_mode == Mode.Placing || _mode == Mode.Moving) && _ghostDef != null)
            {
                var valid = _mode == Mode.Placing
                    ? _state.CanPlace(_ghostDef.Id, _ghostPos.x, _ghostPos.y).Ok
                    : _state.CanMove(_selectedId, _ghostPos.x, _ghostPos.y).Ok;
                var level = _mode == Mode.Moving ? _state.Get(_selectedId)?.Level ?? 1 : 1;
                _view.ShowGhost(_ghostDef, _ghostPos.x, _ghostPos.y, valid, level);
            }

            _view.ShowSelection(_mode == Mode.Selected ? _state.Get(_selectedId) : null);
        }

        private void UpdateHud()
        {
            var cc = _state.CcLevel;
            var wallId = _catalog.WallTile.Id;
            var walls = _state.CountOf(wallId);
            var wallLimit = _state.MaxCountOf(wallId);

            if (_state.Revision != _hudRevision || cc != _hudCc)
            {
                _hudRevision = _state.Revision;
                _hudCc = cc;
                _hud.RefreshBuildMenu(_state);
                _hud.SetInfo($"Command Center Lv {cc}    Grid {_state.GridSize}x{_state.GridSize}    Walls {walls}/{wallLimit}");
            }

            var selected = _state.Get(_selectedId);
            if (_mode == Mode.Selected && selected == null) SetMode(Mode.Idle);

            switch (_mode)
            {
                case Mode.Idle:
                    _hud.SetContext(_state.CommandCenter == null
                        ? "Start by placing a Command Center from the menu below."
                        : "Tap a building to select it. Drag to pan. Pinch or scroll to zoom.");
                    break;
                case Mode.Selected:
                    var title = $"{selected.Def.Name}    Lv {selected.Level}/{selected.Def.MaxLevel}";
                    if (selected.Def.IsCommandCenter)
                        _hud.SetContext(title,
                            (BaseHud.ActMove, "Move"), (BaseHud.ActUpgrade, "Upgrade"), (BaseHud.ActClose, "Close"));
                    else
                        _hud.SetContext(title,
                            (BaseHud.ActMove, "Move"), (BaseHud.ActUpgrade, "Upgrade"),
                            ("!" + BaseHud.ActRemove, "Remove"), (BaseHud.ActClose, "Close"));
                    break;
                case Mode.Placing:
                    _hud.SetContext($"Place {_ghostDef.Name}: drag it, then tap Place",
                        (BaseHud.ActPlace, "Place"), (BaseHud.ActCancel, "Cancel"));
                    break;
                case Mode.Moving:
                    _hud.SetContext($"Move {_ghostDef.Name}: drag it, then tap Place",
                        (BaseHud.ActPlace, "Place"), (BaseHud.ActCancel, "Cancel"));
                    break;
                case Mode.Walls:
                    _hud.SetContext($"Walls: tap or drag on empty tiles ({walls}/{wallLimit})",
                        (BaseHud.ActDone, "Done"));
                    break;
            }
        }

        // ------------------------------------------------------------------ helpers

        private Vector2Int ClampToGrid(BuildingDef def, Vector2Int p)
        {
            var size = _state.GridSize;
            return new Vector2Int(
                Mathf.Clamp(p.x, 0, Mathf.Max(0, size - def.Width)),
                Mathf.Clamp(p.y, 0, Mathf.Max(0, size - def.Height)));
        }

        /// <summary>Closest valid spot to <paramref name="center"/>, or the clamped centre if the base is full.</summary>
        private Vector2Int FindSpotNear(BuildingDef def, Vector2Int center)
        {
            var origin = ClampToGrid(def, center - new Vector2Int(def.Width / 2, def.Height / 2));
            var size = _state.GridSize;
            var best = origin;
            var bestDist = int.MaxValue;
            for (var y = 0; y <= size - def.Height; y++)
            for (var x = 0; x <= size - def.Width; x++)
            {
                var d = (x - origin.x) * (x - origin.x) + (y - origin.y) * (y - origin.y);
                if (d >= bestDist) continue;
                if (!_state.CanPlace(def.Id, x, y).Ok) continue;
                best = new Vector2Int(x, y);
                bestDist = d;
            }
            return best;
        }

        /// <summary>Tiles on a straight line from a (exclusive) to b (inclusive), so fast drags leave no gaps.</summary>
        private static IEnumerable<Vector2Int> Line(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(b.x - a.x), dy = -Mathf.Abs(b.y - a.y);
            int sx = a.x < b.x ? 1 : -1, sy = a.y < b.y ? 1 : -1;
            int err = dx + dy, x = a.x, y = a.y;
            while (x != b.x || y != b.y)
            {
                var e2 = 2 * err;
                if (e2 >= dy) { err += dy; x += sx; }
                if (e2 <= dx) { err += dx; y += sy; }
                yield return new Vector2Int(x, y);
            }
        }
    }
}
