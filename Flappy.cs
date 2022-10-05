using FlappyBirdAI.AI;
using FlappyBirdAI.Sensor;
using FlappyBirdAI.Utils;

namespace FlappyBird;

/// <summary>
/// Represents a "Flappy" bird.
/// </summary>
internal class Flappy
{
    #region STATIC properties that apply to all Flappies.
    /// <summary>
    /// true - it shows lines indicating sensor of pipes, and draws a box around the pipe.
    /// </summary>
    internal static bool s_testAndDrawSensor = false;

    /// <summary>
    /// When true, it draws the hit point dots around the bird.
    /// </summary>
    internal static bool c_showHitPoints = false;
    
    /// <summary>
    /// Width of a "Flappy" bird.
    /// </summary>
    internal static int s_idthOfAFlappyBirdPX = 0;

    /// <summary>
    /// Height of a "Flappy" bird.
    /// </summary>
    internal static int s_HeightOfAFlappyBirdPX = 0;

    /// <summary>
    /// The 3 frames simulating the bird / flapping.
    /// </summary>
    internal readonly static Bitmap[] s_FlappyImageFrames;

    /// <summary>
    /// A grey frame indicating it went splat.
    /// </summary>
    private readonly static Bitmap s_flappySplatFrame;

    /// <summary>
    /// Similar to the "splat" but smaller (for showing which are playing and those that went splat).
    /// </summary>
    private readonly static Bitmap s_flappySplatFrameIcon;

    /// <summary>
    /// Similar to the "flying frame" but smaller (for showing which are playing and those that went splat).
    /// </summary>
    private readonly static Bitmap s_flappyFrameIcon;

    /// <summary>
    /// This is one of most successful brains. Given they all share the same horizontal position there is no clear winner per se,
    /// all the complete the course are successful.
    /// -1 = no successful brain yet. Typically for the first generations that are learning i.e. haven't completed the course.
    /// </summary>
    internal static int s_successfulBrain = -1;

    /// <summary>
    /// It's sometimes helpful to know where Flappy should have gone thru.
    /// </summary>
    internal static Rectangle s_debugRectangeContainingRegionFlappyIsSupposedToFlyThru = new();
    #endregion

    /// <summary>
    /// Where flappy is horizontally (offset from left of screen).
    /// </summary>
    internal float HorizontalPositionOfFlappyPX;

    /// <summary>
    /// Where Flappy is in the sky (0=top of screen).
    /// </summary>
    internal float VerticalPositionOfFlappyPX;

    /// <summary>
    /// Track of score
    /// </summary>
    internal int Score = 0;

    /// <summary>
    /// Identifier for this bird. It's the link to the bird-brain (neural network). We destroy birds, not the bird-brains.
    /// </summary>
    internal int Id;

    /// <summary>
    /// true = hit pipe -> game-over (we don't do lives for AI training).
    /// false = in game.
    /// </summary>
    internal bool FlappyWentSplat;

    /// <summary>
    /// How fast Flappy is moving in a vertical direction.
    /// </summary>
    private float verticalSpeed;

    /// <summary>
    /// How fast Flappy is accelerating up or down (or not at all).
    /// </summary>
    private float verticalAcceleration;

    /// <summary>
    /// 1=steady flight, 0..1..2=simulated flappping.
    /// </summary>
    private int wingFlappingAnimationFrame = 1;

    /// <summary>
    /// Provides a "sensor", used by AI to detect pipes.
    /// </summary>
    private readonly PipeSensor sensor = new();

    /// <summary>
    /// Tracks telemetry. This only records _id_ enabled in the Telemetry class.
    /// </summary>
    private readonly Telemetry telemetry;

    /// <summary>
    /// Constructor.
    /// </summary>
    internal Flappy(int id)
    {
        HorizontalPositionOfFlappyPX = 10;
        VerticalPositionOfFlappyPX = 100;
        FlappyWentSplat = false;
        Id = id;

        telemetry = new(this);
    }

