using FlappyBird;
using FlappyBirdAI.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlappyBirdAI
{
    public partial class FormFlappyBrain : Form
    {
        public FormFlappyBrain()
        {
            InitializeComponent();
        }

        internal void Visualise(Flappy flappy)
        {
            Bitmap b = FlappyBirdBrainNeuralNetworkVisualiser.Draw(flappy);
            pictureBoxBrain.Image?.Dispose();
            pictureBoxBrain.Image = b;
        }
    }
}
