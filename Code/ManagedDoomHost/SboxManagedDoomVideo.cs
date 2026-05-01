namespace Sandbox;

public sealed class SboxManagedDoomVideo : ManagedDoom.Video.IVideo
{
    private readonly ManagedDoom.Video.Renderer renderer;
    private readonly byte[] rgbaColumnMajor;
    private readonly byte[] rgbaRowMajor;
    private readonly Texture frameTexture;
    private int frameVersion;

    public SboxManagedDoomVideo(ManagedDoom.Config config, ManagedDoom.GameContent content)
    {
        renderer = new ManagedDoom.Video.Renderer(config, content);
        rgbaColumnMajor = new byte[4 * renderer.Width * renderer.Height];
        rgbaRowMajor = new byte[4 * renderer.Width * renderer.Height];
        frameTexture = Texture.Create( renderer.Width, renderer.Height, ImageFormat.RGBA8888 )
            .WithDynamicUsage()
            .Finish();
    }

    public void Render(ManagedDoom.Doom doom, ManagedDoom.Fixed frameFrac)
    {
        renderer.Render(doom, rgbaColumnMajor, frameFrac);
        TransposeColumnMajorToRowMajor();
        frameTexture.Update( rgbaRowMajor, 0, 0, renderer.Width, renderer.Height );
        frameVersion++;
    }

    public void InitializeWipe()
    {
        renderer.InitializeWipe();
    }

    public bool HasFocus()
    {
        return true;
    }

    public int MaxWindowSize => renderer.MaxWindowSize;

    public int WindowSize
    {
        get => renderer.WindowSize;
        set => renderer.WindowSize = value;
    }

    public bool DisplayMessage
    {
        get => renderer.DisplayMessage;
        set => renderer.DisplayMessage = value;
    }

    public int MaxGammaCorrectionLevel => renderer.MaxGammaCorrectionLevel;

    public int GammaCorrectionLevel
    {
        get => renderer.GammaCorrectionLevel;
        set => renderer.GammaCorrectionLevel = value;
    }

    public int WipeBandCount => renderer.WipeBandCount;
    public int WipeHeight => renderer.WipeHeight;

    public int Width => renderer.Width;
    public int Height => renderer.Height;
    public Texture FrameTexture => frameTexture;
    public int FrameVersion => frameVersion;
    public System.Action<ManagedDoom.Doom, ManagedDoom.Video.DrawScreen> OverlayDrawer
    {
        get => renderer.OverlayDrawer;
        set => renderer.OverlayDrawer = value;
    }

    private void TransposeColumnMajorToRowMajor()
    {
        var width = renderer.Width;
        var height = renderer.Height;

        for ( var x = 0; x < width; x++ )
        {
            for ( var y = 0; y < height; y++ )
            {
                var src = 4 * ( height * x + y );
                var dst = 4 * ( width * y + x );

                rgbaRowMajor[dst] = rgbaColumnMajor[src];
                rgbaRowMajor[dst + 1] = rgbaColumnMajor[src + 1];
                rgbaRowMajor[dst + 2] = rgbaColumnMajor[src + 2];
                rgbaRowMajor[dst + 3] = rgbaColumnMajor[src + 3];
            }
        }
    }
}