    internal string GetRawData()
    {
        return $"Id: {Id}\nPosition: ({HorizontalPositionOfFlappyPX},{Math.Round(VerticalPositionOfFlappyPX)})\nV-speed: {verticalSpeed}\nV-accel: {verticalAcceleration}\n";
    }

    /// <summary>
    /// Cache images that apply to all instances of Flappy.
    /// </summary>
    static Flappy()
    {
        s_flappySplatFrame = new(@"images\FlappyFrameFail.png");
        s_flappySplatFrame.MakeTransparent();

        List<Bitmap> flappyImageFrameList = new()
        {
            // load images for Flappy to save us having to paint pixels.
            new(@"images\FlappyFrame1.png"),
            new(@"images\FlappyFrame2.png"),
            new(@"images\FlappyFrame3.png")
        };

        // we need use the array to step thru
        s_FlappyImageFrames = flappyImageFrameList.ToArray();

        // make the 3 images transparent (green part around the outside of Flappy)
        s_FlappyImageFrames[0].MakeTransparent();
        s_FlappyImageFrames[1].MakeTransparent();
        s_FlappyImageFrames[2].MakeTransparent();

        s_flappySplatFrameIcon = FlappyIcon(s_flappySplatFrame);
        s_flappyFrameIcon = FlappyIcon(flappyImageFrameList[2]);

        // width & height of a Flappy
        s_idthOfAFlappyBirdPX = s_FlappyImageFrames[0].Width;
        s_HeightOfAFlappyBirdPX = s_FlappyImageFrames[0].Height;
    }

    /// <summary>
    /// Make a Flappy icon.
    /// </summary>
    /// <param name="s_flappySplatFrame"></param>
    /// <returns></returns>
    private static Bitmap FlappyIcon(Bitmap s_flappySplatFrame)
    {
        Bitmap icon = new(18, 12); 

        using Graphics g = Graphics.FromImage(icon);
        g.DrawImage(s_flappySplatFrame, 0, 0, icon.Width, icon.Height);
        g.Flush();
        
        icon.MakeTransparent();

        return icon;
    }

    /// <summary>
    /// Returns an "icon" for Flappy (a smaller image).
    /// </summary>
    /// <returns></returns>
    internal Bitmap GetIcon()
    {
        return FlappyWentSplat ? s_flappySplatFrameIcon : s_flappyFrameIcon;
    }

    /// <summary>
    /// Draws an animated "Flappy".
    /// </summary>
    /// <param name="graphics"></param>
    internal void Draw(Graphics graphics)
    {
        // draw Flappy (bird)
        graphics.DrawImageUnscaled(FlappyWentSplat ? s_flappySplatFrame : s_FlappyImageFrames[wingFlappingAnimationFrame], (int)HorizontalPositionOfFlappyPX, (int)(VerticalPositionOfFlappyPX - 10));

        // if flapping, toggle frames to simulate, else keep wings still
        if (verticalAcceleration > 0) wingFlappingAnimationFrame = (wingFlappingAnimationFrame + 1) % s_FlappyImageFrames.Length; else wingFlappingAnimationFrame = 1;

        // if user enabled "Draw" using keyboard [D], we draw our pipe sensors
        if (s_testAndDrawSensor) DrawPipeSensors(graphics);

        if (c_showHitPoints) foreach (Point hp in ScrollingScenery.HitTestPoints) graphics.DrawEllipse(Pens.Cyan, hp.X + HorizontalPositionOfFlappyPX - 1, hp.Y + VerticalPositionOfFlappyPX - 11, 1, 1);

        if (s_debugRectangeContainingRegionFlappyIsSupposedToFlyThru.Top != 0 || s_debugRectangeContainingRegionFlappyIsSupposedToFlyThru.Left != 0)
        {
            using SolidBrush safearea = new(Color.FromArgb(100, 255, 0, 0));
            graphics.FillRectangle(safearea, s_debugRectangeContainingRegionFlappyIsSupposedToFlyThru);
        }
    }

