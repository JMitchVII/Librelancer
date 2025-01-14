using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ImGuiNET;
using LibreLancer;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.ImUI;
using LibreLancer.Infocards;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.World;
using ModelRenderer = LibreLancer.Render.ModelRenderer;

namespace LancerEdit;

public class SystemEditorTab : GameContentTab
{
    //public fields
    public SystemEditData SystemData;
    public GameWorld World;
    public bool ObjectsDirty = false;
    public StarSystem CurrentSystem;
    public List<SystemObject> DeletedObjects = new();
    public GameDataContext Data;

    private MainWindow win;
    private SystemRenderer renderer;
    private Viewport3D viewport;
    private LookAtCamera camera;
    private SystemMap systemMap = new SystemMap();
    private SystemObjectList objectList;
    public ZoneList ZoneList;

    private bool mapOpen = false;
    private bool infocardOpen = false;

    Infocard systemInfocard;
    private InfocardControl icard;

    private PopupManager popups = new PopupManager();

    public SystemEditorTab(GameDataContext gameData, MainWindow mw, StarSystem system)
    {
        Title = "System Editor";
        SaveStrategy = new StarSystemSaveStrategy(this);
        this.Data = gameData;
        viewport = new Viewport3D(mw);
        viewport.EnableMSAA = false; //MSAA handled by SystemRenderer
        viewport.DefaultOffset = new Vector3(0, 0, 4);
        viewport.ModelScale = 1000f;
        viewport.Mode = CameraModes.Walkthrough;
        viewport.Background = new Vector4(0.12f, 0.12f, 0.12f, 1f);
        viewport.ResetControls();
        viewport.DoubleClicked += ViewportOnDoubleClicked;
        camera = new LookAtCamera()
        {
            GameFOV = true,
            ZRange = new Vector2(3f, 10000000f)
        };

        //Extract nav_prettymap texture
        string navPrettyMap;
        if ((navPrettyMap = gameData.GameData.VFS.Resolve(
                gameData.GameData.Ini.Freelancer.DataPath + "INTERFACE/NEURONET/NAVMAP/NEWNAVMAP/nav_prettymap.3db",
                false)) !=
            null)
        {
            gameData.Resources.LoadResourceFile(navPrettyMap);
        }

        systemMap.CreateContext(gameData, mw);
        this.win = mw;
        objectList = new SystemObjectList(mw);
        objectList.OnMoveCamera += MoveCameraTo;
        objectList.OnDelete += DeleteObject;

        LoadSystem(system);
    }

    private void ViewportOnDoubleClicked(Vector2 pos)
    {
        if (openTabs[1])
        {
            var sel = World.GetSelection(camera, null, pos.X, pos.Y, viewport.RenderWidth, viewport.RenderHeight);
            if (ShouldAddSecondary())
            {
                if (sel != null && !objectList.Selection.Contains(sel))
                    objectList.Selection.Add(sel);
            }
            else
            {
                objectList.SelectSingle(sel);
                objectList.ScrollToSelection();
            }
        }
    }

    void LoadSystem(StarSystem system)
    {
        objectList.SelectSingle(null);
        if (World != null)
        {
            World.Renderer.Dispose();
            World.Dispose();
        }
        //Load system
        renderer = new SystemRenderer(camera, Data.Resources, win);
        World = new GameWorld(renderer, null);
        CurrentSystem = system;
        systemInfocard = Data.GameData.GetInfocard(CurrentSystem.IdsInfo, Data.Fonts);
        if (icard != null) icard.SetInfocard(systemInfocard);
        Data.GameData.LoadAllSystem(CurrentSystem);
        World.LoadSystem(CurrentSystem, Data.Resources, false, false);
        World.Renderer.LoadLights(CurrentSystem);
        World.Renderer.LoadStarspheres(CurrentSystem);
        systemMap.SetObjects(CurrentSystem);
        renderer.PhysicsHook = RenderZones;
        renderer.GridHook = RenderGrid;
        SystemData = new SystemEditData(CurrentSystem);
        //Setup UI
        ZoneList = new ZoneList();
        ZoneList.SetZones(CurrentSystem.Zones, CurrentSystem.AsteroidFields, CurrentSystem.Nebulae);
        World.Renderer.LoadZones(ZoneList.AsteroidFields, ZoneList.Nebulae);
        objectList.SetObjects(World);
    }

    private bool renderGrid = false;

    private void RenderGrid()
    {
        if (!renderGrid) return;
        var cpos = camera.Position;
        var y = Math.Abs(cpos.Y);
        if (y <= 100) y = 100;

        GridRender.Draw(win.RenderContext, GridRender.DistanceScale(y), win.Config.GridColor, camera.ZRange.X,
            camera.ZRange.Y);
    }

    private bool showZones = false;
    private string hoveredZone = null;

