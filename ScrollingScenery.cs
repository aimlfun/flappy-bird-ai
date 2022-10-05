using System.Drawing;
using System.Security.Cryptography;

namespace FlappyBird;

// Images courtesy of: https://www.kindpng.com/downpng/iimbxmh_atlas-png-flappy-bird-transparent-png/
// "kindpng_1842511.png" was split into the parts. The chevron edges had to be adjusted, as do the
// Flappy bird.

internal class ScrollingScenery
{
    /// <summary>
    /// To exercise Flappy, we put pipes for 20,000 pixels.
    /// </summary>
    internal const int c_numberOfPixelsScrolledBeforeReachingTheEndOfGame = 20000;

    /// <summary>
    /// Parallax scrolling: background city moves slower. So we track where we are on 
    /// the background here.
    /// </summary>
    internal static int s_posBackground = 0;

    /// <summary>
    /// Parallax scrolling: chevron + pipes move independent of the background. This tracks where
    /// we are in the game (and thus where we draw the pipes).
    /// </summary>
    internal static int s_pos = 0;

    /// <summary>
    /// Width of the playable game area.
    /// </summary>
    private readonly static int s_widthOfGameArea;

    /// <summary>
    /// Height of the playable game area.
    /// </summary>
    private readonly static int s_heightOfGameArea;

    /// <summary>
    /// This is super inefficient. Image from above URL. More efficient would probably be to have a thin
    /// Chevron bitmap, and fill a rectangle the ground colour.
    /// </summary>
    private static Bitmap s_bitmapChevronFloor;

    /// <summary>
    /// This is super inefficient. Image from above URL. More efficient would probably be to have a layer 
    /// for buildings, and fill a rectangle for the sky.
    /// </summary>
    private static Bitmap s_bitmapSlowMovingBuildings;

    /// <summary>
    /// This is a pipe facing up (fat end at the top).
    /// </summary>
    private static Bitmap s_upPipe;

    /// <summary>
    /// This is a pipe facing down (fat end at the bottom).
    /// </summary>
    private static Bitmap s_downPipe;

    /// <summary>
    /// We make a list of pipe locations (left edge, and center).
    /// </summary>
    private static List<Point> s_pointsWhereRandomPipesAppearInTheScenery;

    /// <summary>
    /// These are the points around the bird used to detect collisions/
    /// </summary>
    internal static Point[] HitTestPoints = new Point[]
    {
        new Point(1,8),
        new Point(14,0),
        new Point(26,11),
        new Point(13,18),
        new Point(23,16),
        new Point(21,3),
        new Point(8,2),
        new Point(4,15),
        new Point(9,2),
        new Point(23,8),
        new Point(25,15),
        new Point(17,17),
        new Point(7,16),
        new Point(2,12),
        new Point(3,5)
    };

    /// <summary>
    /// Constructor.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable fields ARE initialised in the methods it calls!
    static ScrollingScenery()
#pragma warning restore CS8618 // Non-nullable fields ARE initialised in the methods it calls!
    {
        // store dimensions
        s_widthOfGameArea = 559;
        s_heightOfGameArea = 305;

        if (s_upPipe is null)
        {
            InitialiseImages(s_widthOfGameArea);

            CreateRandomPipes();
        }
    }

    /// <summary>
    /// Loads the images, and applies transparency.
    /// </summary>
    /// <param name="width"></param>
    private static void InitialiseImages(int width)
    {
        s_upPipe = new(@"images\pipeUP.png");
        s_upPipe.MakeTransparent();

        s_downPipe = new(@"images\pipeDOWN.png");
        s_downPipe.MakeTransparent();

        CreateScrollingChevronAreaImage(width);

        CreateScrollingBuildingsWithSkyImage(width);
    }

    /// <summary>
    /// Creates a large enough scrolling bitmap we can blit as we scroll.
    /// Part of the parallax, this is the floor the pipes scroll with.
    /// </summary>
    /// <param name="width"></param>
    /// <returns></returns>
    private static void CreateScrollingChevronAreaImage(int width)
    {
        // make the "scrolling" brown area into twice the size of the screen
        using Bitmap bitmapBottom = new(@"images\chevronGround.png");

        s_bitmapChevronFloor = new(width * 2, bitmapBottom.Height);
        
        using Graphics graphics = Graphics.FromImage(s_bitmapChevronFloor);

        for (int x = 0; x <= s_bitmapChevronFloor.Width; x += bitmapBottom.Width)
        {
            graphics.DrawImageUnscaled(bitmapBottom, x, 0);
        }
    }

