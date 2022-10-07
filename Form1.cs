using FlappyBirdAI;
using FlappyBirdAI.AI;
using FlappyBirdAI.Sensor;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace FlappyBird;

/// <summary>
/// FlappyBirds AI Form.
/// </summary>
public partial class Form1 : Form
{
    #region CONSTANTS
    /// <summary>
    /// How many AI Flappy birds on screen at the same time
    /// </summary>
    private const int c_birds = 24;

    /// <summary>
    /// true - it saves a screenshot of the failure (even in quiet mode) to d:\temp\
    /// </summary>
    private const bool c_takeSnapShotWhenFinalFlappyGoesSplat = false;
    #endregion

    #region STATIC VARIABLES
    /// <summary>
    /// When in silent mode it learns at maximum speed (as it doesn't need to paint the UI).
    /// </summary>
    private static bool s_silentMode = false;
    #endregion

    /// <summary>
    /// Each AI bird is a new "Flappy", this tracks them.
    /// </summary>
    private readonly Dictionary<int, Flappy> flappyBirds = new();

    /// <summary>
    /// Tracks then epoch/generation the learning is on
    /// </summary>
    private int generation = 0;

    /// <summary>
    /// Tracks how many times Flappy has got to the end.
    /// </summary>
    private int numberOfTimesFlappyFinishedGame = 0;

    /// <summary>
    /// ID of the brain we're monitoring 0..NeuralNetwork.s_networks.Count-1.
    /// -1 indicates no brain being monitored.
    /// </summary>
    private int brainMonitor = -1;

    /// <summary>
    /// Tracks the separate form in which we show the Flappy's brain.
    /// </summary>
    private FormFlappyBrain? formFlappyBrain = null;

    /// <summary>
    /// Constructor.
    /// </summary>
    public Form1()
    {
        InitializeComponent();
        
        InitialiseBirdBrains();

        CreateFlappyBirds();

        // from this point, the timer runs and the AI tries to steer the birds.
        timerScroll.Start();
    }

    /// <summary>
    /// Each bird has a neural network. This creates them (random weights/biases).
    /// </summary>
    private static void InitialiseBirdBrains()
    {
        NeuralNetwork.s_networks.Clear();

        for (int i = 0; i < c_birds; i++)
        {
            // inputs: x1 per sensor sample-point + vertical velocity + vertical acceleration
            // hidden: one neuron per input 
            // output: the vertical acceleration to apply to the bird. We'll ignore F=ma and assume "m" of 1 "bird" mass. thus a=F.
            _ = new NeuralNetwork(i, new int[] { PipeSensor.s_samplePoints + 2, PipeSensor.s_samplePoints + 2, 1 }, true);

            // we ignore the output, as they are all tracked in NeuralNetwork.s_networks[]
        }

        // load previous saved network configuration if available (if not available, load is ignored)
        foreach (int n in NeuralNetwork.s_networks.Keys) NeuralNetwork.s_networks[n].Load($@"c:\temp\flappy-{n}.ai");
    }

    /// <summary>
    /// Creates new Flappy one per AI neural network.
    /// </summary>
    private void CreateFlappyBirds()
    {
        flappyBirds.Clear();

        for (int i = 0; i < c_birds; i++)
        {
            flappyBirds.Add(i, new Flappy(i)); // no eggs required.
        }

        ++generation;
      }

    /// <summary>
    /// Mutates the bottom 50%, resets the scenery (new pipes), creates our AI birds.
    /// </summary>
    private void NextGeneration()
    {
        MutateTheBirdBrains();
        ScrollingScenery.Reset();
        CreateFlappyBirds();

        Text = $"Generation {generation} / {numberOfTimesFlappyFinishedGame}"; // rudimentary, but sufficient
    }