    void ZonesPanel()
    {
        PanelWithProperties("##zones", () =>
        {
            if (ImGui.Button("New Zone"))
            {
                popups.OpenPopup(new NicknameInputPopup("New Zone", "", ZoneList.ZoneExists, n =>
                {
                    var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                              Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
                    var dir = Vector3.Transform(-Vector3.UnitZ, rot);
                    var to = viewport.CameraOffset + (dir * 50);
                    var z = new Zone() {Nickname = n, Position = to};
                    z.Shape = new ZoneSphere(z, 50);
                    ZoneList.Selected = ZoneList.AddZone(z);
                }));
            }
            ImGui.SameLine();
            if (ImGui.Button("Show All"))
                ZoneList.ShowAll();
            ImGui.SameLine();
            if (ImGui.Button("Hide All"))
                ZoneList.HideAll();
            ZoneList.Draw();
        }, ZoneProperties);
    }

    void ZoneProperties()
    {
        if (ZoneList.Selected == null)
        {
            ImGui.Text("No Zone Selected");
            return;
        }

        var sel = ZoneList.Selected.Current;
        var ez = ZoneList.Selected;

        if (ImGuiExt.Button("Reset", ez.Dirty))
        {
            ez.Reset();
            ZoneList.CheckDirty();
        }

        Controls.BeginPropertyTable("properties", true, false, true);
        Controls.PropertyRow("Nickname", sel.Nickname);
        if (ImGui.Button($"{Icons.Edit}##nickname"))
        {
            popups.OpenPopup(new NicknameInputPopup("Rename", sel.Nickname, x => ZoneList.HasZone(x), x =>
            {
                sel.Nickname = x;
                ZoneList.SetZonesDirty(ez);
            }));
        }

        Controls.PropertyRow("Name", Data.Infocards.GetStringResource(sel.IdsName));
        if (ImGui.Button($"{Icons.Edit}##name"))
        {
            popups.OpenPopup(IdsSearch.SearchStrings(Data.Infocards, Data.Fonts,
                newIds =>
                {
                    sel.IdsName = newIds;
                    ZoneList.SetZonesDirty(ez);
                }));
        }

        //Position
        Controls.PropertyRow("Position", $"{sel.Position.X:0.00}, {sel.Position.Y:0.00}, {sel.Position.Z: 0.00}");
        if (ImGui.Button($"{Icons.Edit}##position"))
        {
            popups.OpenPopup(new Vector3Popup("Position", sel.Position, (v, d) =>
            {
                sel.Position = v;
                if (d) ZoneList.SetZonesDirty(ez);
            }));
        }
        var rot = sel.RotationMatrix.GetEulerDegrees();
        Controls.PropertyRow("Rotation",
            $"{rot.X: 0.00}, {rot.Y:0.00}, {rot.Z: 0.00}");
        if (ImGui.Button("0##rot"))
        {
            sel.RotationAngles = Vector3.Zero;
            sel.RotationMatrix = Matrix4x4.Identity;
            sel.Shape.Update();
            ZoneList.SetZonesDirty(ez);
        }

        if (ShapeProperties(ref sel.Shape, ez)) ZoneList.SetZonesDirty(ez);
        Controls.EndPropertyTable();

        //Comment
        Controls.BeginPropertyTable("comment", true, false, true);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Comment");
        ImGui.TableNextColumn();
        Controls.TruncText(sel.Comment, 20);
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##comment"))
            popups.OpenPopup(new CommentPopup(sel.Comment, x =>
            {
                sel.Comment = x;
                ZoneList.SetZonesDirty(ez);
            }));
        Controls.EndPropertyTable();
    }

    bool ShapeChangeButton<T>(ref ZoneShape sh, Zone z) where T : ZoneShape
    {
        if (ImGui.Button($"{Icons.Edit}##shape"))
            ImGui.OpenPopup("shapeitem");
        bool changed = false;
        if (ImGui.BeginPopup("shapeitem"))
        {
            if (ImGui.MenuItem("Sphere", typeof(T) != typeof(ZoneSphere)))
            {
                sh = sh.ChangeTo<ZoneSphere>(z);
                changed = true;
            }

            if (ImGui.MenuItem("Ellipsoid", typeof(T) != typeof(ZoneEllipsoid)))
            {
                sh = sh.ChangeTo<ZoneEllipsoid>(z);
                changed = true;
            }

            if (ImGui.MenuItem("Box", typeof(T) != typeof(ZoneBox)))
            {
                sh = sh.ChangeTo<ZoneBox>(z);
                changed = true;
            }

            if (ImGui.MenuItem("Cylinder", typeof(T) != typeof(ZoneCylinder)))
            {
                sh = sh.ChangeTo<ZoneCylinder>(z);
                changed = true;
            }

            if (ImGui.MenuItem("Ring", typeof(T) != typeof(ZoneRing)))
            {
                sh = sh.ChangeTo<ZoneRing>(z);
                changed = true;
            }

            ImGui.EndPopup();
        }

        return changed;
    }

    void ZoneFloatRow(string name, float size, Action<float> set, EditZone ez)
    {
        Controls.PropertyRow(name, size.ToString("0.####"));
        if (ImGui.Button($"{Icons.Edit}##{name}"))
        {
            popups.OpenPopup(new FloatPopup(name, size, (v, d) =>
            {
                set(v);
                if (d)
                    ZoneList.SetZonesDirty(ez);
            }, 1f));
        }
    }

