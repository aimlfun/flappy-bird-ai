namespace FlappyBirdAI
{
    partial class FormFlappyBrain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBoxBrain = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxBrain)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxBrain
            // 
            this.pictureBoxBrain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxBrain.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxBrain.Name = "pictureBoxBrain";
            this.pictureBoxBrain.Size = new System.Drawing.Size(400, 266);
            this.pictureBoxBrain.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxBrain.TabIndex = 0;
            this.pictureBoxBrain.TabStop = false;
            // 
            // FormFlappyBrain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(400, 266);
            this.Controls.Add(this.pictureBoxBrain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormFlappyBrain";
            this.Text = "Inside the Flappy brain";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxBrain)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private PictureBox pictureBoxBrain;
    }
}