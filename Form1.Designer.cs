namespace LeagueIconsReplacer {
    partial class Form1 {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            buttonSetDirectory = new Button();
            buttonStart = new Button();
            checkBoxUseWadMaker = new CheckBox();
            SuspendLayout();
            // 
            // buttonSetDirectory
            // 
            buttonSetDirectory.BackColor = Color.FromArgb(42, 163, 204);
            buttonSetDirectory.FlatAppearance.BorderSize = 0;
            buttonSetDirectory.FlatStyle = FlatStyle.Flat;
            buttonSetDirectory.ForeColor = Color.FromArgb(42, 42, 42);
            buttonSetDirectory.Location = new Point(42, 29);
            buttonSetDirectory.Name = "buttonSetDirectory";
            buttonSetDirectory.Size = new Size(123, 23);
            buttonSetDirectory.TabIndex = 0;
            buttonSetDirectory.Text = "Choose Directory";
            buttonSetDirectory.UseVisualStyleBackColor = false;
            buttonSetDirectory.Click += buttonSetDirectory_Click;
            // 
            // buttonStart
            // 
            buttonStart.BackColor = Color.FromArgb(42, 163, 204);
            buttonStart.FlatAppearance.BorderSize = 0;
            buttonStart.FlatStyle = FlatStyle.Flat;
            buttonStart.ForeColor = Color.FromArgb(42, 42, 42);
            buttonStart.Location = new Point(42, 77);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(123, 23);
            buttonStart.TabIndex = 1;
            buttonStart.Text = "Start Replacing";
            buttonStart.UseVisualStyleBackColor = false;
            buttonStart.Click += buttonStart_Click;
            // 
            // checkBoxUseWadMaker
            // 
            checkBoxUseWadMaker.AutoSize = true;
            checkBoxUseWadMaker.ForeColor = Color.White;
            checkBoxUseWadMaker.Location = new Point(11, 115);
            checkBoxUseWadMaker.Name = "checkBoxUseWadMaker";
            checkBoxUseWadMaker.Size = new Size(149, 19);
            checkBoxUseWadMaker.TabIndex = 2;
            checkBoxUseWadMaker.Text = "Use Wad Maker Instead";
            checkBoxUseWadMaker.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(58, 69, 86);
            ClientSize = new Size(207, 146);
            Controls.Add(checkBoxUseWadMaker);
            Controls.Add(buttonStart);
            Controls.Add(buttonSetDirectory);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Name = "Form1";
            Text = "Lol Icon Replacer";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonSetDirectory;
        private Button buttonStart;
        private CheckBox checkBoxUseWadMaker;
    }
}