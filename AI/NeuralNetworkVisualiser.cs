using FlappyBird;
using FlappyBirdAI.Sensor;
using FlappyBirdAI.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace FlappyBirdAI.AI
{
    internal static class FlappyBirdBrainNeuralNetworkVisualiser
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="flappy"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static Bitmap Draw(Flappy flappy)
        {
            NeuralNetwork n = NeuralNetwork.s_networks[flappy.Id];

            if (n.Layers.Length != 3) throw new Exception("Visualisation is designed for a single hidden layer.");

            Bitmap b = new(1200, (PipeSensor.s_samplePoints+2)*75+40);

            using Graphics graphics = Graphics.FromImage(b);
            graphics.Clear(Color.White);

            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            
            using Font font = new("Arial", 7);

            graphics.DrawString(flappy.GetRawData(), font, Brushes.Black, 10, 250);

            PointF brainCenter = new(600, b.Height/2);
            
            // compute the proximity sensor positions
            double sensorAngleToCheckInDegrees = PipeSensor.s_fieldOfVisionStartInDegrees;

            // this is (in PipeSensor) setup during first read, for testing outside, we need this.
            float s_sensorVisionAngleInDegrees = (float)Math.Abs(PipeSensor.s_fieldOfVisionStartInDegrees * 2f - 1) / PipeSensor.s_samplePoints;

            List<PointF> layer1 = new();
            List<PointF> layer2 = new();

            using Pen penDottedLine = new(Color.Black);
            penDottedLine.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

            int z = 0;
            for (int LIDARangleIndex = 0; LIDARangleIndex < PipeSensor.s_samplePoints; LIDARangleIndex++)
            {
                double LIDARangleToCheckInRadiansMin = MathUtils.DegreesInRadians(sensorAngleToCheckInDegrees);

                // make the points proportional to the distance proximity the INPUT neurons are sending
                PointF pointSensor = new((float)(Math.Cos(LIDARangleToCheckInRadiansMin) * ((1 - n.Neurons[0][z]) * 2 * 350).Clamp(-390, 390) + brainCenter.X),
                                         (float)(Math.Sin(LIDARangleToCheckInRadiansMin) * ((1 - n.Neurons[0][z]) * 2 * 350).Clamp(-390, 390) + brainCenter.Y));

                layer1.Add(pointSensor);

                graphics.DrawLine(penDottedLine, brainCenter, pointSensor);

                sensorAngleToCheckInDegrees += s_sensorVisionAngleInDegrees;

                // store the points for drawing the neuron later
                PointF pointLayer2 = new(250, (LIDARangleIndex - PipeSensor.s_samplePoints / 2) * 75 + brainCenter.Y);
                layer2.Add(pointLayer2);
                ++z;
            }


            layer1.Add(new PointF(700, b.Height - 150));
            layer1.Add(new PointF(700, b.Height - 75));

            layer2.Add(new PointF(250, b.Height - 150));
            layer2.Add(new PointF(250, b.Height - 75));

            graphics.DrawImageUnscaled(Flappy.s_FlappyImageFrames[1], (int)brainCenter.X - 14, (int)brainCenter.Y - 9);

            PointF layer3Neuron = new(30, b.Height/2);

            // draw lines from INPUT > HIDDEN layer and INPUT > OUTPUT
            for (int j = 0; j < layer2.Count; j++)
            {
                for (int i = 0; i < layer1.Count; i++)
                {
                    DrawLineBetweenNeurons(graphics: graphics, locationOfNeuron1: layer2[j], locationOfNeuron2: layer1[i], weight: n.Weights[0][j][i], amount: n.Weights[0][j][i] * n.Neurons[0][i]);
                }

                DrawLineBetweenNeurons(graphics: graphics, locationOfNeuron1: layer3Neuron, locationOfNeuron2: layer2[j], weight: n.Weights[1][0][j], amount: n.Weights[1][0][j] * n.Neurons[1][j]);
            }

            // draw INPUT neurons
            for (int i = 0; i < layer1.Count; i++)
            {
                DrawNeuron(graphics: graphics, locationOfNeuron: layer1[i], neuronValue: n.Neurons[0][i], neuronBias: 0);
            }

            // draw HIDDEN neurons
            for (int i = 0; i < layer2.Count; i++)
            {
                DrawNeuron(graphics: graphics, locationOfNeuron: layer2[i], neuronValue: n.Neurons[1][i], neuronBias: n.Biases[1][i]);
            }

            // draw output neuron
            DrawNeuron(graphics: graphics, locationOfNeuron: layer3Neuron, neuronValue: n.Neurons[2][0], neuronBias: n.Biases[2][0]);

            // draw arrows indicating acceleration applied to bird
            DrawAccelerationArrow(graphics: graphics, locationOfNeuron: layer3Neuron, neuronValue: n.Neurons[2][0]);

            //b.Save($@"c:\temp\visualisation-{flappy.Id}.png", ImageFormat.Png); uncomment to snapshot this image
            return b;
        }

        /// <summary>
        /// Draw an arrow indicating the neural network output.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="locationOfNeuron"></param>
        /// <param name="neuronValue"></param>
        private static void DrawAccelerationArrow(Graphics graphics, PointF locationOfNeuron, double neuronValue)
        {
            if (neuronValue == 0) return;

            // invert, because positive should be up, negative down but Bitmap cartesian is (0,0) top left not bottom right.
            neuronValue = -neuronValue;

            float y = locationOfNeuron.Y + Math.Sign(neuronValue) * 40;
            float extendsTo = locationOfNeuron.Y + Math.Sign(neuronValue) * (float)(40 + Math.Abs(neuronValue) * 20);

            // these define the arrow
            List<PointF> points = new()
            {
                new PointF(locationOfNeuron.X - 5, y), // top left
                new PointF(locationOfNeuron.X + 5, y), // top right
                new PointF(locationOfNeuron.X + 5, extendsTo), // bottom right
                new PointF(locationOfNeuron.X + 10, extendsTo), // bottom right triangle                
                new PointF(locationOfNeuron.X, extendsTo + Math.Sign(neuronValue) * 20), // bottom right triangle
                new PointF(locationOfNeuron.X - 10, extendsTo), // bottom left triangle
                new PointF(locationOfNeuron.X - 5, extendsTo), // bottom left
                new PointF(locationOfNeuron.X - 5, y) // top left
            };

            using SolidBrush p = new(Color.FromArgb(50, 0, 0, 0));
            graphics.FillPolygon(p, points.ToArray());
        }

        /// <summary>
        /// Draws a line between the 2 neurons, with a label on the slope indicating weight.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="locationOfNeuron1"></param>
        /// <param name="locationOfNeuron2"></param>
        /// <param name="weight"></param>
        /// <param name="amount"></param>
        private static void DrawLineBetweenNeurons(Graphics graphics, PointF locationOfNeuron1, PointF locationOfNeuron2, double weight, double amount)
        {
            if (weight == 0) return; // no line as weight does not use the source neuron's output

            // color red or green depending on sign of weighting
            using Pen pen = new(amount < 0 ? Color.FromArgb(100, 255, 0, 0) : Color.FromArgb(100, 0, 255, 0), Math.Abs((float)amount * 2.5f));

            graphics.DrawLine(pen, locationOfNeuron1, locationOfNeuron2);

            // write a label at the center of the line with the slope of the line
            WriteLabelAlongTheSlopeOfLine(graphics, locationOfNeuron1, locationOfNeuron2, weight);
        }

        /// <summary>
        /// It looks tidier with label aligning to slop of line.
        /// We thus compute the angle of the line, and rotate whilst drawing the text.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="locationOfNeuron1"></param>
        /// <param name="locationOfNeuron2"></param>
        /// <param name="weight"></param>
        private static void WriteLabelAlongTheSlopeOfLine(Graphics graphics, PointF locationOfNeuron1, PointF locationOfNeuron2, double weight)
        {
            int avgX = (int)(locationOfNeuron1.X + locationOfNeuron2.X) / 2;
            int avgY = (int)(locationOfNeuron1.Y + locationOfNeuron2.Y) / 2;

            using Font font = new("Arial", 7);

            string weightingsLabel = "x" + Math.Round(weight, 3).ToString();
            SizeF size = graphics.MeasureString(weightingsLabel, font);

            // compute the angle of the line. ATAN2 does it properly.
            float angleOfLineInRADIANS = (float)Math.Atan2(locationOfNeuron1.Y - locationOfNeuron2.Y,
                                                           locationOfNeuron1.X - locationOfNeuron2.X);

            float angleOfLineInDEGREES = (float)MathUtils.RadiansInDegrees(angleOfLineInRADIANS) + 180; // 180 -> we want text reading left-to-right.

            var savedGraphicsState = graphics.Save(); // save so we can revert the transform (otherwise everything that is drawn subsequently rotates)

            graphics.TranslateTransform(avgX, avgY); // we are rotating about the origin (center of the text on the line)
            graphics.RotateTransform(angleOfLineInDEGREES);
            graphics.DrawString(weightingsLabel, font, Brushes.Black, new PointF(-size.Width / 2, -size.Height / 2)); // drawn at an angle

            graphics.Restore(savedGraphicsState); // undo the "rotate"
        }

        /// <summary>
        /// Draws a circular neuron, coloured based on the neuron's value.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="locationOfNeuron"></param>
        /// <param name="neuronValue"></param>
        /// <param name="neuronBias"></param>
        private static void DrawNeuron(Graphics graphics, PointF locationOfNeuron, double neuronValue, double neuronBias)
        {
            const int radius = 40;

            using Font font = new("Arial", 7);

            Color colour = neuronValue == 0 ? Color.White : (neuronValue < 0 ? Color.FromArgb((int)Math.Abs(neuronValue * 255), 255, 0, 0) : Color.FromArgb((int)Math.Abs(neuronValue * 255).Clamp(0,255), 0, 255, 0));

            using SolidBrush brushNeuron = new(colour);

            graphics.FillEllipse(brushNeuron, locationOfNeuron.X - radius / 2, locationOfNeuron.Y - radius / 2, radius, radius);
            graphics.DrawEllipse(Pens.Black, locationOfNeuron.X - radius / 2, locationOfNeuron.Y - radius / 2, radius, radius);

            string label = Math.Round(neuronValue, 3).ToString();
            SizeF size = graphics.MeasureString(label, font);
            graphics.DrawString(label, font, Brushes.Black, new PointF(locationOfNeuron.X - size.Width / 2, locationOfNeuron.Y - size.Height / 2));

            // don't show 0 bias (as they contribute nothing)
            if (neuronBias != 0)
            {
                label = Math.Round(neuronBias, 3).ToString();
                size = graphics.MeasureString(label, font);
                graphics.DrawString(label, font, neuronBias < 0 ? Brushes.Red : Brushes.Green, new PointF(locationOfNeuron.X - size.Width / 2, locationOfNeuron.Y - radius / 2 - 5 - size.Height));
            }
        }
    }
}