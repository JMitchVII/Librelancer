using System.Numerics;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;

namespace LibreLancer.ContentEdit;

public class ModelRenderer
{
    static Lighting lighting;
    static ModelRenderer()
    {
        lighting = Lighting.Create();
        lighting.Enabled = true;
        lighting.Ambient = Color3f.Black;
        var src = new SystemLighting();
        src.Lights.Add(new DynamicLight()
        {
            Light = new RenderLight()
            {
                Kind = LightKind.Directional,
                Direction = new Vector3(0, -1, 0),
                Color = Color3f.White
            }
        });
        src.Lights.Add(new DynamicLight()
        {
            Light = new RenderLight()
            {
                Kind = LightKind.Directional,
                Direction = new Vector3(0, 0, 1),
                Color = Color3f.White
            }
        });
        lighting.Lights.SourceLighting = src;
        lighting.Lights.SourceEnabled[0] = true;
        lighting.Lights.SourceEnabled[1] = true;
        lighting.NumberOfTilesX = -1;
    }
    public static void RenderToPng(GameResourceManager resources, RigidModel model, int width, int height, string outfile)
    {
        //Set up state
        var renderContext = resources.GLWindow.RenderContext;
        var commandBuffer = new CommandBuffer();
        var restoreTarget = renderContext.RenderTarget;
        var renderTarget = new RenderTarget2D(width, height);
        renderContext.RenderTarget = renderTarget;
        renderContext.PushViewport(0,0,width,height);
        renderContext.ClearColor = Color4.Transparent;
        renderContext.ClearAll();
        //Set camera
        var mat = Matrix4x4.CreateFromYawPitchRoll(2.62f, -0.24f, 0);
        var res = Vector3.Transform(new Vector3(0, 0, model.GetRadius() * 2.1f), mat);
        var camera = new LookAtCamera();
        camera.Update(width, height, res, Vector3.Zero);
        renderContext.SetCamera(camera);
        commandBuffer.Camera = camera;
        //Set model
        model.Update(0);
        model.UpdateTransform();
        //Draw
        commandBuffer.StartFrame(renderContext);
        model.DrawBuffer(0, commandBuffer, resources, Matrix4x4.Identity, ref lighting);
        commandBuffer.DrawOpaque(renderContext);
        renderContext.DepthWrite = false;
        commandBuffer.DrawTransparent(renderContext);
        renderContext.DepthWrite = true;
        //Clean state
        renderContext.PopViewport();
        renderContext.RenderTarget = restoreTarget;
        commandBuffer.Dispose();
        //Save to file
        byte[] data = new byte[width * height * 4];
        renderTarget.Texture.GetData(data);
        for (int i = 0; i < data.Length; i += 4)
        {
            //Swap channels
            var x = data[i + 2];
            data[i + 2] = data[i];
            data[i] = x;
        }
        ImageLib.PNG.Save(outfile, width, height, data);
        renderTarget.Dispose();
    }
}