    /// <summary>
    /// Creates a large enough scrolling bitmap that we can blit as we slowly scroll.
    /// It contains sky (you cannot tell its scrolling) and the buildings.
    /// </summary>
    /// <param name="width"></param>
    private static void CreateScrollingBuildingsWithSkyImage(int width)
    {
        // make the "scrolling" buildings area into twice the size of the screen.
        using Bitmap bitmapBackground = new(@"images\buildingsAndSky.png");

        s_bitmapSlowMovingBuildings = new Bitmap(width * 2, bitmapBackground.Height);

        using Graphics graphics = Graphics.FromImage(s_bitmapSlowMovingBuildings);

        for (int x = 0; x <= s_bitmapSlowMovingBuildings.Width; x += bitmapBackground.Width)
        {
            graphics.DrawImageUnscaled(bitmapBackground, x, 0);
        }
    }

    /// <summary>
    /// Creates the pipes. They are random vertically, and get closer as the Flappy progresses.
    /// </summary>
    private static void CreateRandomPipes()
    {
        int height = 399 - 140;

        s_pointsWhereRandomPipesAppearInTheScenery = new();

        int horizontalDistanceBetweenPipes = 110;

        int lastY = height / 2;

        // 300 ensures Flappy gets a "run up"
        for (int i = 300; i < c_numberOfPixelsScrolledBeforeReachingTheEndOfGame; i += RandomNumberGenerator.GetInt32(94, 124) + horizontalDistanceBetweenPipes)
        {
            if (horizontalDistanceBetweenPipes > 0) horizontalDistanceBetweenPipes -= 3; // reduces the distance between pipes to make the level harder as it progresses

            // the initial design was pick a random point 0..295, but what can happen is two consecutive points at 0 and 295, and there is no hope
            // of Flappy being able to mvoe up or down quick enough (i.e. a guaranteed lose).
            // So we restrict the difference in height to something practical.

            /*    |  |   |  |           0
             *    ====   |  |
             *           |  |   lastY  ---                  if newy > lasty and newy-lasty > 100 then newy = lasty+100-random(20);
             *    ====   |  |           :
             *    |  |   |  |           :   > 100?
             *    |  |   ====           :
             *    |  |          newY   ---
             *    |  |   ====
             *    |  |   |  |          399
             */
            int newY = RandomNumberGenerator.GetInt32(30, height);

            // ensure the gap between pipes isn't too excessive.
            if (newY > lastY && newY - lastY > 100) 
                newY = lastY + 100 - RandomNumberGenerator.GetInt32(0, 17);
            else
            if (newY < lastY && lastY - newY > 100)
                newY = lastY - 100 + RandomNumberGenerator.GetInt32(0, 17);

            s_pointsWhereRandomPipesAppearInTheScenery.Add(new Point(i, newY));

            lastY = newY;
        }
    }

    /// <summary>
    /// Draws the parallax scrolling scenery including pipes, but not Flappy.
    /// </summary>
    /// <param name="graphics"></param>
    internal static void Draw(Graphics graphics)
    {
        graphics.Clear(Color.FromArgb(78, 192, 202));

        // parallax scrolling, these two actually move at different rates

        // the top is SLOWER scrolling
        graphics.DrawImageUnscaled(s_bitmapSlowMovingBuildings, -(s_posBackground % 225), 205);

        DrawVisiblePipes(graphics);

        // the bottom is FASTER scrolling.
        graphics.DrawImageUnscaled(s_bitmapChevronFloor, -(s_pos % 264), 293);
    }

    /// <summary>
    /// Moves the scenery to the next point (progress game)
    /// </summary>
    /// <returns>true when Flappy has reached the end.</returns>
    internal static bool Move()
    {
        s_pos++;

        // Parallax scrolling achieved by moving background every 4.
        if (s_pos % 4 == 0) ++s_posBackground;

        return s_pos > c_numberOfPixelsScrolledBeforeReachingTheEndOfGame;
    }

