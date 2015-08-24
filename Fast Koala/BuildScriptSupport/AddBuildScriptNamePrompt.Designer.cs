﻿namespace Wijits.FastKoala.BuildScriptSupport
{
    partial class AddBuildScriptNamePrompt
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
            this.label1 = new System.Windows.Forms.Label();
            this.txtBuildScriptFileName = new System.Windows.Forms.TextBox();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.cmdOK = new System.Windows.Forms.Button();
            this.rdoInvokeBefore = new System.Windows.Forms.RadioButton();
            this.rdoInvokeAfter = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(182, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Enter a name for the new build script:";
            // 
            // txtBuildScriptFileName
            // 
            this.txtBuildScriptFileName.Location = new System.Drawing.Point(15, 26);
            this.txtBuildScriptFileName.Name = "txtBuildScriptFileName";
            this.txtBuildScriptFileName.Size = new System.Drawing.Size(221, 20);
            this.txtBuildScriptFileName.TabIndex = 1;
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(166, 86);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 2;
            this.cmdCancel.Text = "&Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            // 
            // cmdOK
            // 
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.Location = new System.Drawing.Point(85, 86);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(75, 23);
            this.cmdOK.TabIndex = 3;
            this.cmdOK.Text = "&OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            // 
            // rdoInvokeBefore
            // 
            this.rdoInvokeBefore.AutoSize = true;
            this.rdoInvokeBefore.Location = new System.Drawing.Point(15, 53);
            this.rdoInvokeBefore.Name = "rdoInvokeBefore";
            this.rdoInvokeBefore.Size = new System.Drawing.Size(89, 17);
            this.rdoInvokeBefore.TabIndex = 4;
            this.rdoInvokeBefore.Text = "InvokeBefore";
            this.rdoInvokeBefore.UseVisualStyleBackColor = true;
            // 
            // rdoInvokeAfter
            // 
            this.rdoInvokeAfter.AutoSize = true;
            this.rdoInvokeAfter.Checked = true;
            this.rdoInvokeAfter.Location = new System.Drawing.Point(107, 53);
            this.rdoInvokeAfter.Name = "rdoInvokeAfter";
            this.rdoInvokeAfter.Size = new System.Drawing.Size(80, 17);
            this.rdoInvokeAfter.TabIndex = 5;
            this.rdoInvokeAfter.TabStop = true;
            this.rdoInvokeAfter.Text = "InvokeAfter";
            this.rdoInvokeAfter.UseVisualStyleBackColor = true;
            // 
            // AddBuildScriptNamePrompt
            // 
            this.AcceptButton = this.cmdOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(248, 122);
            this.Controls.Add(this.rdoInvokeAfter);
            this.Controls.Add(this.rdoInvokeBefore);
            this.Controls.Add(this.cmdOK);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.txtBuildScriptFileName);
            this.Controls.Add(this.label1);
            this.Name = "AddBuildScriptNamePrompt";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add Build Script";
            this.Load += new System.EventHandler(this.AddBuildScriptNamePrompt_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtBuildScriptFileName;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.RadioButton rdoInvokeBefore;
        private System.Windows.Forms.RadioButton rdoInvokeAfter;
    }
}