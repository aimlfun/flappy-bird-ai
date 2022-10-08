using System.Drawing.Drawing2D;
using FlappyBirdAI.Utils;

namespace FlappyBirdAI.Sensor;

/// <summary>
///     ___  _          ____                     
///    / _ \(_)__  ___ / __/__ ___  ___ ___  ____
///   / ___/ / _ \/ -_)\ \/ -_) _ \(_-</ _ \/ __/
///  /_/  /_/ .__/\__/___/\__/_//_/___/\___/_/   
///        /_/                                   
/// </summary>
internal class PipeSensor
{
    /// <summary>
    /// Stores the "lines" that contain pipe.
    /// </summary>
    private readonly List<PointF[]> wallSensorTriangleTargetIsInPolygonsInDeviceCoordinates = new();

    /// <summary>
    /// How many sample points it radiates to detect a pipe.
    /// </summary>
    internal static int s_samplePoints = 7;

    // e.g 
    // input to the neural network
    //   _ \ | / _   
    //   0 1 2 3 4 
    //        
    internal static double s_fieldOfVisionStartInDegrees = -125;

    //   _ \ | / _   
    //   0 1 2 3 4
    //   [-] this
    internal static double s_sensorVisionAngleInDegrees = 0;

    /// <summary>
    /// Detects pipes using a sensor.
    /// </summary>
    /// <param name="flappyLocation"></param>
    /// <param name="heatSensorRegionsOutput"></param>
    /// <returns></returns>
    internal double[] Read(List<Rectangle> pipesOnScreen, PointF flappyLocation, out double[] heatSensorRegionsOutput)
    {
        wallSensorTriangleTargetIsInPolygonsInDeviceCoordinates.Clear();

        heatSensorRegionsOutput = new double[s_samplePoints];

        //   _ \ | / _   
        //   0 1 2 3 4
        //   [-] this
        s_sensorVisionAngleInDegrees = (float)Math.Abs(s_fieldOfVisionStartInDegrees * 2f - 1) / s_samplePoints;

        //   _ \ | / _   
        //   0 1 2 3 4
        //   ^ this
        double sensorAngleToCheckInDegrees = s_fieldOfVisionStartInDegrees;

        // how far it looks for a pipe
        double DepthOfVisionInPixels = 300;

        for (int LIDARangleIndex = 0; LIDARangleIndex < s_samplePoints; LIDARangleIndex++)
        {
            //     -45  0  45
            //  -90 _ \ | / _ 90   <-- relative to flappy
            double LIDARangleToCheckInRadiansMin = MathUtils.DegreesInRadians(sensorAngleToCheckInDegrees);

            PointF pointSensor = new((float)(Math.Cos(LIDARangleToCheckInRadiansMin) * DepthOfVisionInPixels + flappyLocation.X),
                                     (float)(Math.Sin(LIDARangleToCheckInRadiansMin) * DepthOfVisionInPixels + flappyLocation.Y));

            heatSensorRegionsOutput[LIDARangleIndex] = 1; // no target in this direction

            PointF intersection = new();

            // check each "target" rectangle (marking the border of a pipe) and see if it intersects with the sensor.            
            foreach (Rectangle boundingBoxAroundPipe in pipesOnScreen)
            {
                // rectangle is bounding box for the pipe
                Point topRight = new(boundingBoxAroundPipe.Right, boundingBoxAroundPipe.Top);
                Point topLeft = new(boundingBoxAroundPipe.Left, boundingBoxAroundPipe.Top);
                Point bottomLeft = new(boundingBoxAroundPipe.Left, boundingBoxAroundPipe.Bottom);
                Point bottomRight = new(boundingBoxAroundPipe.Right, boundingBoxAroundPipe.Bottom);

                // check left side of pipe
                FindIntersectionWithPipe(flappyLocation, heatSensorRegionsOutput, DepthOfVisionInPixels, LIDARangleIndex, pointSensor, ref intersection, topLeft, bottomLeft);

                // check the bottom side of pipe (pipes coming out of ceiling)
                FindIntersectionWithPipe(flappyLocation, heatSensorRegionsOutput, DepthOfVisionInPixels, LIDARangleIndex, pointSensor, ref intersection, bottomLeft, bottomRight);

                // check the top of pipe (pipes coming out floor)
                FindIntersectionWithPipe(flappyLocation, heatSensorRegionsOutput, DepthOfVisionInPixels, LIDARangleIndex, pointSensor, ref intersection, topLeft, topRight);

                // we check right side, even though we're looking forwards because some proximity sensors point backwards
                FindIntersectionWithPipe(flappyLocation, heatSensorRegionsOutput, DepthOfVisionInPixels, LIDARangleIndex, pointSensor, ref intersection, topRight, bottomRight);
            }

            // detect where the floor is
            FindIntersectionWithPipe(flappyLocation, heatSensorRegionsOutput, DepthOfVisionInPixels, LIDARangleIndex, pointSensor, ref intersection, new(0, 294), new(800, 294));

            // check where the ceiling is
            FindIntersectionWithPipe(flappyLocation, heatSensorRegionsOutput, DepthOfVisionInPixels, LIDARangleIndex, pointSensor, ref intersection, new(0, 0), new(800, 0));

            heatSensorRegionsOutput[LIDARangleIndex] = 1 - heatSensorRegionsOutput[LIDARangleIndex];

            // only add to the sensor visual if we have detected something
            if (heatSensorRegionsOutput[LIDARangleIndex] > 0 && (intersection.X != 0 || intersection.Y != 0)) wallSensorTriangleTargetIsInPolygonsInDeviceCoordinates.Add(new PointF[] { flappyLocation, intersection });

            //   _ \ | / _         _ \ | / _   
            //   0 1 2 3 4         0 1 2 3 4
            //  [-] from this       [-] to this
            sensorAngleToCheckInDegrees += s_sensorVisionAngleInDegrees;
        }

        return heatSensorRegionsOutput;
    }

