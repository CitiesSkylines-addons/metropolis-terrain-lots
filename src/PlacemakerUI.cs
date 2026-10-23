using System;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using UnityEngine;

namespace MetropolisTerrainLots
{
    public sealed class PlacemakerButton : UIButton
    {
        private static PlacemakerButton _instance;
        private bool _dragging;
        private Vector3 _dragMouseStart;
        private Vector3 _dragButtonStart;
        private const float DragThreshold = 6f;

        public static void EnsureCreated()
        {
            if (_instance != null) return;
            UIView view = UIView.GetAView();
            if (view != null)
            {
                _instance = view.AddUIComponent(typeof(PlacemakerButton)) as PlacemakerButton;
            }
        }

        public static void DestroyButton()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        public override void Start()
        {
            base.Start();
            name = "MetropolisPlacemakerButton";
            size = new Vector2(42f, 42f);
            normalBgSprite = "ButtonMenu";
            hoveredBgSprite = "ButtonMenuHovered";
            pressedBgSprite = "ButtonMenuPressed";
            focusedBgSprite = "ButtonMenu";
            text = "P/T";
            textScale = 0.9f;
            textColor = new Color32(140, 240, 160, 255);
            hoveredTextColor = new Color32(255, 255, 255, 255);
            textVerticalAlignment = UIVerticalAlignment.Middle;
            textHorizontalAlignment = UIHorizontalAlignment.Center;
            playAudioEvents = true;
            tooltip = "Metropolis Terrain Lots · Placemaker (Alt+P)\nParcheggi, Parchi, Piazze e Lotti Poligonali";

            UIView view = GetUIView();
            float y = view != null ? view.fixedHeight - 210f : 810f;
            relativePosition = new Vector3(64f, Mathf.Max(20f, y));
        }

        protected override void OnMouseDown(UIMouseEventParameter p)
        {
            _dragging = false;
            _dragMouseStart = Input.mousePosition;
            _dragButtonStart = relativePosition;
            base.OnMouseDown(p);
        }

        protected override void OnMouseMove(UIMouseEventParameter p)
        {
            if (p.buttons != 0)
            {
                Vector3 delta = Input.mousePosition - _dragMouseStart;
                if (!_dragging && (Mathf.Abs(delta.x) > DragThreshold || Mathf.Abs(delta.y) > DragThreshold))
                {
                    _dragging = true;
                }

                if (_dragging)
                {
                    UIView view = GetUIView();
                    float ratio = view != null ? view.ratio : 1f;
                    Vector3 newPos = _dragButtonStart;
                    newPos.x += delta.x * ratio;
                    newPos.y -= delta.y * ratio;
                    if (view != null)
                    {
                        newPos.x = Mathf.Clamp(newPos.x, 0f, view.fixedWidth - width);
                        newPos.y = Mathf.Clamp(newPos.y, 0f, view.fixedHeight - height);
                    }
                    relativePosition = newPos;
                    p.Use();
                }
            }
            base.OnMouseMove(p);
        }

        protected override void OnMouseUp(UIMouseEventParameter p)
        {
            if (_dragging)
            {
                _dragging = false;
                p.Use();
                return;
            }
            base.OnMouseUp(p);
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            if (!_dragging && (p.buttons & UIMouseButton.Left) != 0)
            {
                PlacemakerPanel.Toggle();
                p.Use();
            }
            base.OnClick(p);
        }
    }

    public sealed class PlacemakerPanel : UIPanel
    {
        private static PlacemakerPanel _instance;
        public static PlacemakerPanel Instance { get { return _instance; } }

        private UIDragHandle _dragHandle;
        private UILabel _titleLabel;
        private UIButton _langButton;
        private UIButton _closeButton;

        private UIButton _btnMode;
        private UIButton _btnPreset;
        private UIButton _btnDensity;
        private UIButton _btnBrushSize;
        private UIButton _btnSnap;
        private UIButton _btnOutgrow;
        private UIButton _btnBuilding;
        private UIButton _btnZone;

