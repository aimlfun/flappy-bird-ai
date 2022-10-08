#define USING_AI
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
        if (!FlappyWentSplat)
        {
#if USING_AI
            UseAItoMoveFlappy();
#else
            UseFormulaToMoveFlappy();
#endif
        }
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
    }

#if !USING_AI
    /// <summary>
    /// Flappy without AI.
    /// </summary>
    private void UseFormulaToMoveFlappy()
    {
        List<Rectangle> rectanglesIndicatingWherePipesWerePresent = ScrollingScenery.GetClosestPipes(this, 300);
        // sensors around Flappy detect a pipe
        sensor.Read(rectanglesIndicatingWherePipesWerePresent, new PointF(HorizontalPositionOfFlappyPX + 14, VerticalPositionOfFlappyPX), out double[] proximitySensorRegionsOutput);

        // we supplement with existing speed and acceleration
        List<double> arrayd = new(proximitySensorRegionsOutput)
       {
          verticalSpeed / 3,
          verticalAcceleration / 3
        };
            double outputFromNeuralNetwork = Math.Tanh(
        (-0.9988000106532127 * Math.Tanh(
        (-1.057900033891201 * arrayd[0]) +
        (-0.2867500651627779 * arrayd[1]) +
        (0.8420499847270548 * arrayd[2]) +
        (1.5785999735817313 * arrayd[3]) +
        (1.1670000134035945 * arrayd[4]) +
        (-0.20890004152897745 * arrayd[5]) +
        (-0.5317500188248232 * arrayd[6]) +
        (-2.2370999874547124 * arrayd[7]) +
        (-0.29650002066046 * arrayd[8]) +

        -0.7375999917276204)) +
        (0.7578500248491764 * Math.Tanh(
        (0.3652000145521015 * arrayd[0]) +
        (-0.6132000106736086 * arrayd[1]) +
        (-0.12814998719841242 * arrayd[2]) +
        (-1.6254000074695796 * arrayd[3]) +
        (0.12840002123266459 * arrayd[4]) +
        (-0.17580002709291875 * arrayd[5]) +
        (0.07210002583451569 * arrayd[6]) +
        (-0.1198500216123648 * arrayd[7]) +
        (-1.9426499353721738 * arrayd[8]) +

        -1.236149983247742)) +
        (2.0082500012940727 * Math.Tanh(
        (-1.4253999553620815 * arrayd[0]) +
        (1.1117999972775578 * arrayd[1]) +
        (-0.6991000338457525 * arrayd[2]) +
        (1.4481000099913217 * arrayd[3]) +
        (0.7332999855279922 * arrayd[4]) +
        (0.47010000236332417 * arrayd[5]) +
        (-0.2587499369401485 * arrayd[6]) +
        (1.9267000164836645 * arrayd[7]) +
        (-1.8759499847656116 * arrayd[8]) +

        -0.1334499849472195)) +
        (-0.5514500066637993 * Math.Tanh(
        (-1.5925500185694546 * arrayd[0]) +
        (-0.8255499922670424 * arrayd[1]) +
        (0.7761000087484717 * arrayd[2]) +
        (0.6142999920994043 * arrayd[3]) +
        (-0.9090000055730343 * arrayd[4]) +
        (1.1578999997582287 * arrayd[5]) +
        (0.26390002598054707 * arrayd[6]) +
        (0.36634998384397477 * arrayd[7]) +
        (0.4955500189680606 * arrayd[8]) +

        -0.27695000474341214)) +
        (0.06589999049901962 * Math.Tanh(
        (0.21245002443902194 * arrayd[0]) +
        (0.25369999720714986 * arrayd[1]) +
        (-0.5360500463284552 * arrayd[2]) +
        (-0.18174999201437458 * arrayd[3]) +
        (-0.5732500264421105 * arrayd[4]) +
        (-0.2005500253289938 * arrayd[5]) +
        (-1.7122000206145458 * arrayd[6]) +
        (0.9294499862007797 * arrayd[7]) +
        (-1.788049994269386 * arrayd[8]) +

        -0.48239994142204523)) +
        (-0.5051000055391341 * Math.Tanh(
        (0.5047999820671976 * arrayd[0]) +
        (-1.0102000199258327 * arrayd[1]) +
        (-0.7457999978214502 * arrayd[2]) +
        (-0.9040000076638535 * arrayd[3]) +
        (-0.12569999897823436 * arrayd[4]) +
        (0.7401999970898032 * arrayd[5]) +
        (-1.6305499924346805 * arrayd[6]) +
        (-1.2936499833595008 * arrayd[7]) +
        (-0.0008999996935017407 * arrayd[8]) +

        -1.4018999887630343)) +
        (0.505500023951754 * Math.Tanh(
        (-0.9191999947652221 * arrayd[0]) +
        (0.3889500219374895 * arrayd[1]) +
        (-1.6522000096738338 * arrayd[2]) +
        (2.050000006565824 * arrayd[3]) +
        (-2.0614000409841537 * arrayd[4]) +
        (-0.11570001346990466 * arrayd[5]) +
        (0.2827000077813864 * arrayd[6]) +
        (-0.3508000150322914 * arrayd[7]) +
        (0.9331500136759132 * arrayd[8]) +

        +0.4791499625425786)) +
        (1.1332500383905426 * Math.Tanh(
        (-1.3275499949231744 * arrayd[0]) +
        (0.10255000204779208 * arrayd[1]) +
        (-0.1730999993160367 * arrayd[2]) +
        (-0.7827000010001939 * arrayd[3]) +
        (0.9183999933302402 * arrayd[4]) +
        (1.8512000192713458 * arrayd[5]) +
        (0.11984998924890533 * arrayd[6]) +
        (-0.5924999928101897 * arrayd[7]) +
        (1.139050024212338 * arrayd[8]) +

        -1.5733500104397535)) +
        (-0.5695499941357411 * Math.Tanh(
        (1.2554000343661755 * arrayd[0]) +
        (0.3267000149935484 * arrayd[1]) +
        (0.7267500003799796 * arrayd[2]) +
        (-0.8972499901428819 * arrayd[3]) +
        (-0.7791500145758619 * arrayd[4]) +
        (-0.478449996560812 * arrayd[5]) +
        (-0.2854499854147434 * arrayd[6]) +
        (1.1654999999445863 * arrayd[7]) +
        (0.5919499471783638 * arrayd[8]) +

        +0.8333999977912754)) +

        -0.0662000086158514);

        // neural network provides thrust/wing flapping to give acceleration up or down.
        verticalAcceleration += (float)outputFromNeuralNetwork;

        // if enabled, we track telemetry and write it for birds that complete the course.
        telemetry.Record(arrayd, outputFromNeuralNetwork, rectanglesIndicatingWherePipesWerePresent);
    }
#else

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
#endif

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