    /// <summary>
    /// Detect intersection between line to sensor limit and the pipe returning "intersection" where they meet.
    /// </summary>
    /// <param name="flappyLocation"></param>
    /// <param name="heatSensorRegionsOutput"></param>
    /// <param name="DepthOfVisionInPixels"></param>
    /// <param name="LIDARangleIndex"></param>
    /// <param name="pointSensorLimit"></param>
    /// <param name="intersection"></param>
    /// <param name="pointPipeLine1"></param>
    /// <param name="pointPipeLine2"></param>
    private static void FindIntersectionWithPipe(
        PointF flappyLocation,
        double[] heatSensorRegionsOutput,
        double DepthOfVisionInPixels,
        int LIDARangleIndex,
        PointF pointSensorLimit,
        ref PointF intersection,
        Point pointPipeLine1,
        Point pointPipeLine2)
    {
        /* _______________________
         *       .   |  |   |  |           |  |
         *      .   .====   |  |     left> |  |
         *     .  .         |  |           |  |   
         *  (<)O.....====   |  |           ====
         *    . .  . |  |   |  |             ^ bottom 
         *    .  .   |  |   ====   
         *    .   .  |  |          
         *    .    . |  |   ====
         *    .      |  |   |  |   
         *  __.______|__|___|__|__
         */
        if (MathUtils.GetLineIntersection(flappyLocation, pointSensorLimit, pointPipeLine1, pointPipeLine2, out PointF intersectionLeftSide))
        {
            double mult = MathUtils.DistanceBetweenTwoPoints(flappyLocation, intersectionLeftSide).Clamp(0F, (float)DepthOfVisionInPixels) / DepthOfVisionInPixels;

            // this "surface" intersects closer than prior ones at the same angle?
            if (mult < heatSensorRegionsOutput[LIDARangleIndex])
            {
                heatSensorRegionsOutput[LIDARangleIndex] = mult;  // closest
                intersection = intersectionLeftSide;
            }
        }
    }

    /// <summary>
    /// Draws lines radiating, show where pipe was detected.
    /// </summary>
    /// <param name="graphics"></param>
    internal void DrawWhereTargetIsInRespectToSweepOfHeatSensor(Graphics graphics)
    {
        using Pen pen = new(Color.FromArgb(60, 40, 0, 0));
        pen.DashStyle = DashStyle.Dot;

        // draw the heat sensor
        foreach (PointF[] point in wallSensorTriangleTargetIsInPolygonsInDeviceCoordinates)
        {
            graphics.DrawLines(pen, point);
        }
    }
}