namespace FlappyBird
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.timerScroll = new System.Windows.Forms.Timer(this.components);
            this.pictureBoxStats = new System.Windows.Forms.PictureBox();
            this.pictureBoxFlappyGameScreen = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStats)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxFlappyGameScreen)).BeginInit();
            this.SuspendLayout();
            // 
            // timerScroll
            // 
            this.timerScroll.Interval = 10;
            this.timerScroll.Tick += new System.EventHandler(this.TimerScroll_Tick);
            // 
            // pictureBoxStats
            // 
            this.pictureBoxStats.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pictureBoxStats.Location = new System.Drawing.Point(0, 305);
            this.pictureBoxStats.Name = "pictureBoxStats";
            this.pictureBoxStats.Size = new System.Drawing.Size(559, 76);
            this.pictureBoxStats.TabIndex = 1;
            this.pictureBoxStats.TabStop = false;
            // 
            // pictureBoxFlappyGameScreen
            // 
            this.pictureBoxFlappyGameScreen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxFlappyGameScreen.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxFlappyGameScreen.Name = "pictureBoxFlappyGameScreen";
            this.pictureBoxFlappyGameScreen.Size = new System.Drawing.Size(559, 305);
            this.pictureBoxFlappyGameScreen.TabIndex = 2;
            this.pictureBoxFlappyGameScreen.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CausesValidation = false;
            this.ClientSize = new System.Drawing.Size(559, 381);
            this.Controls.Add(this.pictureBoxFlappyGameScreen);
            this.Controls.Add(this.pictureBoxStats);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Flappy";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStats)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxFlappyGameScreen)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer timerScroll;
        private PictureBox pictureBoxStats;
        private PictureBox pictureBoxFlappyGameScreen;
    }
}