    bool ShapeProperties(ref ZoneShape shape, EditZone zone)
    {
        bool changed = false;
        switch (shape)
        {
            case ZoneCylinder cyl:
                Controls.PropertyRow("Shape", "Cylinder");
                changed = ShapeChangeButton<ZoneCylinder>(ref shape, zone.Current);
                ZoneFloatRow("Radius", cyl.Radius, v => cyl.Radius = v, zone);
                ZoneFloatRow("Height", cyl.Height, v => cyl.Height = v, zone);
                break;
            case ZoneSphere sphere:
                Controls.PropertyRow("Shape", "Sphere");
                changed = ShapeChangeButton<ZoneSphere>(ref shape, zone.Current);
                ZoneFloatRow("Radius", sphere.Radius, v => sphere.Radius = v, zone);
                break;
            case ZoneEllipsoid ellipsoid:
                Controls.PropertyRow("Shape", "Ellipsoid");
                changed = ShapeChangeButton<ZoneEllipsoid>(ref shape, zone.Current);
                ZoneFloatRow("Size X", ellipsoid.Size.X, v => ellipsoid.Size.X = v, zone);
                ZoneFloatRow("Size Y", ellipsoid.Size.Y, v => ellipsoid.Size.Y = v, zone);
                ZoneFloatRow("Size Z", ellipsoid.Size.Z, v => ellipsoid.Size.Z = v, zone);
                break;
            case ZoneBox box:
                Controls.PropertyRow("Shape", "Box");
                changed = ShapeChangeButton<ZoneBox>(ref shape, zone.Current);
                ZoneFloatRow("Size X", box.Size.X, v => box.Size.X = v, zone);
                ZoneFloatRow("Size Y", box.Size.Y, v => box.Size.Y = v, zone);
                ZoneFloatRow("Size Z", box.Size.Z, v => box.Size.Z = v, zone);
                break;
            case ZoneRing ring:
                Controls.PropertyRow("Shape", "Ring");
                changed = ShapeChangeButton<ZoneRing>(ref shape, zone.Current);
                ZoneFloatRow("Outer Radius", ring.OuterRadius, v => ring.OuterRadius = v, zone);
                ZoneFloatRow("Inner Radius", ring.InnerRadius, v => ring.InnerRadius = v, zone);
                ZoneFloatRow("Height", ring.Height, v => ring.Height = v, zone);
                break;
        }

        return changed;
    }


    void ViewPanel()
    {
        ImGui.Checkbox("Nebulae", ref renderer.DrawNebulae);
        ImGui.Checkbox("Starspheres", ref renderer.DrawStarsphere);
    }


    void MoveCameraTo(GameObject obj)
    {
        var r = (obj.RenderComponent as ModelRenderer)?.Model?.GetRadius() ?? 10f;
        var pos = Vector3.Transform(Vector3.Zero, obj.LocalTransform);
        viewport.CameraOffset = pos + new Vector3(0, 0, -r * 3.5f);
        viewport.CameraRotation = new Vector2(-MathF.PI, 0);
    }

    ObjectEditData GetEditData(GameObject obj, bool create = true)
    {
        if (!obj.TryGetComponent<ObjectEditData>(out var d))
        {
            if (create)
            {
                d = new ObjectEditData(obj);
                ObjectsDirty = true;
                obj.Components.Add(d);
            }
        }

        return d;
    }

    void ChangeLoadout(GameObject obj, ObjectLoadout loadout)
    {
        var ed = GetEditData(obj);
        ed.Loadout = loadout;

        var newObj = World.NewObject(obj.SystemObject, Data.Resources, false,
            true, loadout, ed.Archetype);
        ed.Parent = newObj;
        newObj.Components.Add(ed);
        objectList.SelectSingle(newObj);
        newObj.SetLocalTransform(obj.LocalTransform);
        obj.Unregister(World.Physics);
        World.RemoveObject(obj);
        objectList.SetObjects(World);
    }

    void ChangeArchetype(GameObject obj, Archetype archetype)
    {
        var ed = GetEditData(obj);
        ed.Archetype = archetype;
        ed.Loadout = null;
        var newObj = World.NewObject(obj.SystemObject, Data.Resources, false,
            true, null, ed.Archetype);
        ed.Parent = newObj;
        newObj.Components.Add(ed);
        objectList.SelectSingle(newObj);
        newObj.SetLocalTransform(obj.LocalTransform);
        obj.Unregister(World.Physics);
        World.RemoveObject(obj);
        objectList.SetObjects(World);
    }

    void FactionRow(string name, Faction f, Action<Faction> onSet)
    {
        Controls.PropertyRow(name,
            f == null ? "(none)" : $"{f.Nickname} ({Data.Infocards.GetStringResource(f.IdsName)})");
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            popups.OpenPopup(new FactionSelection(onSet, name, f, Data));
    }

    void BaseRow(string name, Base f, Action<Base> onSet, string message = null)
    {
        Controls.PropertyRow(name,
            f == null ? "(none)" : $"{f.Nickname} ({Data.Infocards.GetStringResource(f.IdsName)})");
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            popups.OpenPopup(new BaseSelection(onSet, name, message, f, Data));
    }