    /// <summary>
    /// Draws all the visible pipes. 
    /// </summary>
    /// <param name="graphics"></param>
    private static void DrawVisiblePipes(Graphics graphics)
    {
        int leftEdgeOfScreenTakingIntoAccountScrolling = s_pos;
        int rightEdgeOfScreenTakingIntoAccountScrolling = s_pos + s_widthOfGameArea;
        int pipeWidth = s_upPipe.Width;
        int pipeHeight = s_downPipe.Height;

        foreach (Point p in s_pointsWhereRandomPipesAppearInTheScenery)
        {
            if (p.X > rightEdgeOfScreenTakingIntoAccountScrolling) break; // offscreen to the right, no need to check for more

            if (p.X < leftEdgeOfScreenTakingIntoAccountScrolling - pipeWidth) continue; // offscreen to the left

            // draw the top pipe
            graphics.DrawImageUnscaled(s_downPipe, p.X - leftEdgeOfScreenTakingIntoAccountScrolling, p.Y - pipeHeight - 40);

            // draw the bottom pipe
            graphics.DrawImageUnscaled(s_upPipe, p.X - leftEdgeOfScreenTakingIntoAccountScrolling, p.Y + 40);
        }
    }

    /// <summary>
    /// Collision detection. Has flappy hit anything?
    /// </summary>
    /// <param name="flappy"></param>
    /// <returns>true - flappy collided with pipe | false - flappy has not hit anything.</returns>
    internal static bool FlappyCollidedWithPipe(Flappy flappy, out Rectangle rect)
    {
        rect = new Rectangle();
    
        int left = s_pos-30;
        int right = Flappy.s_idthOfAFlappyBirdPX + s_pos + 12;

        int score = 0;

        bool collided = false;

        // traverse each point, and count how many Flappy has gone past, and also whether it has collided
        foreach (Point p in s_pointsWhereRandomPipesAppearInTheScenery)
        {
            if (p.X < left) // we haven't got to flappy (offscreen, already gone past)
            {
                ++score;
                continue;
            }

            if (p.X > right) break; // we are looking too far right of Flappy

            ++score;

            // this is the rectangle between the top and bottom pipe
            Rectangle rectangleAreaBetweenVerticalPipes = new(p.X, p.Y - 40, 39, 80);

            foreach (Point hp in HitTestPoints)
            {
                int hitPointX = (int)(hp.X + flappy.HorizontalPositionOfFlappyPX - 1)+s_pos; // real world (scrolled coords)
                int hitPointY = (int)(hp.Y + flappy.VerticalPositionOfFlappyPX - 11); // center is 11.

                if (hitPointX >= rectangleAreaBetweenVerticalPipes.Right) continue;
                if (hitPointX <= rectangleAreaBetweenVerticalPipes.Left) continue;
             
                if (!rectangleAreaBetweenVerticalPipes.Contains(hitPointX,hitPointY))
                {
                    rect = new(p.X - s_pos, p.Y - 39, 39, 80);
                    collided = true;
                    break;
                }
            }
        }

        flappy.Score = score; // provide a score
        return collided;
    }

    /// <summary>
    /// Get a list of pipes within a certain distance of Flappy.
    /// </summary>
    /// <param name="flappy">Which Flappy bird we get the pipes for. Technically have the same X position.</param>
    /// <param name="distance">How far ahead to look.</param>
    /// <returns></returns>
    internal static List<Rectangle> GetClosestPipes(Flappy flappy, int distance)
    {
        int left = (int)(s_pos + flappy.HorizontalPositionOfFlappyPX) - 40;
        int right = (int)(Flappy.s_idthOfAFlappyBirdPX + left + flappy.HorizontalPositionOfFlappyPX) + distance;
        int bottom = s_heightOfGameArea - s_bitmapChevronFloor.Height;

        int found = 0;
        List<Rectangle> result = new();

        foreach (Point p in s_pointsWhereRandomPipesAppearInTheScenery)
        {
            if (p.X < left) continue;

            ++found;

            if (p.X > right || found > 3) break; // limit, seeing 50 ahead doesn't help so we cap it

            // add top pipe rectangle
            result.Add(new(p.X - s_pos, 0, 40, p.Y - 40));

            // add bottom pipe rectangle
            result.Add(new(p.X - s_pos, p.Y + 40, 40, bottom - p.Y - 40));
        }

        return result;
    }

    /// <summary>
    /// Reset the scenery to the start (we do it when all Flappy's have gone splat). And create new random pipes.
    /// </summary>
    internal static void Reset()
    {
        s_posBackground = 0;
        s_pos = 0;

        CreateRandomPipes();
    }
}