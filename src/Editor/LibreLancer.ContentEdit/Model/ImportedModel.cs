using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Vms;
using LibreLancer.World;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public class ImportedModel
{
    public string Name;
    public ImportedModelNode Root;
    public Dictionary<string, ImageData> Images;

    public static EditResult<ImportedModel> FromSimpleMesh(string name, SimpleMesh.Model input)
    {
        Dictionary<string, ModelNode[]> autodetect = new Dictionary<string,ModelNode[]>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var obj in input.Roots)
        {
            var res = GetLods(obj, autodetect);
            if (res.IsError) 
                return new EditResult<ImportedModel>(null, res.Messages);
        }
        List<ImportedModelNode> nodes = new List<ImportedModelNode>();
        foreach(var obj in input.Roots) {
            var res = AutodetectTree(obj, nodes, null, autodetect);
            if (res.IsError)
                return new EditResult<ImportedModel>(null, res.Messages);
        }
        if (nodes.Count > 1) {
            return EditResult<ImportedModel>.Error("More than one root model");
        }
        if (nodes.Count == 0) {
            return EditResult<ImportedModel>.Error("Could not find root model");
        }
        if(nodes[0].Def.Geometry?.Kind == GeometryKind.Lines)
            return EditResult<ImportedModel>.Error("Root mesh cannot be wireframe");
        var m = new ImportedModel() {Name = name, Root = nodes[0], Images = input.Images};
        //Set up root
        m.Root.Construct = null;
        foreach (var child in m.Root.Children)
            child.Construct.ParentName = "Root";
        return new EditResult<ImportedModel>(m);
    }

    static bool IsHull(ModelNode node)
    {
        return node.Properties.ContainsKey("hull") ||
               node.Name.EndsWith("$hull");
    }

    static bool GetHardpoint(ModelNode node, out HardpointDefinition hp)
    {
        hp = null;
        PropertyValue pv;
        if (!node.Properties.TryGetValue("hardpoint", out pv) || !pv.AsBoolean())
            return false;
        var orientation = Matrix4x4.CreateFromQuaternion(node.Transform.ExtractRotation());
        var position = Vector3.Transform(Vector3.Zero, node.Transform);
        if (node.Properties.TryGetValue("hptype", out pv) && pv.AsString(out var hptype) &&
            hptype.Equals("rev", StringComparison.OrdinalIgnoreCase))
        {
            Vector3 axis;
            float min;
            float max;
            if (!node.Properties.TryGetValue("axis", out pv) || !pv.AsVector3(out axis))
                axis = Vector3.UnitY;
            if (!node.Properties.TryGetValue("min", out pv) || !pv.AsSingle(out min))
                min = -45f;
            if (!node.Properties.TryGetValue("max", out pv) || !pv.AsSingle(out max))
                max = 45f;
            if (min > max) {
                (min, max) = (max, min);
            }
            hp = new RevoluteHardpointDefinition(node.Name) {
                Orientation = orientation,
                Position = position,
                Min = MathHelper.DegreesToRadians(min),
                Max = MathHelper.DegreesToRadians(max),
                Axis = axis,
            };
        }
        else
        {
            hp = new FixedHardpointDefinition(node.Name) {Orientation = orientation, Position = position};
        }
        return true;
    }

    static AbstractConstruct GetConstruct(ModelNode node, string childName, string parentName)
    {
        var rot = Matrix4x4.CreateFromQuaternion(node.Transform.ExtractRotation());
        var origin = Vector3.Transform(Vector3.Zero, node.Transform);
        if (!node.Properties.TryGetValue("construct", out var construct) ||
            !construct.AsString(out var contype))
        {
            return new FixConstruct() {Rotation = rot, Origin = origin, ParentName = parentName, ChildName = childName};
        }
        PropertyValue pv;
        Vector3 axis;
        Vector3 offset = Vector3.Zero;
        float min;
        float max;
        switch (contype.ToLowerInvariant())
        {
            case "rev":
            {
               
                if(node.Properties.TryGetValue("offset", out pv))  pv.AsVector3(out offset);
                if (!node.Properties.TryGetValue("axis_rotation", out pv) || !pv.AsVector3(out axis))
                    axis = Vector3.UnitY;
                if (!node.Properties.TryGetValue("min", out pv) || !pv.AsSingle(out min))
                    min = -90f;
                if (!node.Properties.TryGetValue("max", out pv) || !pv.AsSingle(out max))
                    max = 90f;
                if (min > max) {
                    (min, max) = (max, min);
                }
                return new RevConstruct()
                {
                    Rotation = rot, Origin = origin,
                    Min = MathHelper.DegreesToRadians(min),
                    Max = MathHelper.DegreesToRadians(max),
                    AxisRotation = axis,
                    Offset = offset,
                    ParentName = parentName,
                    ChildName = node.Name,
                };
            }
            case "pris":
            {
                if(node.Properties.TryGetValue("offset", out pv))  pv.AsVector3(out offset);
                if (!node.Properties.TryGetValue("axis_translation", out pv) || !pv.AsVector3(out axis))
                    axis = Vector3.UnitY;
                if (!node.Properties.TryGetValue("min", out pv) || !pv.AsSingle(out min))
                    min = 0;
                if (!node.Properties.TryGetValue("max", out pv) || !pv.AsSingle(out max))
                    max = 1;
                if (min > max) {
                    (min, max) = (max, min);
                }
                return new PrisConstruct()
                {
                    Rotation = rot, Origin = origin,
                    Min = min,
                    Max = max,
                    AxisTranslation = axis,
                    Offset = offset,
                    ParentName = parentName,
                    ChildName = childName
                };
            }
            case "sphere":
                if(node.Properties.TryGetValue("offset", out pv))  pv.AsVector3(out offset);
                if (!node.Properties.TryGetValue("min", out pv) || !pv.AsVector3(out var minaxis))
                    minaxis = new Vector3(-MathF.PI);
                if (!node.Properties.TryGetValue("max", out pv) || !pv.AsVector3(out var maxaxis))
                    maxaxis = new Vector3(MathF.PI);
                return new SphereConstruct()
                {
                    Rotation = rot, Origin = origin,
                    Offset = offset,
                    Min1 = minaxis.X, Min2 = minaxis.Y, Min3 = minaxis.Z,
                    Max1 = maxaxis.X, Max2 = maxaxis.Y, Max3 = maxaxis.Z,
                    ParentName = parentName,
                    ChildName = childName
                };
            case "fix":
            default:
                return new FixConstruct() {Rotation = rot, Origin = origin, ParentName = parentName, ChildName = childName};
        }
    }

    static bool IsWire(ModelNode mn) => mn.Geometry != null && mn.Geometry.Kind == GeometryKind.Lines;


    static EditResult<bool> AutodetectTree(ModelNode obj, List<ImportedModelNode> parent, string parentName, Dictionary<string,ModelNode[]> autodetect)
    {
        //Skip detected lods & hulls
        var num = LodNumber(obj, out _);
        if (num != 0) return true.AsResult();
        if (IsHull(obj)) return true.AsResult();
        //Build tree
        var mdl = new ImportedModelNode();
        mdl.Name = obj.Name;
        if (obj.Name.EndsWith("$lod0", StringComparison.InvariantCultureIgnoreCase))
            mdl.Name = obj.Name.Remove(obj.Name.Length - 5, 5);
        mdl.Construct = GetConstruct(obj, mdl.Name, parentName);
        mdl.Construct?.Reset();
        var geometry = autodetect[mdl.Name];
        foreach (var g in geometry)
            if (g != null) mdl.LODs.Add(g);
        foreach(var child in obj.Children) {
            
            if(IsHull(child))
                mdl.Hulls.Add(child);
            else if (IsWire(child))
            {
                if (mdl.Wire != null)
                    return EditResult<bool>.Error($"Node {obj.Name} has more than one wireframe child");
                mdl.Wire = child;
            }
            else if (GetHardpoint(child, out var hp))
                mdl.Hardpoints.Add(hp);
            else
                AutodetectTree(child, mdl.Children, mdl.Name, autodetect);
        }
        parent.Add(mdl);
        return true.AsResult();
    }
    static EditResult<bool> GetLods(ModelNode obj, Dictionary<string,ModelNode[]> autodetect)
    {
        string objn;
        var num = LodNumber(obj, out objn);
        if(num != -1) {
            ModelNode[] lods;
            if(!autodetect.TryGetValue(objn, out lods)) {
                lods = new ModelNode[10];
                autodetect.Add(objn, lods);
            }
            lods[num] = obj;
        }
        foreach (var child in obj.Children)
        {
            var res = GetLods(child, autodetect);
            if (res.IsError) return res;
        }
        return true.AsResult();
    }
    
    static bool CheckSuffix(string postfixfmt, string src, int count)
    {
        for (int i = 0; i < count; i++)
            if (src[src.Length - postfixfmt.Length + i] != postfixfmt[i]) return false;
        return true;
    }
    
    //Autodetected LOD: object with geometry + suffix $lod[0-9]
    static int LodNumber(ModelNode obj, out string name)
    {
        name = obj.Name;
        if (obj.Geometry == null) return -1;
        if (obj.Geometry.Kind == GeometryKind.Lines) return -1;
        if (obj.Name.Length < 6) return 0;
        if (!char.IsDigit(obj.Name, obj.Name.Length - 1)) return 0;
        if (!CheckSuffix("$lodX", obj.Name, 4)) return 0;
        name = obj.Name.Substring(0, obj.Name.Length - "$lodX".Length);
        return int.Parse(obj.Name[obj.Name.Length - 1] + "");
    }

    bool VerifyModelMaterials(ModelNode mn)
    {
        if (mn.Geometry.Groups.Any(x => string.IsNullOrWhiteSpace(x.Material.Name)))
            return false;
        return true;
    }
    
    bool VerifyMaterials(ImportedModelNode r)
    {
        foreach (var l in r.LODs)
        {
            if (!VerifyModelMaterials(l)) return false;
        }
        if (!VerifyModelMaterials(r.Def)) return false;
        foreach(var child in r.Children)
            if (!VerifyMaterials(child))
                return false;
        return true;
    }

    public EditResult<EditableUtf> CreateModel(ModelImporterSettings settings)
    {
        var utf = new EditableUtf();
        //Vanity
        var expv = new LUtfNode() {Name = "Exporter Version", Parent = utf.Root};
        expv.StringData = "LancerEdit " + Platform.GetInformationalVersion<ImportedModel>();
        utf.Root.Children.Add(expv);

        if (string.IsNullOrWhiteSpace(Name))
            return EditResult<EditableUtf>.Error("Model name cannot be empty");
        if (Root == null)
            return EditResult<EditableUtf>.Error("Model must have a root node");
        if(Root.LODs.Count == 0)
            return EditResult<EditableUtf>.Error("Model root must have a mesh");
        if (!VerifyMaterials(Root))
            return EditResult<EditableUtf>.Error("Material name cannot be empty");
        if (Root.Children.Count == 0 && !settings.ForceCompound)
            Export3DB(Name, utf.Root, Root);
        else
        {
            var suffix = $".{IdSalt.New()}.3db";
            var vmslib = new LUtfNode() {Name = "VMeshLibrary", Parent = utf.Root, Children = new List<LUtfNode>()};
            utf.Root.Children.Add(vmslib);
            var cmpnd = new LUtfNode() {Name = "Cmpnd", Parent = utf.Root, Children = new List<LUtfNode>()};
            utf.Root.Children.Add(cmpnd);
            ExportModels(Name, utf.Root, suffix, vmslib, Root);
            int cmpndIndex = 1;
            var consBuilder = new ConsBuilder();
            cmpnd.Children.Add(CmpndNode(cmpnd, "Root", Root.Name + suffix, "Root", 0));
            foreach (var child in Root.Children)
            {
                ProcessConstruct(child, cmpnd, consBuilder, suffix, ref cmpndIndex);
            }

            var cons = new LUtfNode() {Name = "Cons", Parent = cmpnd, Children = new List<LUtfNode>()};
            if (consBuilder.Fix != null) {
                cons.Children.Add(new LUtfNode() { Name = "Fix", Parent = cons, Data = consBuilder.Fix.GetData()});
            }
            if (consBuilder.Rev != null) {
                cons.Children.Add(new LUtfNode() { Name = "Rev", Parent = cons, Data = consBuilder.Rev.GetData()});
            }
            if (consBuilder.Pris != null) {
                cons.Children.Add(new LUtfNode() { Name = "Pris", Parent = cons, Data = consBuilder.Pris.GetData()});
            }
            if (consBuilder.Sphere != null) {
                cons.Children.Add(new LUtfNode() { Name = "Sphere", Parent = cons, Data = consBuilder.Sphere.GetData()});
            }
            if(cons.Children.Count > 0)
                cmpnd.Children.Add(cons);
        }

        if (settings.GenerateMaterials)
        {
            List<SimpleMesh.Material> materials = new List<SimpleMesh.Material>();
            IterateMaterials(materials, Root);
            var mats = new LUtfNode() { Name = "material library", Parent = utf.Root };
            mats.Children = new List<LUtfNode>();
            int i = 0;
            foreach (var mat in materials)
                mats.Children.Add(DefaultMaterialNode(mats,mat,i++));
            var txms = new LUtfNode() { Name = "texture library", Parent = utf.Root };
            txms.Children = new List<LUtfNode>();
            HashSet<string> createdTextures = new HashSet<string>();
            foreach (var mat in materials)
            {
                if (mat.DiffuseTexture != null)
                {
                    if (createdTextures.Contains(mat.DiffuseTexture)) continue;
                    createdTextures.Add(mat.DiffuseTexture);
                    if (settings.ImportTextures && Images != null && Images.TryGetValue(mat.DiffuseTexture, out var img))
                    {
                        txms.Children.Add(ImportTextureNode(txms, mat.DiffuseTexture, img.Data));
                    }
                    else
                    {
                        txms.Children.Add(DefaultTextureNode(txms, mat.DiffuseTexture));
                    }
                }
                else {
                    txms.Children.Add(DefaultTextureNode(txms, mat.Name));
                }
            }

            utf.Root.Children.Add(mats);
            utf.Root.Children.Add(txms);
        }

        return utf.AsResult();
    }
    
    static LUtfNode DefaultMaterialNode(LUtfNode parent, SimpleMesh.Material mat, int i)
    {
        var matnode = new LUtfNode() { Name = mat.Name, Parent = parent };
        matnode.Children = new List<LUtfNode>();
        matnode.Children.Add(new LUtfNode() { Name = "Type", Parent = matnode, StringData = "DcDt" });
        var arr = new float[] {mat.DiffuseColor.X, mat.DiffuseColor.Y, mat.DiffuseColor.Z};
        matnode.Children.Add(new LUtfNode() { Name = "Dc", Parent = matnode, Data = UnsafeHelpers.CastArray(arr) });
        string textureName = (mat.DiffuseTexture ?? mat.Name) + ".dds";
        matnode.Children.Add(new LUtfNode() { Name = "Dt_name", Parent = matnode, StringData = textureName });
        matnode.Children.Add(new LUtfNode() { Name = "Dt_flags", Parent = matnode, Data = BitConverter.GetBytes(64) });
        return matnode;
    }
    
    static LUtfNode ImportTextureNode(LUtfNode parent, string name, ReadOnlySpan<byte> data)
    {
        var texnode = new LUtfNode() { Name = name + ".dds", Parent = parent };
        texnode.Children = new List<LUtfNode>();
        texnode.Children.Add(TextureImport.ImportAsMIPSNode(data, texnode));
        return texnode;
    }
    
    static LUtfNode DefaultTextureNode(LUtfNode parent, string name)
    {
        var texnode = new LUtfNode() { Name = name + ".dds", Parent = parent };
        texnode.Children = new List<LUtfNode>();
        var d = new byte[DefaultTexture.Data.Length];
        Buffer.BlockCopy(DefaultTexture.Data, 0, d, 0, DefaultTexture.Data.Length);
        texnode.Children.Add(new LUtfNode() { Name = "MIPS", Parent = texnode, Data = d });
        return texnode;
    }

    static bool HasMat(List<Material> materials, Material m)
    {
        foreach (var m2 in materials)
        {
            if (m2.Name == m.Name) return true;
        }
        return false;
    }
    static void IterateMaterials(List<Material> materials, ImportedModelNode mdl)
    {
        foreach (var lod in mdl.LODs)
        foreach (var dc in lod.Geometry.Groups)
            if (dc.Material != null && !HasMat(materials, dc.Material))
                materials.Add(dc.Material);
        foreach (var child in mdl.Children)
            IterateMaterials(materials, child);
    }

    class ConsBuilder
    {
        public FixConstructor Fix;
        public RevConstructor Rev;
        public PrisConstructor Pris;
        public SphereConstructor Sphere;
    }

    void ProcessConstruct(ImportedModelNode mdl, LUtfNode cmpnd, ConsBuilder cons, string suffix,
        ref int index)
    {
        cmpnd.Children.Add(CmpndNode(cmpnd, "PART_" + mdl.Name, mdl.Name + suffix, mdl.Name, index++));
        switch (mdl.Construct)
        {
            case FixConstruct fix:
                cons.Fix ??= new FixConstructor();
                cons.Fix.Add(fix);
                break;
            case RevConstruct rev:
                cons.Rev ??= new RevConstructor();
                cons.Rev.Add(rev);
                break;
            case PrisConstruct pris:
                cons.Pris ??= new PrisConstructor();
                cons.Pris.Add(pris);
                break;
            case SphereConstruct sphere:
                cons.Sphere ??= new SphereConstructor();
                cons.Sphere.Add(sphere);
                break;
        }
        foreach (var child in mdl.Children)
            ProcessConstruct(child, cmpnd, cons, suffix, ref index);
    }

    LUtfNode CmpndNode(LUtfNode cmpnd, string name, string filename, string objname, int index)
    {
        var node = new LUtfNode() {Parent = cmpnd, Name = name, Children = new List<LUtfNode>()};
        node.Children.Add(new LUtfNode()
        {
            Name = "File Name",
            Parent = node,
            StringData = filename
        });
        node.Children.Add(new LUtfNode()
        {
            Name = "Object Name",
            Parent = node,
            StringData = objname,
        });
        node.Children.Add(new LUtfNode()
        {
            Name = "Index",
            Parent = node,
            Data = BitConverter.GetBytes(index)
        });
        return node;
    }

    void ExportModels(string mdlName, LUtfNode root, string suffix, LUtfNode vms, ImportedModelNode model)
    {
        var modelNode = new LUtfNode() {Parent = root, Name = model.Name + suffix};
        modelNode.Children = new List<LUtfNode>();
        root.Children.Add(modelNode);
        Export3DB(mdlName, modelNode, model, vms);
        foreach (var child in model.Children)
            ExportModels(mdlName, root, suffix, vms, child);
    }

    static ushort[] GetIndicesForWire(Geometry lod, Geometry vmeshwire)
    {
        ushort[] newIndices = new ushort[vmeshwire.Indices.Length];
        for (int i = 0; i < vmeshwire.Indices.Length; i++)
        {
            var pos = vmeshwire.Vertices[vmeshwire.Indices.Indices16[i]].Position;
            int j;
            for (j = 0; j < lod.Vertices.Length; j++)
            {
                if (Vector3.Distance(lod.Vertices[j].Position, pos) < 0.01f)
                    break;
            }
            if (j == lod.Vertices.Length)
                return null;
            newIndices[i] = (ushort)j;
        }
        return newIndices;
    }

    static LUtfNode GetVMeshWireNode(LUtfNode parentNode, uint crc, ushort[] indices)
    {
        var vertexOffset = indices.Min();
        var max = indices.Max();
        using var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write(VMeshWire.HEADER_SIZE);
        writer.Write(crc);
        writer.Write(vertexOffset); 
        writer.Write((ushort)(max - vertexOffset)); //vertex count
        writer.Write((ushort)(indices.Length)); //index count
        writer.Write(max); //max vertex
        foreach(var i in indices)
            writer.Write((ushort)(i - vertexOffset));
        var wireNode = new LUtfNode{Name = "VMeshWire", Parent = parentNode, Children = new List<LUtfNode>()};
        wireNode.Children.Add(new LUtfNode() { Name = "VWireData", Parent = wireNode, Data = ms.ToArray()});
        return wireNode;
    }

    static void Export3DB(string mdlName, LUtfNode node3db, ImportedModelNode mdl, LUtfNode vmeshlibrary = null)
    {
        var vms = vmeshlibrary ?? new LUtfNode()
            {Name = "VMeshLibrary", Parent = node3db, Children = new List<LUtfNode>()};
        for (int i = 0; i < mdl.LODs.Count; i++)
        {
            var n = new LUtfNode()
            {
                Name = $"{mdlName}-{mdl.Name}.lod{i}.{(int) GeometryWriter.FVF(mdl.LODs[i].Geometry)}.vms",
                Parent = vms
            };
            n.Children = new List<LUtfNode>();
            n.Children.Add(new LUtfNode()
                {Name = "VMeshData", Parent = n, Data = GeometryWriter.VMeshData(mdl.LODs[i].Geometry)});
            vms.Children.Add(n);
        }
        
        if (vmeshlibrary == null)
            node3db.Children.Add(vms);
        if (mdl.LODs.Count > 1)
        {
            var multilevel = new LUtfNode() {Name = "MultiLevel", Parent = node3db};
            multilevel.Children = new List<LUtfNode>();
            var switch2 = new LUtfNode() {Name = "Switch2", Parent = multilevel};
            multilevel.Children.Add(switch2);
            for (int i = 0; i < mdl.LODs.Count; i++)
            {
                var n = new LUtfNode() {Name = "Level" + i, Parent = multilevel};
                n.Children = new List<LUtfNode>();
                n.Children.Add(new LUtfNode() {Name = "VMeshPart", Parent = n, Children = new List<LUtfNode>()});
                n.Children[0].Children.Add(new LUtfNode()
                {
                    Name = "VMeshRef",
                    Parent = n.Children[0],
                    Data = GeometryWriter.VMeshRef(mdl.LODs[i].Geometry,
                        string.Format("{0}-{1}.lod{2}.{3}.vms", mdlName, mdl.Name, i,
                            (int) GeometryWriter.FVF(mdl.LODs[i].Geometry)))
                });
                multilevel.Children.Add(n);
            }

            //Generate Switch2: TODO - Be more intelligent about this
            var mlfloats = new float[multilevel.Children.Count];
            mlfloats[0] = 0;
            float cutOff = 2250;
            for (int i = 1; i < mlfloats.Length - 1; i++)
            {
                mlfloats[i] = cutOff;
                cutOff *= 2;
            }

            mlfloats[mlfloats.Length - 1] = 1000000;
            switch2.Data = UnsafeHelpers.CastArray(mlfloats);
            node3db.Children.Add(multilevel);
        }
        else
        {
            var part = new LUtfNode() {Name = "VMeshPart", Parent = node3db};
            part.Children = new List<LUtfNode>();
            part.Children.Add(new LUtfNode()
            {
                Name = "VMeshRef",
                Parent = part,
                Data = GeometryWriter.VMeshRef(mdl.LODs[0].Geometry,
                    string.Format("{0}-{1}.lod0.{2}.vms", mdlName, mdl.Name,
                        (int) GeometryWriter.FVF(mdl.LODs[0].Geometry)))
            });
            node3db.Children.Add(part);
        }

        if (mdl.Hardpoints.Count > 0)
        {
            var hp = new ModelHpNode() {Node = node3db};
            hp.HardpointsToNodes(mdl.Hardpoints.Select(x => new Hardpoint(x, null)).ToList());
        }
        
        if (mdl.Wire != null)
        {
            ushort[] wireIndices = null;
            Geometry wireLod = mdl.Wire.Geometry;
            Geometry srcGeometry = null;
            int i;
            for (i = 0; i < mdl.LODs.Count; i++)
            {
                if ((wireIndices = GetIndicesForWire(mdl.LODs[i].Geometry, wireLod)) != null) {
                    srcGeometry = mdl.LODs[i].Geometry;
                    break;
                }
            }
            if (wireIndices != null)
            {
                FLLog.Info("Import", $"{mdl.Name} VMeshWire created from existing VMeshData");
                node3db.Children.Add(GetVMeshWireNode(node3db,
                    CrcTool.FLModelCrc($"{mdlName}-{mdl.Name}.lod{i}.{(int) GeometryWriter.FVF(srcGeometry)}.vms"),
                    wireIndices
                    ));
            }
            else
            {
                FLLog.Info("Import", $"{mdl.Name} VMeshWire creating new VMeshData");
                string nodeName = $"{mdlName}-{mdl.Name}.vmeshwire.pos.vms";
                var n = new LUtfNode()
                {
                    Name = nodeName,
                    Parent = vms
                };
                n.Children = new List<LUtfNode>();
                n.Children.Add(new LUtfNode()
                    {Name = "VMeshData", Parent = n, Data = GeometryWriter.VMeshData(wireLod, D3DFVF.XYZ)});
                vms.Children.Add(n);
                node3db.Children.Add(GetVMeshWireNode(node3db, CrcTool.FLModelCrc(nodeName), wireLod.Indices.Indices16));
            }
        }
    }
}