    string DockDescription(DockAction a)
    {
        if (a == null) return "(none)";
        var sb = new StringBuilder();
        sb.AppendLine(a.Kind.ToString());
        if (a.Kind == DockKinds.Jump)
        {
            var sys = Data.GameData.Systems.Get(a.Target);
            var sname = sys == null ? "INVALID" : Data.Infocards.GetStringResource(sys.IdsName);
            sb.AppendLine($"{a.Target} ({sname})");
            sb.AppendLine($"{a.Exit}");
        }

        if (a.Kind == DockKinds.Base)
        {
            var b = Data.GameData.Bases.Get(a.Target);
            var bname = b == null ? "INVALID" : Data.Infocards.GetStringResource(b.IdsName);
            sb.AppendLine($"{a.Target} ({bname})");
        }

        return sb.ToString();
    }

    void DockRow(DockAction act, Archetype a, Action<DockAction> onSet)
    {
        Controls.PropertyRow("Dock", DockDescription(act));
        if (ImGui.Button($"{Icons.Edit}##dock"))
        {
            popups.OpenPopup(
                new DockActionSelection(
                    onSet, act, a,
                    objectList.Objects.Select(x => x.SystemObject.Nickname).ToArray(),
                    Data
                )
            );
        }
    }

    void ObjectProperties(GameObject sel)
    {
        var ed = GetEditData(sel, false);
        var gc = sel.Content();
        if (ImGuiExt.Button("Reset", ed != null && !ed.IsNewObject))
        {
            sel.Unregister(World.Physics);
            World.RemoveObject(sel);
            sel = World.NewObject(sel.SystemObject, Data.Resources, false);
            objectList.SelectedTransform = sel.LocalTransform;
            objectList.SelectSingle(sel);
            objectList.SetObjects(World);
        }

        Controls.BeginPropertyTable("properties", true, false, true);
        Controls.PropertyRow("Nickname", sel.Nickname);
        if (ImGui.Button($"{Icons.Edit}##nickname"))
        {
            popups.OpenPopup(new NicknameInputPopup("Rename", sel.Nickname, n => (World.GetObject(n) != null), x =>
            {
                GetEditData(sel);
                sel.Nickname = x;
                objectList.SetObjects(World);
                objectList.ScrollToSelection();
            }));
        }

        Controls.PropertyRow("Name", ed == null
            ? sel.Name.GetName(Data.GameData, camera.Position)
            : Data.Infocards.GetStringResource(ed.IdsName));
        if (ImGui.Button($"{Icons.Edit}##name"))
        {
            popups.OpenPopup(IdsSearch.SearchStrings(Data.Infocards, Data.Fonts,
                newIds => { GetEditData(sel).IdsName = newIds; }));
        }

        //Position
        var pos = Vector3.Transform(Vector3.Zero, sel.LocalTransform);
        var rot = sel.LocalTransform.GetEulerDegrees();
        Controls.PropertyRow("Position", $"{pos.X:0.00}, {pos.Y:0.00}, {pos.Z: 0.00}");
        Controls.PropertyRow("Rotation", $"{rot.X: 0.00}, {rot.Y:0.00}, {rot.Z: 0.00}");
        if (ImGui.Button("0##rot"))
        {
            //Create data if needed
            GetEditData(sel);
            sel.SetLocalTransform(Matrix4x4.CreateTranslation(pos));
            objectList.SelectedTransform = sel.LocalTransform;
        }

        //Archetype
        Controls.PropertyRow("Archetype", gc.Archetype?.Nickname ?? "(none)");
        if (ImGui.Button($"{Icons.Edit}##archetype"))
        {
            popups.OpenPopup(new ArchetypeSelection(
                x => ChangeArchetype(sel, x),
                ed?.Archetype ?? sel.SystemObject.Archetype,
                Data));
        }

        //Loadout
        Controls.PropertyRow("Loadout", gc.Loadout?.Nickname ?? "(none)");
        if (ImGui.Button($"{Icons.Edit}##loadout"))
        {
            popups.OpenPopup(new LoadoutSelection(
                x => ChangeLoadout(sel, x),
                ed != null ? ed.Loadout : sel.SystemObject.Loadout,
                sel.GetHardpoints().Select(x => x.Name).ToArray(),
                Data));
        }

        //Visit
        Controls.PropertyRow("Visit", VisitFlagEditor.FlagsString(gc.Visit));
        if (ImGui.Button($"{Icons.Edit}##visit"))
            popups.OpenPopup(new VisitFlagEditor(gc.Visit, x => GetEditData(sel).Visit = x));
        FactionRow("Reputation", gc.Reputation, x => GetEditData(sel).Reputation = x);
        Controls.EndPropertyTable();
        Controls.BeginPropertyTable("base and docking", true, false, true);
        BaseRow(
            "Base",
            gc.Base,
            x => GetEditData(sel).Base = x,
            "This is the base entry used when linking infocards.\nDocking requires setting the dock action"
        );
        DockRow(gc.Dock, gc.Archetype, x => GetEditData(sel).Dock = x);
        Controls.EndPropertyTable();

        //Comment
        Controls.BeginPropertyTable("comment", true, false, true);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Comment");
        ImGui.TableNextColumn();
        Controls.TruncText(gc.Comment, 20);
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##comment"))
            popups.OpenPopup(new CommentPopup(gc.Comment, x => GetEditData(sel).Comment = x));
        Controls.EndPropertyTable();
    }

