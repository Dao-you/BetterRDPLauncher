namespace remote_window
{
    partial class Form3
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
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Windows-SSD");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("稍後插入的磁碟機");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("磁碟機", new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2});
            System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("Windows-SSD");
            System.Windows.Forms.TreeNode treeNode5 = new System.Windows.Forms.TreeNode("稍後插入的裝置");
            System.Windows.Forms.TreeNode treeNode6 = new System.Windows.Forms.TreeNode("視訊擷取裝置", new System.Windows.Forms.TreeNode[] {
            treeNode4,
            treeNode5});
            System.Windows.Forms.TreeNode treeNode7 = new System.Windows.Forms.TreeNode("稍後插入的裝置");
            System.Windows.Forms.TreeNode treeNode8 = new System.Windows.Forms.TreeNode("其他支援即插即用 (PnP) 的裝置", new System.Windows.Forms.TreeNode[] {
            treeNode7});
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.groupBox2);
            this.groupBox1.Font = new System.Drawing.Font("新細明體", 10F);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(875, 441);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "本機裝置和資源";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(36, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(382, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "選擇您要在遠端工作階段使用的本機裝置和資源。";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.treeView1);
            this.groupBox2.Controls.Add(this.checkBox4);
            this.groupBox2.Controls.Add(this.checkBox3);
            this.groupBox2.Controls.Add(this.checkBox2);
            this.groupBox2.Controls.Add(this.checkBox1);
            this.groupBox2.Location = new System.Drawing.Point(33, 43);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(804, 392);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Enter += new System.EventHandler(this.groupBox2_Enter);
            // 
            // treeView1
            // 
            this.treeView1.BackColor = System.Drawing.SystemColors.Menu;
            this.treeView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.treeView1.CheckBoxes = true;
            this.treeView1.ItemHeight = 24;
            this.treeView1.Location = new System.Drawing.Point(6, 128);
            this.treeView1.Name = "treeView1";
            treeNode1.Name = "Node2";
            treeNode1.Text = "Windows-SSD";
            treeNode2.Name = "Node3";
            treeNode2.Text = "稍後插入的磁碟機";
            treeNode3.Name = "Node1";
            treeNode3.Text = "磁碟機";
            treeNode4.Name = "Node2";
            treeNode4.Text = "Windows-SSD";
            treeNode5.Name = "Node3";
            treeNode5.Text = "稍後插入的裝置";
            treeNode6.Name = "Node1";
            treeNode6.Text = "視訊擷取裝置";
            treeNode7.Name = "Node4";
            treeNode7.Text = "稍後插入的裝置";
            treeNode8.Name = "Node0";
            treeNode8.Text = "其他支援即插即用 (PnP) 的裝置";
            this.treeView1.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode3,
            treeNode6,
            treeNode8});
            this.treeView1.Scrollable = false;
            this.treeView1.Size = new System.Drawing.Size(489, 239);
            this.treeView1.TabIndex = 4;
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Location = new System.Drawing.Point(28, 102);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(64, 21);
            this.checkBox4.TabIndex = 3;
            this.checkBox4.Text = "位置";
            this.checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(28, 75);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(81, 21);
            this.checkBox3.TabIndex = 2;
            this.checkBox3.Text = "連接埠";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(28, 48);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(316, 21);
            this.checkBox2.TabIndex = 1;
            this.checkBox2.Text = "WebAuthn(Windows Hello 或安全性金鑰)";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(28, 21);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(243, 21);
            this.checkBox1.TabIndex = 0;
            this.checkBox1.Text = "智慧卡或Window Hello企業版";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("新細明體", 10F);
            this.button1.Location = new System.Drawing.Point(596, 453);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(130, 43);
            this.button1.TabIndex = 1;
            this.button1.Text = "確定";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("新細明體", 10F);
            this.button2.Location = new System.Drawing.Point(756, 453);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(130, 43);
            this.button2.TabIndex = 2;
            this.button2.Text = "取消 ";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // Form3
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 510);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form3";
            this.Text = "Form3";
            this.Load += new System.EventHandler(this.Form3_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.TreeView treeView1;
    }
}