        private UIButton _btnPickMap;
        private UIButton _btnCenterStamp;
        private UIButton _btnApply;
        private UIButton _btnUndoLast;
        private UIButton _btnUndoVertex;
        private UIButton _btnClear;

        private UILabel _lblStatus;
        private UILabel _lblStats;

        private bool _isItalian = true;
        private static readonly float[] BrushSizes = { 16f, 24f, 32f, 48f, 64f };
        private int _brushIndex = 2; // 32m

        public static void Toggle()
        {
            if (_instance == null)
            {
                UIView view = UIView.GetAView();
                if (view != null)
                {
                    _instance = view.AddUIComponent(typeof(PlacemakerPanel)) as PlacemakerPanel;
                }
            }
            else
            {
                _instance.isVisible = !_instance.isVisible;
                if (_instance.isVisible)
                {
                    _instance.BringToFront();
                    _instance.RefreshUI();
                }
                else
                {
                    LotScanner.MapPickActive = false;
                }
            }
        }

        public static void DestroyPanel()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        public override void Start()
        {
            base.Start();
            DetectLanguage();

            name = "MetropolisPlacemakerPanel";
            backgroundSprite = "MenuPanel2";
            canFocus = true;
            isInteractive = true;
            width = 480f;
            height = 560f;

            UIView view = GetUIView();
            float x = 120f;
            float y = view != null ? Mathf.Max(40f, (view.fixedHeight - height) * 0.5f) : 200f;
            relativePosition = new Vector3(x, y);

            BuildHeader();
            BuildControls();
            BuildActionButtons();
            BuildStatusArea();
            RefreshUI();
        }