    void CreateObject(string nickname, Archetype archetype)
    {
        var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                  Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
        var dir = Vector3.Transform(-Vector3.UnitZ, rot);
        var to = viewport.CameraOffset + (dir * 50);
        var sysobj = new SystemObject()
        {
            Nickname = nickname,
            IdsInfo = Array.Empty<int>(),
            Archetype = archetype,
            Position = to,
        };
        var o = World.NewObject(sysobj, Data.Resources, false);
        GetEditData(o).IsNewObject = true;
        objectList.SelectSingle(o);
        objectList.SetObjects(World);
        objectList.ScrollToSelection();
        ObjectsDirty = true;
    }

    void DeleteObject(GameObject go)
    {
        if (objectList.Selection.Contains(go))
            objectList.Selection.Remove(go);
        var ed = GetEditData(go);
        if (ed == null || !ed.IsNewObject)
            DeletedObjects.Add(go.SystemObject);
        go.Unregister(World.Physics);
        World.RemoveObject(go);
        objectList.SetObjects(World);
    }

    void RestoreObject(SystemObject o)
    {
        DeletedObjects.Remove(o);
        World.NewObject(o, Data.Resources, false);
        objectList.SetObjects(World);
    }

    bool ShouldAddSecondary() => objectList.Selection.Count > 0 && (win.Keyboard.IsKeyDown(Keys.LeftShift) ||
                                                                    win.Keyboard.IsKeyDown(Keys.RightShift));

    private float h1 = 150, h2 = 200;

    private bool propertiesWindow = false;

    void PanelWithProperties(string id, Action panel, Action properties)
    {
        if (!propertiesWindow)
        {
            var totalH = ImGui.GetWindowHeight();
            ImGuiExt.SplitterV(2f, ref h1, ref h2, 15 * ImGuiHelper.Scale, 60 * ImGuiHelper.Scale, -1);
            h1 = totalH - h2 - 24f * ImGuiHelper.Scale;
            ImGui.BeginChild(id, new Vector2(ImGui.GetWindowWidth(), h1));
        }
        else
        {
            ImGui.BeginChild(id);
        }

        panel();
        ImGui.EndChild();
        if (propertiesWindow)
        {
            ImGui.SetNextWindowSize(new Vector2(400, 300) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Properties", ref propertiesWindow))
            {
                properties();
                ImGui.End();
            }
        }
        else
        {
            ImGui.BeginChild("##properties", new Vector2(ImGui.GetWindowWidth(), h2));
            ImGui.Text("Properties");
            ImGui.SameLine();
            if (Controls.SmallButton($"{Icons.UpRightFromSquare}"))
                propertiesWindow = true;
            ImGui.Separator();
            properties();
            ImGui.EndChild();
        }
    }

    void ObjectsPanel()
    {
        PanelWithProperties("##objects", () =>
        {
            if (ImGui.Button("New Object"))
            {
                popups.OpenPopup(new NewObjectPopup(Data, World, CreateObject));
            }

            ImGui.SameLine();
            if (ImGuiExt.Button("Restore Object", DeletedObjects.Count > 0))
                ImGui.OpenPopup("Restore");
            if (ImGui.BeginPopupContextItem("Restore"))
            {
                for (int i = 0; i < DeletedObjects.Count; i++)
                    if (ImGui.MenuItem(DeletedObjects[i].Nickname))
                        RestoreObject(DeletedObjects[i]);
                ImGui.EndPopup();
            }

            objectList.Draw();
        }, () =>
        {
            if (objectList.Selection.Count == 1)
                ObjectProperties(objectList.Selection[0]);
            else if (objectList.Selection.Count > 0)
            {
                ImGui.Text("Multiple objects selected");
                bool canReset = false;
                foreach (var obj in objectList.Selection)
                {
                    if (GetEditData(obj, false) != null)
                    {
                        canReset = true;
                        break;
                    }
                }

                if (ImGuiExt.Button("Reset All", canReset))
                {
                    for (int i = 0; i < objectList.Selection.Count; i++)
                    {
                        var sel = objectList.Selection[i];
                        sel.Unregister(World.Physics);
                        World.RemoveObject(sel);
                        sel = World.NewObject(sel.SystemObject, Data.Resources, false);
                        if (i == 0)
                            objectList.SelectedTransform = sel.LocalTransform;
                        objectList.Selection[i] = sel;
                    }

                    objectList.SetObjects(World);
                }
            }
            else
                ImGui.Text("No Object Selected");
        });
    }

    void LightsPanel()
    {

    }

    void MusicProp(string name, string arg, Action<string> onSet)
    {
        Controls.PropertyRow(name, arg ?? "(none)");
        if (Controls.Music(name, win, !string.IsNullOrEmpty(arg)))
            Data.Sounds.PlayMusic(arg, 0, true);
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            popups.OpenPopup(new MusicSelection(onSet, name, arg, Data, win));
        ImGui.TableNextColumn();
        if (ImGuiExt.Button($"{Icons.TrashAlt}##{name}", !string.IsNullOrEmpty(arg)))
            onSet(null);
    }

    void StarsphereProp(string name, ResolvedModel arg, Action<ResolvedModel> onSet)
    {
        var modelName = arg?.ModelFile;
        if (modelName != null)
            modelName = Path.GetFileName(modelName);
        Controls.PropertyRow(name, modelName ?? "(none)");
        if (ImGui.Button($"{Icons.Edit}##{name}"))
        {
            popups.OpenPopup(new FileSelector(name, Data.GetDataFolder(), file =>
            {
                var modelFile = Data.GameData.ResolveDataPath(file);
                var sourcePath = file.Replace('/', '\\');
                onSet(new ResolvedModel() {ModelFile = modelFile, SourcePath = sourcePath});
                ReloadStarspheres();
            }, FileSelector.MakeFilter(".cmp")));
        }

        ImGui.TableNextColumn();
        if (ImGuiExt.Button($"{Icons.TrashAlt}##{name}", arg != null))
        {
            onSet(null);
            ReloadStarspheres();
        }
    }

    void ReloadStarspheres()
    {
        var models = new List<RigidModel>();
        if (SystemData.StarsBasic != null)
        {
            var mdl = SystemData.StarsBasic.LoadFile(Data.Resources);
            if (mdl is IRigidModelFile rm)
                models.Add(rm.CreateRigidModel(true, Data.Resources));
        }

        if (SystemData.StarsComplex != null)
        {
            var mdl = SystemData.StarsComplex.LoadFile(Data.Resources);
            if (mdl is IRigidModelFile rm)
                models.Add(rm.CreateRigidModel(true, Data.Resources));
        }

        if (SystemData.StarsNebula != null)
        {
            var mdl = SystemData.StarsNebula.LoadFile(Data.Resources);
            if (mdl is IRigidModelFile rm)
                models.Add(rm.CreateRigidModel(true, Data.Resources));
        }

        renderer.StarSphereModels = models.ToArray();
    }

    void ColorProperty(string name, Color4 color, Action<Color4> onSet)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(name);
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(-1);
        if (ImGui.ColorButton(name, color, ImGuiColorEditFlags.NoAlpha,
                new Vector2(ImGui.CalcItemWidth(), ImGui.GetFrameHeight())))
        {
            popups.OpenPopup(new ColorPicker(name, color, onSet));
        }

        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            popups.OpenPopup(new ColorPicker(name, color, onSet));
        ImGui.PopItemWidth();
    }