    /// <summary>
    /// Draws a dotted ray from bird to pipes detected by proximity sensors.
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawPipeSensors(Graphics graphics)
    {
        List<Rectangle> r = ScrollingScenery.GetClosestPipes(this, 300);

        sensor.Read(r, new PointF(HorizontalPositionOfFlappyPX + 14, VerticalPositionOfFlappyPX), out double[] _);

        foreach (Rectangle rect in r) graphics.DrawRectangle(Pens.White, rect);

        sensor.DrawWhereTargetIsInRespectToSweepOfHeatSensor(graphics);
    }

    /// <summary>
    /// Move Flappy.
    /// </summary>
    internal void Move()
    {
        if (!FlappyWentSplat) UseAItoMoveFlappy();

        // Flappy does not have an anti-gravity module. So we apply gravity acceleration (-). It's not "9.81" because we
        // don't know how much Flappy weighs nor wing thrust ratio. 
        verticalAcceleration -= 0.001f;

        // Prevent Flappy falling too fast. Too early for terminal velocity, but makes the game "playable">
        if (verticalAcceleration < -1) verticalAcceleration = -1;

        // Apply acceleration to the speed.
        verticalSpeed -= verticalAcceleration;

        // Ensure Flappy doesn't go too quick (makes it unplayable).
        verticalSpeed = verticalSpeed.Clamp(-2, 3);

        VerticalPositionOfFlappyPX += verticalSpeed;

        // Stop Flappy going off screen (top/bottom).
        VerticalPositionOfFlappyPX = VerticalPositionOfFlappyPX.Clamp(0, 285);

        if (FlappyWentSplat) return; // no collision detection required.

        // if bird collided with the pipe, we set a flag (prevents control), and ensure it falls out the sky.
        if (ScrollingScenery.FlappyCollidedWithPipe(this, out s_debugRectangeContainingRegionFlappyIsSupposedToFlyThru))
        {
            FlappyWentSplatAgainstPipe();
        }
    }

    /// <summary>
    /// Flappy went splat against a pipe: Arrest upward velocity, so it falls.
    /// </summary>
    private void FlappyWentSplatAgainstPipe()
    {
        verticalAcceleration = 0;
        verticalSpeed = 0;
        FlappyWentSplat = true;
        
        Score = ScrollingScenery.s_pos; // how far
    }

    /// <summary>
    /// Provide AI with the sensor outputs, and have it provide a vertical acceleration.
    /// </summary>
    private void UseAItoMoveFlappy()
    {
        List<Rectangle> rectanglesIndicatingWherePipesWerePresent = ScrollingScenery.GetClosestPipes(this, 300);

        // sensors around Flappy detect a pipe
        sensor.Read(rectanglesIndicatingWherePipesWerePresent, new PointF(HorizontalPositionOfFlappyPX + 14, VerticalPositionOfFlappyPX), out double[] proximitySensorRegionsOutput);

        // we supplement with existing speed and acceleration
        List<double> d = new(proximitySensorRegionsOutput)
            {
                verticalSpeed / 3,
                verticalAcceleration / 3
            };

        double[] outputFromNeuralNetwork = NeuralNetwork.s_networks[Id].FeedForward(d.ToArray()); // process inputs

        // neural network provides "thrust"/"wing flapping" to give acceleration up or down.
        verticalAcceleration += (float)outputFromNeuralNetwork[0];

        // if enabled, we track telemetry and write it for birds that complete the course.
        telemetry.Record(d, outputFromNeuralNetwork[0], rectanglesIndicatingWherePipesWerePresent);
    }
   

    /// <summary>
    /// We rank bird brains, not birds. This transfers the score (number of pipes it went thru) as a score for ranking the bird-brain.
    /// </summary>
    internal void UpdateFitnessOfBirdBrainBasedOnPipesAvoided()
    {
        NeuralNetwork.s_networks[Id].Fitness = Score;
    }

    /// <summary>
    /// Writes the telemetry to a file if enabled.
    /// </summary>
    internal void WriteTelemetryIfEnabled()
    {
        s_successfulBrain = Id;

        telemetry.WriteTelemetryIfEnabled();

        File.WriteAllText($@"c:\temp\formula based on {Id} without AI.txt", NeuralNetwork.s_networks[Id].Formula());
    }
}