        private void DetectLanguage()
        {
            try
            {
                if (LocaleManager.exists && LocaleManager.instance.language != null)
                {
                    _isItalian = LocaleManager.instance.language.StartsWith("it", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                _isItalian = true;
            }
        }

        private string T(string it, string en)
        {
            return _isItalian ? it : en;
        }

        private void BuildHeader()
        {
            _dragHandle = AddUIComponent<UIDragHandle>();
            _dragHandle.target = this;
            _dragHandle.relativePosition = Vector3.zero;
            _dragHandle.size = new Vector2(width - 80f, 40f);
            _dragHandle.constrainToScreen = true;

            _titleLabel = AddUIComponent<UILabel>();
            _titleLabel.textScale = 0.92f;
            _titleLabel.textColor = new Color32(140, 240, 160, 255);
            _titleLabel.relativePosition = new Vector3(16f, 12f);

            _langButton = AddUIComponent<UIButton>();
            _langButton.size = new Vector2(38f, 24f);
            _langButton.textScale = 0.75f;
            _langButton.normalBgSprite = "ButtonMenu";
            _langButton.hoveredBgSprite = "ButtonMenuHovered";
            _langButton.pressedBgSprite = "ButtonMenuPressed";
            _langButton.relativePosition = new Vector3(width - 76f, 8f);
            _langButton.eventClick += delegate
            {
                _isItalian = !_isItalian;
                RefreshUI();
            };

            _closeButton = AddUIComponent<UIButton>();
            _closeButton.size = new Vector2(26f, 26f);
            _closeButton.text = "X";
            _closeButton.textScale = 0.85f;
            _closeButton.normalBgSprite = "buttonclose";
            _closeButton.hoveredBgSprite = "buttonclosehover";
            _closeButton.pressedBgSprite = "buttonclosepressed";
            _closeButton.relativePosition = new Vector3(width - 32f, 7f);
            _closeButton.eventClick += delegate
            {
                LotScanner.MapPickActive = false;
                isVisible = false;
            };
        }

        private UILabel _lblMode, _lblPreset, _lblDensity, _lblBrush, _lblSnap, _lblOutgrow, _lblBuilding, _lblZone;

        private void BuildControls()
        {
            float y = 48f;
            float rowH = 33f;
            float labelW = 195f;
            float btnW = 245f;

            _lblMode = CreateRowLabel(16f, y);
            _btnMode = CreateOptionButton(16f + labelW, y, btnW);
            _btnMode.eventClick += delegate
            {
                PlacemakerConfig c = LotScanner.Config;
                c.Mode = (SelectionMode)(((int)c.Mode + 1) % 4);
                LotScanner.RebuildPreviewPlan();
                RefreshUI();
            };

            y += rowH;
            _lblPreset = CreateRowLabel(16f, y);
            _btnPreset = CreateOptionButton(16f + labelW, y, btnW);
            _btnPreset.eventClick += delegate
            {
                PlacemakerConfig c = LotScanner.Config;
                c.Preset = (PlacePreset)(((int)c.Preset + 1) % 5);
                LotScanner.RebuildPreviewPlan();
                RefreshUI();
            };

            y += rowH;
            _lblDensity = CreateRowLabel(16f, y);
            _btnDensity = CreateOptionButton(16f + labelW, y, btnW);
            _btnDensity.eventClick += delegate
            {
                PlacemakerConfig c = LotScanner.Config;
                c.Density = (DensityLevel)(((int)c.Density + 1) % 3);
                LotScanner.RebuildPreviewPlan();
                RefreshUI();
            };

            y += rowH;
            _lblBrush = CreateRowLabel(16f, y);
            _btnBrushSize = CreateOptionButton(16f + labelW, y, btnW);
            _btnBrushSize.eventClick += delegate
            {
                _brushIndex = (_brushIndex + 1) % BrushSizes.Length;
                LotScanner.Config.BrushSize = BrushSizes[_brushIndex];
                LotScanner.RebuildPreviewPlan();
                RefreshUI();
            };

            y += rowH;
            _lblSnap = CreateRowLabel(16f, y);
            _btnSnap = CreateOptionButton(16f + labelW, y, btnW);
            _btnSnap.eventClick += delegate
            {
                PlacemakerConfig c = LotScanner.Config;
                c.SnapToCurb = !c.SnapToCurb;
                c.SnapRightAngles = c.SnapToCurb;
                RefreshUI();
            };

            y += rowH;
            _lblOutgrow = CreateRowLabel(16f, y);
            _btnOutgrow = CreateOptionButton(16f + labelW, y, btnW);
            _btnOutgrow.eventClick += delegate
            {
                PlacemakerConfig c = LotScanner.Config;
                c.OutgrowEnabled = !c.OutgrowEnabled;
                LotScanner.RebuildPreviewPlan();
                RefreshUI();
            };

            y += rowH;
            _lblBuilding = CreateRowLabel(16f, y);
            _btnBuilding = CreateOptionButton(16f + labelW, y, btnW);
            _btnBuilding.eventClick += delegate
            {
                PlacemakerConfig c = LotScanner.Config;
                c.IncludeRoadsideBuilding = !c.IncludeRoadsideBuilding;
                LotScanner.RebuildPreviewPlan();
                RefreshUI();
            };

            y += rowH;
            _lblZone = CreateRowLabel(16f, y);
            _btnZone = CreateOptionButton(16f + labelW, y, btnW);
            _btnZone.eventClick += delegate
            {
                PlacemakerConfig c = LotScanner.Config;
                c.Zone = (ZoneChoice)(((int)c.Zone + 1) % 5);
                LotScanner.RebuildPreviewPlan();
                RefreshUI();
            };
        }

        private void BuildActionButtons()
        {
            float y = 320f;
            float wHalf = 218f;

            _btnPickMap = CreateOptionButton(16f, y, wHalf);
            _btnPickMap.height = 32f;
            _btnPickMap.eventClick += delegate
            {
                LotScanner.MapPickActive = !LotScanner.MapPickActive;
                RefreshUI();
            };

            _btnCenterStamp = CreateOptionButton(244f, y, wHalf);
            _btnCenterStamp.height = 32f;
            _btnCenterStamp.eventClick += delegate
            {
                LotScanner.StampOrVertexAtScreenCenter();
                RefreshUI();
            };

            y += 38f;
            _btnApply = CreateOptionButton(16f, y, wHalf);
            _btnApply.height = 34f;
            _btnApply.eventClick += delegate { TriggerApply(); };

            _btnUndoLast = CreateOptionButton(244f, y, wHalf);
            _btnUndoLast.height = 34f;
            _btnUndoLast.eventClick += delegate
            {
                PlacemakerExecutor.UndoLast(removed =>
                {
                    if (_lblStatus != null)
                    {
                        _lblStatus.text = T("Annullato ultimo piazzamento (" + removed + " elementi rimossi).",
                                            "Undid last placement (" + removed + " items removed).");
                    }
                    RefreshUI();
                });
            };

            y += 40f;
            _btnUndoVertex = CreateOptionButton(16f, y, wHalf);
            _btnUndoVertex.height = 28f;
            _btnUndoVertex.eventClick += delegate
            {
                LotScanner.UndoVertex();
                RefreshUI();
            };

            _btnClear = CreateOptionButton(244f, y, wHalf);
            _btnClear.height = 28f;
            _btnClear.eventClick += delegate
            {
                LotScanner.Clear();
                RefreshUI();
            };
        }

        private void BuildStatusArea()
        {
            _lblStatus = AddUIComponent<UILabel>();
            _lblStatus.textScale = 0.82f;
            _lblStatus.textColor = new Color32(130, 220, 255, 255);
            _lblStatus.relativePosition = new Vector3(16f, 436f);
            _lblStatus.autoSize = false;
            _lblStatus.size = new Vector2(width - 32f, 22f);

            _lblStats = AddUIComponent<UILabel>();
            _lblStats.textScale = 0.78f;
            _lblStats.textColor = new Color32(230, 230, 230, 255);
            _lblStats.relativePosition = new Vector3(16f, 460f);
            _lblStats.autoSize = false;
            _lblStats.size = new Vector2(width - 32f, 92f);
            _lblStats.wordWrap = true;
        }

        public void TriggerApply()
        {
            PlacemakerPlan plan = LotScanner.CurrentPlan;
            if (plan == null)
            {
                if (_lblStatus != null)
                {
                    _lblStatus.text = T("Disegna o seleziona prima un'area valida (min 3 vertici).",
                                        "Draw or select a valid area first (min 3 vertices).");
                }
                return;
            }

            if (_lblStatus != null)
            {
                _lblStatus.text = T("Costruzione in corso nella città...", "Building layout in city...");
            }

            LotScanner.Config.Seed = (LotScanner.Config.Seed * 31 + 7) & 0x7FFFFF;
            PlacemakerExecutor.ExecuteOnSimulationThread(plan, LotScanner.Config, (rec, err) =>
            {
                if (err != null)
                {
                    if (_lblStatus != null) _lblStatus.text = T("Errore: ", "Error: ") + err;
                }
                else
                {
                    int total = rec != null ? rec.TotalItems : 0;
                    if (_lblStatus != null)
                    {
                        _lblStatus.text = T("Area costruita! Elementi creati: " + total,
                                            "Layout built! Created items: " + total);
                    }
                    LotScanner.Clear();
                }
                RefreshUI();
            });
        }

        public void RefreshUI()
        {
            if (_titleLabel == null) return;
            PlacemakerConfig c = LotScanner.Config;

            _titleLabel.text = T("Metropolis Terrain Lots · Placemaker", "Metropolis Terrain Lots · Placemaker");
            _langButton.text = _isItalian ? "IT" : "EN";

            _lblMode.text = T("Strumento Selezione:", "Selection Tool:");
            _lblPreset.text = T("Tipo Area (Preset):", "Place Preset:");
            _lblDensity.text = T("Densità Dettagli:", "Detail Density:");
            _lblBrush.text = T("Dimensione Pennello:", "Brush / Block Size:");
            _lblSnap.text = T("Snap Cordolo & 90°:", "Curb & 90° Snap:");
            _lblOutgrow.text = T("Outgrow (Bordi Organici):", "Outgrow (Organic Edges):");
            _lblBuilding.text = T("Edificio Fronte Strada:", "Roadside Asset:");
            _lblZone.text = T("Tipo Zona (Outgrow):", "Zone Type (Outgrow):");

            string[] modeNamesIt = { "Vertici (Poligono)", "Secchiello (Isolato)", "Pennello Rettangolo", "Pennello Cerchio" };
            string[] modeNamesEn = { "Points (Polygon)", "Paint Bucket (Block)", "Rectangle Brush", "Circle Brush" };
            _btnMode.text = "< " + (_isItalian ? modeNamesIt[(int)c.Mode] : modeNamesEn[(int)c.Mode]) + " >";

            string[] presetNamesIt = { "Parcheggio (Parking Lot)", "Parco Urbano (City Park)", "Giardino & Natura", "Piazza Pedonale (Plaza)", "Zona + Cortile (Outgrow)" };
            string[] presetNamesEn = { "Parking Lot", "City Park & Playground", "Garden & Nature Trail", "Urban Plaza & Square", "Zone + Courtyard Outgrow" };
            _btnPreset.text = "< " + (_isItalian ? presetNamesIt[(int)c.Preset] : presetNamesEn[(int)c.Preset]) + " >";

            string[] densIt = { "Rada (Sparse)", "Bilanciata (Balanced)", "Folta (Lush)" };
            string[] densEn = { "Sparse", "Balanced", "Lush" };
            _btnDensity.text = "< " + (_isItalian ? densIt[(int)c.Density] : densEn[(int)c.Density]) + " >";

            _btnBrushSize.text = "< " + c.BrushSize.ToString("F0") + " x " + c.BrushSize.ToString("F0") + " m >";
            _btnSnap.text = c.SnapToCurb ? T("ATTIVO (Curb + 90°)", "ON (Curb + 90°)") : T("DISATTIVO", "OFF");
            _btnOutgrow.text = c.OutgrowEnabled ? T("ATTIVO (Siepi/Alberi)", "ON (Border Blend)") : T("DISATTIVO", "OFF");
            _btnBuilding.text = c.IncludeRoadsideBuilding ? T("ATTIVO (Automatico)", "ON (Auto Fit)") : T("SOLO PROP/ SENTIERI", "PATHS & PROPS ONLY");

            string[] zoneIt = { "Residenziale Bassa", "Residenziale Alta", "Commerciale Bassa", "Commerciale Alta", "Uffici" };
            string[] zoneEn = { "Low Residential", "High Residential", "Low Commercial", "High Commercial", "Office" };
            _btnZone.text = "< " + (_isItalian ? zoneIt[(int)c.Zone] : zoneEn[(int)c.Zone]) + " >";

            _btnPickMap.text = LotScanner.MapPickActive
                ? T("● Clicca Mappa: ATTIVO (Esc)", "● Map Click: ACTIVE (Esc)")
                : T("Seleziona su Mappa", "Pick on Map");
            _btnPickMap.textColor = LotScanner.MapPickActive ? new Color32(120, 255, 140, 255) : new Color32(255, 255, 255, 255);

            _btnCenterStamp.text = c.Mode == SelectionMode.PolygonPoints
                ? T("+ Vertice Centro Vista", "+ Vertex at View Center")
                : T("Crea Area al Centro Vista", "Stamp Area at View Center");

            bool hasValidPlan = LotScanner.CurrentPlan != null;
            _btnApply.text = T("Costruisci Area (Invio)", "Apply Layout (Enter)");
            _btnApply.isEnabled = hasValidPlan;
            _btnApply.textColor = hasValidPlan ? new Color32(120, 255, 150, 255) : new Color32(130, 130, 130, 255);

            bool canUndo = PlacemakerExecutor.HistoryCount > 0;
            _btnUndoLast.text = T("Annulla Ultimo (" + PlacemakerExecutor.HistoryCount + ")", "Undo Last (" + PlacemakerExecutor.HistoryCount + ")");
            _btnUndoLast.isEnabled = canUndo;
            _btnUndoLast.textColor = canUndo ? new Color32(255, 150, 130, 255) : new Color32(130, 130, 130, 255);

            _btnUndoVertex.text = T("Annulla Vertice (Tasto DX)", "Undo Vertex (Right-Click)");
            _btnClear.text = T("Pulisci Bozza", "Clear Sketch");

            PlacemakerPlan plan = LotScanner.CurrentPlan;
            if (plan != null)
            {
                if (string.IsNullOrEmpty(_lblStatus.text) || _lblStatus.text.StartsWith("Disegna") || _lblStatus.text.StartsWith("Draw") || _lblStatus.text.StartsWith("Anteprima") || _lblStatus.text.StartsWith("Live"))
                {
                    _lblStatus.text = T("Anteprima pronta sulla mappa! Premi 'Costruisci Area' o Invio.",
                                        "Live preview ready on map! Click 'Apply Layout' or press Enter.");
                }
                _lblStats.text = T(
                    string.Format("Vertici: {0}  |  Area: {1:F0} m²  |  Pendenza: {2}  |  Fronte strada: {3}\nPosti auto stimati: {4}  |  Sentieri: {5}  |  Alberi/Siepi: {6}  |  Prop/Luci: {7}  |  Celle Zona: {8}",
                        plan.Polygon.Length, plan.AreaSqm, plan.Slope, plan.HasRoadFrontage ? "Sì" : "Libero",
                        plan.EstimatedParkingSpaces, plan.Paths.Count, plan.Trees.Count, plan.Props.Count, plan.ZoneCells.Count),
                    string.Format("Vertices: {0}  |  Area: {1:F0} m²  |  Slope: {2}  |  Road Frontage: {3}\nEst. Parking Stalls: {4}  |  Paths: {5}  |  Trees/Bushes: {6}  |  Props/Lamps: {7}  |  Zone Cells: {8}",
                        plan.Polygon.Length, plan.AreaSqm, plan.Slope, plan.HasRoadFrontage ? "Yes" : "Freeform",
                        plan.EstimatedParkingSpaces, plan.Paths.Count, plan.Trees.Count, plan.Props.Count, plan.ZoneCells.Count));
            }
            else
            {
                _lblStats.text = T(
                    string.Format("Vertici attuali: {0}. Attiva 'Seleziona su Mappa' e clicca col tasto sinistro sul terreno (o usa il Secchiello dentro un isolato stradale).",
                        LotScanner.VertexCount),
                    string.Format("Current vertices: {0}. Enable 'Pick on Map' and left-click on terrain (or use Paint Bucket inside a road block).",
                        LotScanner.VertexCount));
            }
        }

        private UILabel CreateRowLabel(float x, float y)
        {
            UILabel lbl = AddUIComponent<UILabel>();
            lbl.textScale = 0.82f;
            lbl.textColor = new Color32(235, 235, 235, 255);
            lbl.relativePosition = new Vector3(x, y + 5f);
            return lbl;
        }

        private UIButton CreateOptionButton(float x, float y, float w)
        {
            UIButton btn = AddUIComponent<UIButton>();
            btn.size = new Vector2(w, 26f);
            btn.relativePosition = new Vector3(x, y);
            btn.textScale = 0.8f;
            btn.normalBgSprite = "ButtonMenu";
            btn.hoveredBgSprite = "ButtonMenuHovered";
            btn.pressedBgSprite = "ButtonMenuPressed";
            return btn;
        }
    }
}