    void SystemPanel()
    {
        if (ImGuiExt.Button("Reset All", SystemData.IsDirty()))
        {
            SystemData = new SystemEditData(CurrentSystem);
            ReloadStarspheres();
        }

        ImGui.Separator();
        Controls.BeginPropertyTable("Props", true, false, true);
        Controls.PropertyRow("Name", Data.Infocards.GetStringResource(SystemData.IdsName));
        if (ImGui.Button($"{Icons.Edit}##name"))
        {
            popups.OpenPopup(IdsSearch.SearchStrings(Data.Infocards, Data.Fonts,
                newIds => { SystemData.IdsName = newIds; }));
        }

        ColorProperty("Space Color", SystemData.SpaceColor, x => SystemData.SpaceColor = x);
        ColorProperty("Ambient Color", SystemData.Ambient, x => SystemData.Ambient = x);
        Controls.EndPropertyTable();
        ImGui.Separator();
        ImGui.Text("Music");
        Controls.BeginPropertyTable("Music", true, false, true, true, true);
        MusicProp("Space", SystemData.MusicSpace, x => SystemData.MusicSpace = x);
        MusicProp("Battle", SystemData.MusicBattle, x => SystemData.MusicBattle = x);
        MusicProp("Danger", SystemData.MusicDanger, x => SystemData.MusicDanger = x);
        Controls.EndPropertyTable();
        ImGui.Separator();
        ImGui.Text("Stars");
        Controls.BeginPropertyTable("Stars", true, false, true, true);
        StarsphereProp("Layer 1", SystemData.StarsBasic, x => SystemData.StarsBasic = x);
        StarsphereProp("Layer 2", SystemData.StarsComplex, x => SystemData.StarsComplex = x);
        StarsphereProp("Layer 3", SystemData.StarsNebula, x => SystemData.StarsNebula = x);
        Controls.EndPropertyTable();
    }

    private static readonly Color4[] zoneColors = new Color4[]
    {
        Color4.White,
        Color4.Teal,
        Color4.Coral,
        Color4.LimeGreen,
    };