    /// <summary>
    /// Timer that fires: Moves birds, knows when to mutate.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TimerScroll_Tick(object sender, EventArgs e)
    {
        if (s_silentMode) timerScroll.Stop(); // in silent mode, we don't need a timer to fire

        int movesBeforeYieldingToWindowsEvents = 0;

        do
        {
            if (ScrollingScenery.Move()) // true = Flappy got to the end of the game
            {
                WriteFlappyTelemetry();
                ++numberOfTimesFlappyFinishedGame;
                NextGeneration();
                continue;
            }

            bool allFlappyBirdsWentSplat = MoveAllTheBirds();

            // when all the birds go splat, the game ends, we need to start again.
            if (allFlappyBirdsWentSplat)
            {
                TakeAScreenshotOfFailure();

                NextGeneration();
                continue;
            }

            // we don't draw anything in silent mode
            if (!s_silentMode)
            {
                DrawBackgroundWithPipesAndAllBirds();
            }
            else
            {
                // we're in silent mode, we still want the app to be responsive, this achieves it
                if (++movesBeforeYieldingToWindowsEvents % 100 == 0) Application.DoEvents();
            }
        } while (s_silentMode); // silent mode is reset via main form key-press
       
        timerScroll.Start();
    }

    /// <summary>
    /// It requires at least one to have completed the game. If enabled it will then
    /// take a screenshot of Flappy failing.
    /// </summary>
    private void TakeAScreenshotOfFailure()
    {
        if (!c_takeSnapShotWhenFinalFlappyGoesSplat) return; // turned off

        if (numberOfTimesFlappyFinishedGame == 0) return; // don't do it in early learning

        // we need to enable these so it draws what's happening.
        Flappy.s_testAndDrawSensor = true;
        Flappy.c_showHitPoints = true;

        DrawBackgroundWithPipesAndAllBirds();
        pictureBoxFlappyGameScreen.Image.Save($@"d:\temp\fail_{generation}.png", ImageFormat.Png);
        
        Flappy.s_testAndDrawSensor = false;
        Flappy.c_showHitPoints = false;
    }

    /// <summary>
    /// Stores the telemtry of birds that succeeded.
    /// </summary>
    private void WriteFlappyTelemetry()
    {
        for (int i = 0; i < c_birds; i++)
        {
            if (!flappyBirds[i].FlappyWentSplat) flappyBirds[i].WriteTelemetryIfEnabled();
        }
    }

    /// <summary>
    /// Creates a new bitmap: adds the scenery, overlays the pipes, and then overlays all the birds.
    /// </summary>
    private void DrawBackgroundWithPipesAndAllBirds()
    {
        Bitmap b = new(pictureBoxFlappyGameScreen.Width, pictureBoxFlappyGameScreen.Height);
        using Graphics graphics = Graphics.FromImage(b);

        ScrollingScenery.Draw(graphics);

        for (int i = 0; i < c_birds; i++)
        {
            // if went splat, remove the monitor
            if (flappyBirds[i].FlappyWentSplat && brainMonitor == flappyBirds[i].Id)
            {
                brainMonitor = -1;
                ShowOrHideBrainVisualiser(); // hide it
            }

            if ((!flappyBirds[i].FlappyWentSplat || flappyBirds[i].VerticalPositionOfFlappyPX < 285) && (brainMonitor == -1 || brainMonitor == flappyBirds[i].Id))
            {
                flappyBirds[i].Draw(graphics); // we don't draw the birds that failed except while the fall.
            }
        }

        // write if paused or speed in center of screen
        string label = !timerScroll.Enabled ? (s_silentMode ? " Q U I E T   M O D E" : "P A U S E D") : ((timerScroll.Interval != 10) ? "S P E E D   x 1/" + ((float)timerScroll.Interval / 10f) : "");

        if (!string.IsNullOrEmpty(label))
        {
            DisplayLabelInCenterOfScreen(graphics, label, Brushes.Black);
        }

        // switch the image over
        pictureBoxFlappyGameScreen.Image?.Dispose();
        pictureBoxFlappyGameScreen.Image = b;

        UpdateProgressAndFlappySplatCount();
    }

    /// <summary>
    /// Writes a "label" (such as "PAUSED") centered in the screen.
    /// </summary>
    /// <param name="g"></param>
    /// <param name="label"></param>
    /// <param name="brush"></param>
    private void DisplayLabelInCenterOfScreen(Graphics g, string label, Brush brush)
    {
        using Font font = new("Courier New", 14);
        
        SizeF s = g.MeasureString(label, font);
        g.DrawString(label, font, brush, pictureBoxFlappyGameScreen.Width/2 - s.Width / 2, pictureBoxFlappyGameScreen.Height/2 - s.Height / 2);
    }

    /// <summary>
    /// Display count and progress in the bottom panel. 
    /// </summary>
    private void UpdateProgressAndFlappySplatCount()
    {
        if (ScrollingScenery.s_pos % 10 != 0) return;

        using Font fontSmall = new("Arial", 7);

        Bitmap bitmap = new(pictureBoxStats.Width, pictureBoxStats.Height);

        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.FromArgb(207, 201, 134));

