
using System;

namespace Blockchain
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxNodeName = new System.Windows.Forms.TextBox();
            this.buttonConnect = new System.Windows.Forms.Button();
            this.buttonMine = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.labelStatus = new System.Windows.Forms.Label();
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.buttonConnectPort = new System.Windows.Forms.Button();
            this.infoBlockchain = new System.Windows.Forms.RichTextBox();
            this.infoMining = new System.Windows.Forms.RichTextBox();
            this.buttonStop = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Node name";
            // 
            // textBoxNodeName
            // 
            this.textBoxNodeName.Location = new System.Drawing.Point(117, 9);
            this.textBoxNodeName.Name = "textBoxNodeName";
            this.textBoxNodeName.Size = new System.Drawing.Size(125, 27);
            this.textBoxNodeName.TabIndex = 1;
            // 
            // buttonConnect
            // 
            this.buttonConnect.Location = new System.Drawing.Point(248, 9);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(94, 29);
            this.buttonConnect.TabIndex = 2;
            this.buttonConnect.Text = "Connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            // 
            // buttonMine
            // 
            this.buttonMine.Location = new System.Drawing.Point(348, 9);
            this.buttonMine.Name = "buttonMine";
            this.buttonMine.Size = new System.Drawing.Size(94, 29);
            this.buttonMine.TabIndex = 3;
            this.buttonMine.Text = "Mine";
            this.buttonMine.UseVisualStyleBackColor = true;
            this.buttonMine.Click += new System.EventHandler(this.buttonMine_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(25, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "Status";
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(117, 42);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(109, 20);
            this.labelStatus.TabIndex = 5;
            this.labelStatus.Text = "Not Connected";
            // 
            // textBoxPort
            // 
            this.textBoxPort.Location = new System.Drawing.Point(498, 9);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(125, 27);
            this.textBoxPort.TabIndex = 6;
            // 
            // buttonConnectPort
            // 
            this.buttonConnectPort.Location = new System.Drawing.Point(629, 9);
            this.buttonConnectPort.Name = "buttonConnectPort";
            this.buttonConnectPort.Size = new System.Drawing.Size(121, 29);
            this.buttonConnectPort.TabIndex = 7;
            this.buttonConnectPort.Text = "Connect port";
            this.buttonConnectPort.UseVisualStyleBackColor = true;
            this.buttonConnectPort.Click += new System.EventHandler(this.buttonConnectPort_Click);

            // 
            // infoBlockchain
            // 
            this.infoBlockchain.Location = new System.Drawing.Point(25, 75);
            this.infoBlockchain.Name = "infoBlockchain";
            this.infoBlockchain.Size = new System.Drawing.Size(350, 364);
            this.infoBlockchain.TabIndex = 8;
            this.infoBlockchain.Text = "";
            // 
            // infoMining
            // 
            this.infoMining.Location = new System.Drawing.Point(400, 75);
            this.infoMining.Name = "infoMining";
            this.infoMining.Size = new System.Drawing.Size(350, 364);
            this.infoMining.TabIndex = 9;
            this.infoMining.Text = "";
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(629, 44);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(121, 29);
            this.buttonStop.TabIndex = 10;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(782, 453);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.infoMining);
            this.Controls.Add(this.infoBlockchain);
            this.Controls.Add(this.buttonConnectPort);
            this.Controls.Add(this.textBoxPort);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonMine);
            this.Controls.Add(this.buttonConnect);
            this.Controls.Add(this.textBoxNodeName);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxNodeName;
        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.Button buttonMine;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.Button buttonConnectPort;
        private System.Windows.Forms.RichTextBox infoBlockchain;
        private System.Windows.Forms.RichTextBox infoMining;
        private System.Windows.Forms.Button buttonStop;

        }
    }