    private void RenderZones()
    {
        ZoneRenderer.Begin(win.RenderContext, camera);
        foreach (var ez in ZoneList.Zones)
        {
            if (!ez.Visible &&
                ez != ZoneList.HoveredZone &&
                ez != ZoneList.Selected)
                continue;
            var z = ez.Current;
            var zoneColor = zoneColors[(int) ZoneList.GetZoneType(z.Nickname)]
                .ChangeAlpha(0.5f);
            bool inZone = z.Shape.ContainsPoint(camera.Position);
            switch (z.Shape)
            {
                case ZoneSphere sph:
                    ZoneRenderer.DrawSphere(z.Position, sph.Radius, z.RotationMatrix, zoneColor, inZone);
                    break;
                case ZoneEllipsoid epl:
                    ZoneRenderer.DrawEllipsoid(z.Position, epl.Size, z.RotationMatrix, zoneColor, inZone);
                    break;
                case ZoneCylinder cyl:
                    ZoneRenderer.DrawCylinder(z.Position, cyl.Radius, cyl.Height, z.RotationMatrix, zoneColor, inZone);
                    break;
                case ZoneRing ring:
                    ZoneRenderer.DrawRing(z.Position, ring.InnerRadius, ring.OuterRadius, ring.Height, z.RotationMatrix,
                        zoneColor, inZone);
                    break;
                case ZoneBox box:
                    ZoneRenderer.DrawCube(z.Position, box.Size, z.RotationMatrix, zoneColor, inZone);
                    break;
            }
        }

        hoveredZone = null;
        ZoneRenderer.Finish(Data.Resources);

        foreach (var obj in objectList.Selection)
        {
            var rc = obj.RenderComponent as ModelRenderer;
            if (rc == null) continue;
            var bbox = rc.Model.GetBoundingBox();
            EditorPrimitives.DrawBox(renderer.DebugRenderer, bbox, obj.LocalTransform, Color4.White);
        }
    }

    public override void Update(double elapsed)
    {
        var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                  Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
        var dir = Vector3.Transform(-Vector3.UnitZ, rot);
        var to = viewport.CameraOffset + (dir * 10);
        camera.Update(viewport.RenderWidth, viewport.RenderHeight, viewport.CameraOffset, to, rot);
        World.Update(elapsed);
    }

    void DrawInfocard()
    {
        if (infocardOpen)
        {
            ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Infocard", ref infocardOpen))
            {
                var szX = Math.Max(20, ImGui.GetWindowWidth());
                var szY = Math.Max(20, ImGui.GetWindowHeight());
                if (icard == null)
                {
                    icard = new InfocardControl(win, systemInfocard, szX);
                }

                icard.Draw(szX);
                ImGui.End();
            }
        }
    }

