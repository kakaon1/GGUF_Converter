namespace gguf_Converter
{
    partial class Form1
    {
        /// <summary>필수 디자이너 변수입니다.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>사용 중인 모든 리소스를 정리합니다.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        private void InitializeComponent()
        {
            this.pnlTop = new System.Windows.Forms.Panel();
            this.lblLlama = new System.Windows.Forms.Label();
            this.txtLlamaPath = new System.Windows.Forms.TextBox();
            this.btnBrowseLlama = new System.Windows.Forms.Button();
            this.lblInput = new System.Windows.Forms.Label();
            this.txtInputPath = new System.Windows.Forms.TextBox();
            this.btnBrowseInput = new System.Windows.Forms.Button();
            this.btnBrowseInputFile = new System.Windows.Forms.Button();
            this.lblOutput = new System.Windows.Forms.Label();
            this.txtOutputPath = new System.Windows.Forms.TextBox();
            this.btnBrowseOutput = new System.Windows.Forms.Button();
            this.pnlOption = new System.Windows.Forms.Panel();
            this.lblQuant = new System.Windows.Forms.Label();
            this.cboQuantType = new System.Windows.Forms.ComboBox();
            this.chkKeepIntermediate = new System.Windows.Forms.CheckBox();
            this.chkVerifyLoad = new System.Windows.Forms.CheckBox();
            this.btnConvert = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.pnlTop.SuspendLayout();
            this.pnlOption.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlTop
            //
            this.pnlTop.Controls.Add(this.btnBrowseOutput);
            this.pnlTop.Controls.Add(this.txtOutputPath);
            this.pnlTop.Controls.Add(this.lblOutput);
            this.pnlTop.Controls.Add(this.btnBrowseInputFile);
            this.pnlTop.Controls.Add(this.btnBrowseInput);
            this.pnlTop.Controls.Add(this.txtInputPath);
            this.pnlTop.Controls.Add(this.lblInput);
            this.pnlTop.Controls.Add(this.btnBrowseLlama);
            this.pnlTop.Controls.Add(this.txtLlamaPath);
            this.pnlTop.Controls.Add(this.lblLlama);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(440, 86);
            this.pnlTop.TabIndex = 0;
            //
            // lblLlama
            //
            this.lblLlama.AutoSize = true;
            this.lblLlama.Location = new System.Drawing.Point(8, 9);
            this.lblLlama.Name = "lblLlama";
            this.lblLlama.Size = new System.Drawing.Size(58, 15);
            this.lblLlama.TabIndex = 0;
            this.lblLlama.Text = "llama.cpp";
            //
            // txtLlamaPath
            //
            this.txtLlamaPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLlamaPath.Location = new System.Drawing.Point(70, 6);
            this.txtLlamaPath.Name = "txtLlamaPath";
            this.txtLlamaPath.Size = new System.Drawing.Size(296, 23);
            this.txtLlamaPath.TabIndex = 1;
            //
            // btnBrowseLlama
            //
            this.btnBrowseLlama.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseLlama.Location = new System.Drawing.Point(372, 5);
            this.btnBrowseLlama.Name = "btnBrowseLlama";
            this.btnBrowseLlama.Size = new System.Drawing.Size(58, 25);
            this.btnBrowseLlama.TabIndex = 2;
            this.btnBrowseLlama.Text = "찾기";
            this.btnBrowseLlama.UseVisualStyleBackColor = true;
            this.btnBrowseLlama.Click += new System.EventHandler(this.btnBrowseLlama_Click);
            //
            // lblInput
            //
            this.lblInput.AutoSize = true;
            this.lblInput.Location = new System.Drawing.Point(8, 36);
            this.lblInput.Name = "lblInput";
            this.lblInput.Size = new System.Drawing.Size(58, 15);
            this.lblInput.TabIndex = 3;
            this.lblInput.Text = "입력 모델";
            //
            // txtInputPath
            //
            this.txtInputPath.AllowDrop = true;
            this.txtInputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInputPath.Location = new System.Drawing.Point(70, 33);
            this.txtInputPath.Name = "txtInputPath";
            this.txtInputPath.Size = new System.Drawing.Size(238, 23);
            this.txtInputPath.TabIndex = 4;
            this.txtInputPath.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtInputPath_DragDrop);
            this.txtInputPath.DragEnter += new System.Windows.Forms.DragEventHandler(this.txtInputPath_DragEnter);
            //
            // btnBrowseInput
            //
            this.btnBrowseInput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseInput.Location = new System.Drawing.Point(314, 32);
            this.btnBrowseInput.Name = "btnBrowseInput";
            this.btnBrowseInput.Size = new System.Drawing.Size(54, 25);
            this.btnBrowseInput.TabIndex = 5;
            this.btnBrowseInput.Text = "폴더";
            this.btnBrowseInput.UseVisualStyleBackColor = true;
            this.btnBrowseInput.Click += new System.EventHandler(this.btnBrowseInput_Click);
            //
            // btnBrowseInputFile
            //
            this.btnBrowseInputFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseInputFile.Location = new System.Drawing.Point(372, 32);
            this.btnBrowseInputFile.Name = "btnBrowseInputFile";
            this.btnBrowseInputFile.Size = new System.Drawing.Size(58, 25);
            this.btnBrowseInputFile.TabIndex = 6;
            this.btnBrowseInputFile.Text = "파일";
            this.btnBrowseInputFile.UseVisualStyleBackColor = true;
            this.btnBrowseInputFile.Click += new System.EventHandler(this.btnBrowseInputFile_Click);
            //
            // lblOutput
            //
            this.lblOutput.AutoSize = true;
            this.lblOutput.Location = new System.Drawing.Point(8, 63);
            this.lblOutput.Name = "lblOutput";
            this.lblOutput.Size = new System.Drawing.Size(58, 15);
            this.lblOutput.TabIndex = 6;
            this.lblOutput.Text = "출력 파일";
            //
            // txtOutputPath
            //
            this.txtOutputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutputPath.Location = new System.Drawing.Point(70, 60);
            this.txtOutputPath.Name = "txtOutputPath";
            this.txtOutputPath.Size = new System.Drawing.Size(296, 23);
            this.txtOutputPath.TabIndex = 7;
            //
            // btnBrowseOutput
            //
            this.btnBrowseOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseOutput.Location = new System.Drawing.Point(372, 59);
            this.btnBrowseOutput.Name = "btnBrowseOutput";
            this.btnBrowseOutput.Size = new System.Drawing.Size(58, 25);
            this.btnBrowseOutput.TabIndex = 8;
            this.btnBrowseOutput.Text = "찾기";
            this.btnBrowseOutput.UseVisualStyleBackColor = true;
            this.btnBrowseOutput.Click += new System.EventHandler(this.btnBrowseOutput_Click);
            //
            // pnlOption
            //
            this.pnlOption.Controls.Add(this.btnCancel);
            this.pnlOption.Controls.Add(this.btnConvert);
            this.pnlOption.Controls.Add(this.chkVerifyLoad);
            this.pnlOption.Controls.Add(this.chkKeepIntermediate);
            this.pnlOption.Controls.Add(this.cboQuantType);
            this.pnlOption.Controls.Add(this.lblQuant);
            this.pnlOption.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlOption.Location = new System.Drawing.Point(0, 86);
            this.pnlOption.Name = "pnlOption";
            this.pnlOption.Size = new System.Drawing.Size(440, 60);
            this.pnlOption.TabIndex = 1;
            //
            // lblQuant
            //
            this.lblQuant.AutoSize = true;
            this.lblQuant.Location = new System.Drawing.Point(8, 9);
            this.lblQuant.Name = "lblQuant";
            this.lblQuant.Size = new System.Drawing.Size(44, 15);
            this.lblQuant.TabIndex = 0;
            this.lblQuant.Text = "양자화";
            //
            // cboQuantType
            //
            this.cboQuantType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboQuantType.FormattingEnabled = true;
            this.cboQuantType.Items.AddRange(new object[] {
            "F16",
            "Q8_0",
            "Q6_K",
            "Q5_K_M",
            "Q5_K_S",
            "Q4_K_M",
            "Q4_K_S",
            "Q4_0",
            "Q3_K_M",
            "Q2_K"});
            this.cboQuantType.Location = new System.Drawing.Point(58, 5);
            this.cboQuantType.Name = "cboQuantType";
            this.cboQuantType.Size = new System.Drawing.Size(90, 23);
            this.cboQuantType.TabIndex = 1;
            //
            // chkKeepIntermediate
            //
            this.chkKeepIntermediate.AutoSize = true;
            this.chkKeepIntermediate.Location = new System.Drawing.Point(10, 36);
            this.chkKeepIntermediate.Name = "chkKeepIntermediate";
            this.chkKeepIntermediate.Size = new System.Drawing.Size(80, 19);
            this.chkKeepIntermediate.TabIndex = 2;
            this.chkKeepIntermediate.Text = "F16 유지";
            this.chkKeepIntermediate.UseVisualStyleBackColor = true;
            //
            // chkVerifyLoad
            //
            this.chkVerifyLoad.AutoSize = true;
            this.chkVerifyLoad.Checked = true;
            this.chkVerifyLoad.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkVerifyLoad.Location = new System.Drawing.Point(110, 36);
            this.chkVerifyLoad.Name = "chkVerifyLoad";
            this.chkVerifyLoad.Size = new System.Drawing.Size(148, 19);
            this.chkVerifyLoad.TabIndex = 3;
            this.chkVerifyLoad.Text = "llama.cpp 로드 검증";
            this.chkVerifyLoad.UseVisualStyleBackColor = true;
            //
            // btnConvert
            //
            this.btnConvert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConvert.Location = new System.Drawing.Point(272, 4);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(74, 26);
            this.btnConvert.TabIndex = 4;
            this.btnConvert.Text = "변환";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Enabled = false;
            this.btnCancel.Location = new System.Drawing.Point(352, 4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(78, 26);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "중지";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // txtLog
            //
            this.txtLog.BackColor = System.Drawing.Color.White;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.txtLog.Location = new System.Drawing.Point(0, 146);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.Size = new System.Drawing.Size(440, 154);
            this.txtLog.TabIndex = 2;
            this.txtLog.WordWrap = false;
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(440, 300);
            // 지침 4번 규칙 6 : Dock=Fill 을 먼저 추가하고, Dock=Top 은 아래쪽 -> 위쪽 역순으로 추가한다.
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.pnlOption);
            this.Controls.Add(this.pnlTop);
            // 폼 아이콘은 Form1.cs 의 LoadFormIcon() 에서 런타임에 설정한다.
            // (resx 바이너리 임베드는 타입 문자열 해석에 의존하므로 사용하지 않는다)
            this.MinimumSize = new System.Drawing.Size(456, 260);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "GGUF Converter";
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.pnlOption.ResumeLayout(false);
            this.pnlOption.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblLlama;
        private System.Windows.Forms.TextBox txtLlamaPath;
        private System.Windows.Forms.Button btnBrowseLlama;
        private System.Windows.Forms.Label lblInput;
        private System.Windows.Forms.TextBox txtInputPath;
        private System.Windows.Forms.Button btnBrowseInput;
        private System.Windows.Forms.Button btnBrowseInputFile;
        private System.Windows.Forms.Label lblOutput;
        private System.Windows.Forms.TextBox txtOutputPath;
        private System.Windows.Forms.Button btnBrowseOutput;
        private System.Windows.Forms.Panel pnlOption;
        private System.Windows.Forms.Label lblQuant;
        private System.Windows.Forms.ComboBox cboQuantType;
        private System.Windows.Forms.CheckBox chkKeepIntermediate;
        private System.Windows.Forms.CheckBox chkVerifyLoad;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtLog;
    }
}