        DrawProgressBar(graphics);

        DrawSplatStatusIconsForFlappyBirds(graphics);

        graphics.DrawString("[P]ause   [S]ave   [Q]uiet   [D]raw    [F]rame-rate"+(Flappy.s_successfulBrain != -1 ? "    [V]isualiser" : ""), fontSmall, Brushes.Black, new Point(8, 60));

        // switch the image over
        pictureBoxStats.Image?.Dispose();
        pictureBoxStats.Image = bitmap;
    }

    /// <summary>
    /// Draws a simple progress bar so we can see how far the Flappies have gone, and how close to finishing.
    /// </summary>
    /// <param name="graphics"></param>
    private static void DrawProgressBar(Graphics graphics)
    {
        using Pen highlightPen = new(Color.FromArgb(80, 255, 255, 255));
        using Pen shadowPen = new(Color.FromArgb(130, 0, 0, 0));
        using Font fontBig = new("Arial", 9);
             
        graphics.DrawString("Course Completion Progress", fontBig, Brushes.Black, new Point(6, 12));

        // outline 
        graphics.FillRectangle(Brushes.Silver, new Rectangle(9, 28, 201, 11));
        graphics.DrawRectangle(Pens.Black, new Rectangle(9, 28, 201, 11));

        // bar indicating % progress
        float size = (float)ScrollingScenery.s_pos / (float)ScrollingScenery.c_numberOfPixelsScrolledBeforeReachingTheEndOfGame;

        graphics.FillRectangle(Brushes.SteelBlue, new Rectangle(10, 29, (int)(size * 199), 9));

        graphics.DrawLine(Pens.Black, (int)(size * 199) + 10, 29, (int)(size * 199) + 10, 38);
        graphics.DrawLine(Pens.Silver, (int)(size * 199) + 11, 29, (int)(size * 199) + 11, 38);

        // shadow + highlight around box
        graphics.DrawLine(highlightPen, 10, 29, 209, 29);
        graphics.DrawLine(highlightPen, 10, 29, 10, 37);
        graphics.DrawLine(shadowPen, 10, 38, 209, 38);
        graphics.DrawLine(shadowPen, 209, 29, 209, 38);
    }

    /// <summary>
    /// Draws each bird icon, indicating status of flappy (grey if gone splat).
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawSplatStatusIconsForFlappyBirds(Graphics graphics)
    {
        const int c_leftMostPositionOfIconPX = 235;

        int xPosOfICON = c_leftMostPositionOfIconPX;
        int yPosOfICON = 5;

        for (int birdIndex = 0; birdIndex < c_birds; birdIndex++)
        {
            Bitmap icon = flappyBirds[birdIndex].GetIcon();

            // move to next row? Advance down
            if (xPosOfICON > pictureBoxStats.Width - 30)
            {
                xPosOfICON = c_leftMostPositionOfIconPX;
                yPosOfICON += icon.Height + 2;
            }

            graphics.DrawImageUnscaled(icon, xPosOfICON, yPosOfICON);

            // position next icon to the right
            xPosOfICON += icon.Width + 1;
        }
    }

    /// <summary>
    /// Moves each of the birds.
    /// </summary>
    /// <returns></returns>
    private bool MoveAllTheBirds()
    {
        bool allFlappyBirdsWentSplat = true;

        // unlike most of my AI apps, I haven't use Parallel.ForEach() because to make it work requires the bitmaps to allow concurrency; just not worth it.
        for (int i = 0; i < c_birds; i++)
        {
            flappyBirds[i].Move(); // using the AI bird brain, of course

            if (!flappyBirds[i].FlappyWentSplat) allFlappyBirdsWentSplat = false; // we found one that hasn't gone splat.
        }

        if(!s_silentMode && !allFlappyBirdsWentSplat)
        {
            if (brainMonitor != -1) formFlappyBrain?.Visualise(flappyBirds[brainMonitor]);
        }

        return allFlappyBirdsWentSplat; // if all went splat, we'll mutate (worst performing 50%) and try again.
    }

    /// <summary>
    /// We have a few useful keys to detect.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Form1_KeyDown(object sender, KeyEventArgs e)
    {
        // [P] toggle: pauses game
        if (e.KeyCode == Keys.P)
        {
            timerScroll.Enabled = !timerScroll.Enabled;
            DrawBackgroundWithPipesAndAllBirds();
        }

        // [Q] toggle: silent/quiet mode (runs faster)
        if (e.KeyCode == Keys.Q) s_silentMode = !s_silentMode;

        // [S] saves the neural network
        if (e.KeyCode == Keys.S) SaveFlappyBirdBrains();

        // [D] toggle: draw sensor
        if (e.KeyCode == Keys.D) Flappy.s_testAndDrawSensor = !Flappy.s_testAndDrawSensor;

        // [V] toggle: Flappy brain visualiser
        if (e.KeyCode == Keys.V && Flappy.s_successfulBrain != -1) ShowOrHideBrainVisualiser();

        // [V] toggle: Flappy brain visualiser
        if (e.KeyCode == Keys.F) ToggleFrameSpeed();

    }

    /// <summary>
    /// cycle thru frame speeds.
    /// </summary>
    private void ToggleFrameSpeed()
    {
        timerScroll.Interval = timerScroll.Interval switch
        {
            10 => 100,
            100 => 500,
            500 => 1000,
            1000 => 5000,
            _ => 10,
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void ShowOrHideBrainVisualiser()
    {
        if(formFlappyBrain is not null)
        {
            formFlappyBrain.Close();
            formFlappyBrain.Dispose();
            brainMonitor = -1;

            formFlappyBrain = null;
            return;
        }

        if (Flappy.s_successfulBrain == -1) return;

        brainMonitor = Flappy.s_successfulBrain;

        formFlappyBrain = new FormFlappyBrain();
        
        formFlappyBrain.Show();
        formFlappyBrain.Left = this.Left + this.Width + 5;
        formFlappyBrain.Top = this.Top;
    }

    /// <summary>
    /// Saves all the Flappy birds AI neural networks to individual files.
    /// </summary>
    private static void SaveFlappyBirdBrains()
    {
        foreach (int n in NeuralNetwork.s_networks.Keys)
        {
            NeuralNetwork.s_networks[n].Save($@"c:\temp\flappy-{n}.ai");
        }
    }
    
    /// <summary>
    /// Our goal: Rank the Flappy birds based on how far they got (further = better), then discard the bottom 50%. 
    ///           Make new cohorts to replace them based off the top 50%. (Clone the brain, tweak a little)
    /// </summary>
    private void MutateTheBirdBrains()
    {
        // update networks fitness for each Flappy bird
        foreach (Flappy f in flappyBirds.Values) f.UpdateFitnessOfBirdBrainBasedOnPipesAvoided();

        NeuralNetwork.SortNetworkByFitness(); // largest "fitness" (best performing) goes to the bottom

        // sorting is great but index no longer matches the "id". 
        // this is because the sort swaps but this misaligns id with the entry            
        List<NeuralNetwork> n = new();
        foreach (int i in NeuralNetwork.s_networks.Keys) n.Add(NeuralNetwork.s_networks[i]);

        NeuralNetwork[] array = n.ToArray();

        // replace the 50% worse offenders with the best, then mutate them.
        // we do this by copying top half (lowest fitness) with top half.
        for (int worstNeuralNetworkIndex = 0; worstNeuralNetworkIndex < c_birds / 2; worstNeuralNetworkIndex++)
        {
            // 50..100 (in 100 neural networks) are in the top performing
            int neuralNetworkToCloneFromIndex = worstNeuralNetworkIndex + c_birds / 2; // +50% -> top 50% 

            NeuralNetwork.CopyFromTo(array[neuralNetworkToCloneFromIndex], array[worstNeuralNetworkIndex]); 

            array[worstNeuralNetworkIndex].Mutate(25, 0.5F); // mutate 25% chance, as much as 0.5f
        }

        // unsort, restoring the order of birds to neural network i.e [x]=id of "x".
        Dictionary<int, NeuralNetwork> unsortedNetworksDictionary = new();

        for (int i = 0; i < c_birds; i++)
        {
            var neuralNetwork = NeuralNetwork.s_networks[i];

            unsortedNetworksDictionary[neuralNetwork.Id] = neuralNetwork;
        }

        NeuralNetwork.s_networks = unsortedNetworksDictionary;
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        s_silentMode = false;
    }
}