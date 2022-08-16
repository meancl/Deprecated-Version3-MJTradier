using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

// ========================================================================
// 철학 : Being simple is the best.
// ========================================================================
namespace MJTradier
{
    public partial class Form1 : Form
    {

        public Form1()
        {

            InitializeComponent(); // c# 고유 고정메소드  

            MappingFileToStockCodes();
            dtBeforeOrderTime = DateTime.Now; // 이전요청시간 초기화


            // --------------------------------------------------
            // Winform Event Handler 
            // --------------------------------------------------
            checkMyAccountInfoButton.Click += Button_Click;
            checkMyHoldingsButton.Click += Button_Click;
            setOnMarketButton.Click += Button_Click;//삭제
            setDepositCalcButton.Click += Button_Click;//삭제
            buyButton.Click += Button_Click;


            // --------------------------------------------------
            // Event Handler 
            // --------------------------------------------------
            axKHOpenAPI1.OnEventConnect += OnEventConnectHandler; // 로그인 event slot connect
            axKHOpenAPI1.OnReceiveTrData += OnReceiveTrDataHandler; // TR event slot connect
            axKHOpenAPI1.OnReceiveRealData += OnReceiveRealDataHandler; // 실시간 event slot connect
            axKHOpenAPI1.OnReceiveChejanData += OnReceiveChejanDataHandler; // 체결,접수,잔고 event slot connect

            testTextBox.AppendText("로그인 시도..\r\n"); //++
            axKHOpenAPI1.CommConnect();
        }

    }
}