    void DrawMaps()
    {
        if (mapOpen)
        {
            ImGui.SetNextWindowSize(new Vector2(300) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Map", ref mapOpen ))
            {
                var szX = Math.Max(20, ImGui.GetWindowWidth());
                var szY = Math.Max(20, ImGui.GetWindowHeight() - 37 * ImGuiHelper.Scale);
                systemMap.Draw((int) szX, (int) szY, 1 / 60.0f);
                ImGui.End();
            }
        }
    }

    private static readonly Regex endNumbers = new Regex(@"(\d+)$", RegexOptions.Compiled);

    static string MakeCopyNickname(string nickname)
    {
        var m = endNumbers.Match(nickname);
        if (m.Success)
        {
            var newNumber = (int.Parse(m.Groups[1].Value) + 1)
                .ToString()
                .PadLeft(m.Groups[1].Value.Length, '0');
            return nickname.Substring(0, m.Groups[1].Index) + newNumber;
        }

        return m + "_01";
    }

    public override void OnHotkey(Hotkeys hk, bool shiftPressed)
    {
        if (hk == Hotkeys.Deselect)
        {
            objectList.SelectSingle(null);
            ZoneList.Selected = null;
        }

        if (hk == Hotkeys.ToggleGrid)
            renderGrid = !renderGrid;

        if (hk == Hotkeys.ResetViewport)
            viewport.ResetControls();

        if (hk == Hotkeys.Copy && ObjectMode && objectList.Selection.Count > 0)
        {
            win.Clipboard = objectList.Selection
                .Select(x => (GetEditData(x, false) ?? new ObjectEditData(x)).MakeCopy())
                .ToArray();
        }

        if (hk == Hotkeys.Paste && ObjectMode && win.Clipboard is ObjectEditData[] objs)
        {
            objectList.SetObjects(World);
            objectList.Selection = new List<GameObject>();
            foreach (var o in objs)
            {
                string n = o.SystemObject.Nickname;
                while (World.GetObject(n) != null)
                {
                    n = MakeCopyNickname(n);
                }

                var newData = o.MakeCopy();
                newData.SystemObject.Nickname = n;
                var newObject = World.NewObject(newData.SystemObject, Data.Resources, false, true,
                    newData.Loadout, newData.Archetype);
                newData.Parent = newObject;
                newObject.Components.Add(newData);
                objectList.Selection.Add(newObject);
            }

            objectList.SelectedTransform = objectList.Selection[0].LocalTransform;
        }
    }

    BitArray128 openTabs;

    void TabButtons()
    {
        ImGui.BeginGroup();
        TabHandler.TabButton("Zones", 0, ref openTabs);
        TabHandler.TabButton("Objects", 1, ref openTabs);
        //TabHandler.TabButton("Lights", 2, ref openTabs);
        TabHandler.TabButton("System", 3, ref openTabs);
        TabHandler.TabButton("View", 4, ref openTabs);
        ImGui.EndGroup();
        ImGui.SameLine();
    }

    private bool firstTab = true;

    public override unsafe void Draw(double elapsed)
    {
        var curSysName = Data.Infocards.GetStringResource(SystemData.IdsName);
        Title = $"{curSysName} ({CurrentSystem.Nickname})";
        World.RenderUpdate(elapsed);
        ImGuiHelper.AnimatingElement();
        var contentw = ImGui.GetContentRegionAvail().X;
        if (openTabs.Any())
        {
            ImGui.Columns(2, "##panels", true);
            if (firstTab)
            {
                ImGui.SetColumnWidth(0, contentw * 0.23f);
                firstTab = false;
            }

            ImGui.BeginChild("##tabchild");
            if (openTabs[0]) ZonesPanel();
            if (openTabs[1]) ObjectsPanel();
            if (openTabs[2]) LightsPanel();
            if (openTabs[3]) SystemPanel();
            if (openTabs[4]) ViewPanel();
            ImGui.EndChild();
            ImGui.NextColumn();
        }

        TabButtons();
        ImGui.BeginChild("##main");
        ImGui.SameLine();
        using (var tb = Toolbar.Begin("##toolbar", false))
        {
            tb.CheckItem("Grid", ref renderGrid);
            tb.TextItem($"Camera Position: {camera.Position}");
            tb.ToggleButtonItem("Map", ref mapOpen);
            tb.ToggleButtonItem("Infocard", ref infocardOpen);
        }

        renderer.BackgroundOverride = SystemData.SpaceColor;
        renderer.SystemLighting.Ambient = SystemData.Ambient;
        viewport.Begin();
        renderer.Draw(viewport.RenderWidth, viewport.RenderHeight);
        viewport.End();
        if (ManipulateObjects() || ManipulateZone())
        {
            viewport.SetInputsEnabled(false);
        }
        else
        {
            viewport.SetInputsEnabled(true);
        }

        ImGui.EndChild();
        DrawMaps();
        DrawInfocard();
        popups.Run();
    }

    private bool ZonesMode => openTabs[0];
    private bool ObjectMode => openTabs[1];

    unsafe bool ManipulateObjects()
    {
        if (ObjectMode && objectList.Selection.Count > 0)
        {
            var v = camera.View;
            var p = camera.Projection;
            var mode = ImGui.GetIO().KeyCtrl ? GuizmoMode.WORLD : GuizmoMode.LOCAL;
            fixed (Matrix4x4* tr = &objectList.SelectedTransform)
            {
                Matrix4x4 delta;
                if (ImGuizmo.Manipulate(ref v, ref p, GuizmoOperation.TRANSLATE | GuizmoOperation.ROTATE_AXIS,
                        mode,
                        tr, &delta))
                {
                    ObjectsDirty = true;
                    GetEditData(objectList.Selection[0]);
                    for (int i = 1; i < objectList.Selection.Count; i++)
                    {
                        objectList.Selection[i].SetLocalTransform(objectList.Selection[i].LocalTransform * delta);
                        GetEditData(objectList.Selection[i]);
                    }
                }
            }

            objectList.Selection[0].SetLocalTransform(objectList.SelectedTransform);
            return ImGuizmo.IsOver() || ImGuizmo.IsUsing();
        }

        return false;
    }

    unsafe bool ManipulateZone()
    {
        if (ZonesMode && ZoneList.Selected != null)
        {
            var v = camera.View;
            var p = camera.Projection;
            var zs = ZoneList.Selected;
            var (zsize, zsizemode) = zs.Current.Shape.GetSize();
            var mode = GuizmoMode.LOCAL;
            //Cannot combine scale with WORLD rotations
            if (ImGui.GetIO().KeyCtrl)
            {
                zsizemode = 0;
                mode = GuizmoMode.WORLD;
            }

            var tr = Matrix4x4.CreateScale(zsize) * zs.Current.RotationMatrix *
                     Matrix4x4.CreateTranslation(zs.Current.Position);
            if (ImGuizmo.Manipulate(ref v, ref p, GuizmoOperation.TRANSLATE | GuizmoOperation.ROTATE_AXIS | zsizemode,
                    mode,
                    &tr, (Matrix4x4*) 0))
            {
                Matrix4x4.Decompose(tr, out var newScale, out var rot, out _);
                zs.Current.Position = Vector3.Transform(Vector3.Zero, tr);
                zs.Current.RotationMatrix = Matrix4x4.CreateFromQuaternion(rot);
                zs.Current.Shape.SetSize(newScale);
                zs.Current.RotationAngles = zs.Current.RotationMatrix.GetEulerDegrees();
                zs.Current.Shape?.Update();
                ZoneList.SetZonesDirty(zs);
                World.Renderer.ZoneVersion++;
            }

            return ImGuizmo.IsOver() || ImGuizmo.IsUsing();
        }

        return false;
    }


    public override void Dispose()
    {
        World.Dispose();
        renderer.Dispose();
        viewport.Dispose();
    }
}
