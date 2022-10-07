using FlappyBird;
using FlappyBirdAI.Sensor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlappyBirdAI.AI
{
    internal class Telemetry
    {
        /// <summary>
        /// true - writes telemetry of Flappy to a .csv if they complete the entire course.
        /// false - any calls to record are ignored (as is the write operation).
        /// </summary>
        private readonly bool c_recordTelemetry = false; // not made readonly. If I do that, it moans at all the conditional code.

        /// <summary>
        /// Tracks telemetry as strings to write to a file. We cache to avoid writing ones we later discard.
        /// </summary>
        private readonly StringBuilder telemetry = new(1000); // will grow to 20k if it completes the course.

        /// <summary>
        /// The Flappy that the telemetry was for.
        /// </summary>
        private readonly Flappy flappyWeAreRecording;

        /// <summary>
        /// To enable us to write the header once only without getting size of StringBuilder.
        /// </summary>
        private bool haveWrittenHeader = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="flappy"></param>
        internal Telemetry(Flappy flappy)
        {
            flappyWeAreRecording = flappy;
        }

        /// <summary>
        /// Records a Flappy birds Telemetry in memory.
        /// We don't know if this flappy bird will reach the end, so we don't write the data instead record it.
        /// </summary>
        /// <param name="flappy"></param>
        /// <param name="sensorOutputs"></param>
        /// <param name="neuralNetworkOutput"></param>
        /// <param name="rectanglesIndicatingWherePipesWerePresent"></param>
        internal void Record(List<double> sensorOutputs, double neuralNetworkOutput, List<Rectangle> rectanglesIndicatingWherePipesWerePresent)
        {
            if (!c_recordTelemetry) return; // DISABLED telemetry

            // write telemetry
            if (!haveWrittenHeader) WriteTelemetryHeader(sensorOutputs.Count - 2);

            telemetry.AppendLine($"{ProximitySensorsSerialisedToString(sensorOutputs)},{neuralNetworkOutput},{SerialiseRectanglesRelativeToFlappy(rectanglesIndicatingWherePipesWerePresent)}");
        }

        /// <summary>
        /// Writes telemetry to csv file (if enabled).
        /// </summary>
        internal void WriteTelemetryIfEnabled()
        {
            if (!c_recordTelemetry) return;

            File.WriteAllText($@"c:\temp\flappy-telemetry-{flappyWeAreRecording.Id}.csv", telemetry.ToString()); // if you open in Excel whilst running, you'll get an i/o exception the next time it tries to write

            // we no longer need it, we're going to mutate and create next generation.
            telemetry.Clear();
        }

        /// <summary>
        /// Splits "d" into sensors and vert vel/accel components, expanding the latter two.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private static string ProximitySensorsSerialisedToString(List<double> d)
        {
            string serialisedData = "";

            // add the proximity sensors 
            for (int i = 0; i < d.Count - 2; i++)
            {
                serialisedData += $"{d[i]},";
            }

            // these are divided by 3 to keep -1..1 range for neural network, so we reverse that here
            serialisedData += $"{d[^2] * 3},{d[^1] * 3}";

            return serialisedData;
        }

        /// <summary>
        /// The output has less meaning unless relative. So we adjust the rectangle of the pipe based on where flappy is.
        /// </summary>
        /// <param name="rectanglesIndicatingWherePipesWerePresent"></param>
        /// <returns></returns>
        private string SerialiseRectanglesRelativeToFlappy(List<Rectangle> rectanglesIndicatingWherePipesWerePresent)
        {
            List<string> serialisedData = new();

            foreach (Rectangle rectangle in rectanglesIndicatingWherePipesWerePresent)
            {
                // we want to write the positions of the pipes relative to Flappy
                int relativeRectangleXtop = (int)Math.Round(rectangle.Left + flappyWeAreRecording.HorizontalPositionOfFlappyPX);
                int relativeRectangleYtop = (int)Math.Round(rectangle.Top - flappyWeAreRecording.VerticalPositionOfFlappyPX);
                int relativeRectangleXbottom = (int)Math.Round(rectangle.Right + flappyWeAreRecording.HorizontalPositionOfFlappyPX);
                int relativeRectangleYbottom = (int)Math.Round(Math.Min(305, rectangle.Bottom) - flappyWeAreRecording.VerticalPositionOfFlappyPX);

                serialisedData.Add($"{relativeRectangleXtop},{relativeRectangleYtop},{relativeRectangleXbottom},{relativeRectangleYbottom}");
            }

            return string.Join(",", serialisedData);
        }

        /// <summary>
        /// Telemetry requires a header, and that depends on sensor count.
        /// </summary>
        /// <param name="numberOfSensors"></param>
        /// <returns></returns>
        private void WriteTelemetryHeader(int numberOfSensors)
        {
            string header = "";

            for (int i = 1; i <= numberOfSensors; i++)
            {
                // put the "angle".
                header += $"sensor {Math.Round(PipeSensor.s_fieldOfVisionStartInDegrees + PipeSensor.s_sensorVisionAngleInDegrees * (i - 1)),2} degrees,";
            }

            header += "vertical speed,vertical acceleration,output from neural Network,pipe-top-x1,pipe-top-y1,pipe-bottom-x1,pipe-bottom-y1,pipe-top-x2,pipe-top-y2,pipe-bottom-x2,pipe-bottom-y2,pipe-top-x3,pipe-top-y3,pipe-bottom-x3,pipe-bottom-y3,pipe-top-x4,pipe-top-y4,pipe-bottom-x4,pipe-bottom-y4";

            telemetry.AppendLine(header);

            haveWrittenHeader = true;
        }
    }
}