namespace MJTradier
{
    public partial class Form1
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.depositCalcLabel = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.checkMyHoldingsButton = new System.Windows.Forms.Button();
            this.checkMyAccountInfoButton = new System.Windows.Forms.Button();
            this.myNameLabel = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.myDepositLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.accountComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.axKHOpenAPI1 = new AxKHOpenAPILib.AxKHOpenAPI();
            this.testTextBox = new System.Windows.Forms.TextBox();
            this.setOnMarketButton = new System.Windows.Forms.Button();
            this.setDepositCalcButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.sCodeToBuyTextBox = new System.Windows.Forms.TextBox();
            this.buyButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axKHOpenAPI1)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.depositCalcLabel);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.checkMyHoldingsButton);
            this.groupBox1.Controls.Add(this.checkMyAccountInfoButton);
            this.groupBox1.Controls.Add(this.myNameLabel);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.myDepositLabel);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.accountComboBox);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(6, 23);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.groupBox1.Size = new System.Drawing.Size(317, 246);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "내 정보";
            // 
            // depositCalcLabel
            // 
            this.depositCalcLabel.AutoSize = true;
            this.depositCalcLabel.Location = new System.Drawing.Point(122, 125);
            this.depositCalcLabel.Name = "depositCalcLabel";
            this.depositCalcLabel.Size = new System.Drawing.Size(33, 12);
            this.depositCalcLabel.TabIndex = 14;
            this.depositCalcLabel.Text = "0(원)";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(23, 125);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(77, 12);
            this.label5.TabIndex = 13;
            this.label5.Text = "계산용예수금";
            // 
            // checkMyHoldingsButton
            // 
            this.checkMyHoldingsButton.Location = new System.Drawing.Point(200, 192);
            this.checkMyHoldingsButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkMyHoldingsButton.Name = "checkMyHoldingsButton";
            this.checkMyHoldingsButton.Size = new System.Drawing.Size(111, 23);
            this.checkMyHoldingsButton.TabIndex = 12;
            this.checkMyHoldingsButton.Text = "보유종목확인";
            this.checkMyHoldingsButton.UseVisualStyleBackColor = true;
            // 
            // checkMyAccountInfoButton
            // 
            this.checkMyAccountInfoButton.Location = new System.Drawing.Point(82, 192);
            this.checkMyAccountInfoButton.Name = "checkMyAccountInfoButton";
            this.checkMyAccountInfoButton.Size = new System.Drawing.Size(109, 23);
            this.checkMyAccountInfoButton.TabIndex = 11;
            this.checkMyAccountInfoButton.Text = "예수금확인";
            this.checkMyAccountInfoButton.UseVisualStyleBackColor = true;
            // 
            // myNameLabel
            // 
            this.myNameLabel.AutoSize = true;
            this.myNameLabel.Location = new System.Drawing.Point(122, 63);
            this.myNameLabel.Name = "myNameLabel";
            this.myNameLabel.Size = new System.Drawing.Size(41, 12);
            this.myNameLabel.TabIndex = 10;
            this.myNameLabel.Text = "아무개";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(23, 63);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 12);
            this.label4.TabIndex = 9;
            this.label4.Text = "예금주";
            // 
            // myDepositLabel
            // 
            this.myDepositLabel.AutoSize = true;
            this.myDepositLabel.Location = new System.Drawing.Point(122, 96);
            this.myDepositLabel.Name = "myDepositLabel";
            this.myDepositLabel.Size = new System.Drawing.Size(33, 12);
            this.myDepositLabel.TabIndex = 8;
            this.myDepositLabel.Text = "0(원)";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(23, 96);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 12);
            this.label2.TabIndex = 7;
            this.label2.Text = "예수금";
            // 
            // accountComboBox
            // 
            this.accountComboBox.FormattingEnabled = true;
            this.accountComboBox.Location = new System.Drawing.Point(124, 32);
            this.accountComboBox.Name = "accountComboBox";
            this.accountComboBox.Size = new System.Drawing.Size(121, 20);
            this.accountComboBox.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "계좌번호";
            // 
            // axKHOpenAPI1
            // 
            this.axKHOpenAPI1.Enabled = true;
            this.axKHOpenAPI1.Location = new System.Drawing.Point(362, 458);
            this.axKHOpenAPI1.Name = "axKHOpenAPI1";
            this.axKHOpenAPI1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axKHOpenAPI1.OcxState")));
            this.axKHOpenAPI1.Size = new System.Drawing.Size(125, 63);
            this.axKHOpenAPI1.TabIndex = 3;
            // 
            // testTextBox
            // 
            this.testTextBox.Location = new System.Drawing.Point(328, 34);
            this.testTextBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.testTextBox.Multiline = true;
            this.testTextBox.Name = "testTextBox";
            this.testTextBox.ReadOnly = true;
            this.testTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.testTextBox.Size = new System.Drawing.Size(795, 488);
            this.testTextBox.TabIndex = 9;
            // 
            // setOnMarketButton
            // 
            this.setOnMarketButton.Location = new System.Drawing.Point(6, 274);
            this.setOnMarketButton.Name = "setOnMarketButton";
            this.setOnMarketButton.Size = new System.Drawing.Size(109, 31);
            this.setOnMarketButton.TabIndex = 10;
            this.setOnMarketButton.Text = "강제 장시작";
            this.setOnMarketButton.UseVisualStyleBackColor = true;
            // 
            // setDepositCalcButton
            // 
            this.setDepositCalcButton.Location = new System.Drawing.Point(130, 276);
            this.setDepositCalcButton.Name = "setDepositCalcButton";
            this.setDepositCalcButton.Size = new System.Drawing.Size(109, 29);
            this.setDepositCalcButton.TabIndex = 15;
            this.setDepositCalcButton.Text = "계산용예수금확인";
            this.setDepositCalcButton.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(29, 352);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 16;
            this.label3.Text = "종목코드";
            // 
            // sCodeToBuyTextBox
            // 
            this.sCodeToBuyTextBox.Location = new System.Drawing.Point(121, 349);
            this.sCodeToBuyTextBox.Name = "sCodeToBuyTextBox";
            this.sCodeToBuyTextBox.Size = new System.Drawing.Size(100, 21);
            this.sCodeToBuyTextBox.TabIndex = 18;
            // 
            // buyButton
            // 
            this.buyButton.Location = new System.Drawing.Point(191, 397);
            this.buyButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buyButton.Name = "buyButton";
            this.buyButton.Size = new System.Drawing.Size(111, 48);
            this.buyButton.TabIndex = 15;
            this.buyButton.Text = "주문신청";
            this.buyButton.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1148, 533);
            this.Controls.Add(this.buyButton);
            this.Controls.Add(this.sCodeToBuyTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.setDepositCalcButton);
            this.Controls.Add(this.setOnMarketButton);
            this.Controls.Add(this.testTextBox);
            this.Controls.Add(this.axKHOpenAPI1);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axKHOpenAPI1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox accountComboBox;
        private System.Windows.Forms.Label label1;
        private AxKHOpenAPILib.AxKHOpenAPI axKHOpenAPI1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label myNameLabel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label myDepositLabel;
        private System.Windows.Forms.Button checkMyAccountInfoButton;
        private System.Windows.Forms.Button checkMyHoldingsButton;
        private System.Windows.Forms.TextBox testTextBox;
        private System.Windows.Forms.Button setOnMarketButton;
        private System.Windows.Forms.Label depositCalcLabel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button setDepositCalcButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox sCodeToBuyTextBox;
        private System.Windows.Forms.Button buyButton;
    }
}

