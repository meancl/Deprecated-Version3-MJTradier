using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;


namespace MJTradier
{
    public partial class Form1 : Form
    {
        // ------------------------------------------------------
        // 스크린번호 변수
        // ------------------------------------------------------

        public const int TR_SCREEN_NUM_START = 1000; // TR 초기화면번호
        public const int TR_SCREEN_NUM_END = 9000; // TR 마지막화면번호

        private int nTrScreenNum = TR_SCREEN_NUM_START;
        public int nStockLength;
        private int nRequestCnt;
        private string[] kosdaqCodes; // 코스닥 종목들을 저장한 문자열 배열 //초기화대상
        private string[] kospiCodes; //  코스피 종목들을 저장한 문자열 배열 //초기화대상



        public Form1()
        {
            InitializeComponent(); // c# 고유 고정메소드  

            MappingFileToStockCodes();


            // --------------------------------------------------
            // Event Handler 
            // --------------------------------------------------
            axKHOpenAPI1.OnEventConnect += OnEventConnectHandler; // 로그인 event slot connect
            axKHOpenAPI1.OnReceiveTrData += OnReceiveTrDataHandler; // TR event slot connect


            testTextBox.AppendText("로그인 시도..\r\n");
            axKHOpenAPI1.CommConnect();
        }


        private void MappingFileToStockCodes()
        {
            kosdaqCodes = System.IO.File.ReadAllLines("today_kosdaq_stock_code.txt");
            kospiCodes = System.IO.File.ReadAllLines("today_kospi_stock_code.txt");

            testTextBox.AppendText("코스닥 총길이 : " + kosdaqCodes.Length.ToString() + "\r\n");
            testTextBox.AppendText("코스피 총길이 : " + kospiCodes.Length.ToString() + "\r\n");
            nStockLength = kosdaqCodes.Length + kospiCodes.Length;
        }

        public void Delay(int ms)
        {
            DateTime dateTimeNow = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, ms);
            DateTime dateTimeAdd = dateTimeNow.Add(duration);
            while (dateTimeAdd >= dateTimeNow)
            {
                System.Windows.Forms.Application.DoEvents();
                dateTimeNow = DateTime.Now;
            }
            return;
        }


        // ============================================
        // Tr용 화면번호 재설정메소드
        // ============================================
        private string SetTrScreenNo()
        {
            if (nTrScreenNum > TR_SCREEN_NUM_END)
                nTrScreenNum = TR_SCREEN_NUM_START;

            string sTrScreenNum = nTrScreenNum.ToString();
            nTrScreenNum++;
            return sTrScreenNum;
        }



        // ============================================
        // 로그인 이벤트발생시 핸들러 메소드
        // ============================================
        private void OnEventConnectHandler(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnEventConnectEvent e)
        {
            if (e.nErrCode == 0) // 로그인 성공
            {
                testTextBox.AppendText("로그인 성공\r\n");
                testTextBox.AppendText("=====================코스닥 신청===============\r\n");
                for (int i = 0; i < kosdaqCodes.Length; i++)
                {
                    testTextBox.AppendText((i + 1).ToString() + "번째 " + kosdaqCodes[i] + " 코스닥 요청 \r\n");
                    RequestBasicStockInfo(kosdaqCodes[i]);
                    Delay(5000);
                }

                testTextBox.AppendText("=====================코스피 신청===============\r\n");
                for (int i = 0; i < kospiCodes.Length; i++)
                {
                    testTextBox.AppendText((i + 1).ToString() + "번째 " + kospiCodes[i] + " 코스피 요청 \r\n");
                    RequestBasicStockInfo(kospiCodes[i]);
                    Delay(5000);
                }

            }
            else
            {
                testTextBox.AppendText("로그인 실패\r\n");
            }
        } // END ---- 로그인 이벤트 핸들러





        // ============================================
        // 주식기본정보요청 TR요청메소드
        // ============================================
        private void RequestBasicStockInfo(string sCode)
        {
            axKHOpenAPI1.SetInputValue("종목코드", sCode);
            axKHOpenAPI1.CommRqData("주식기본정보요청", "opt10001", 0, SetTrScreenNo());
        }

        // ============================================
        // TR 이벤트발생시 핸들러 메소드
        // ============================================
        private void OnReceiveTrDataHandler(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            if (e.sRQName.Equals("주식기본정보요청"))
            {
                string sCode = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "종목코드");

                StreamWriter sw = new StreamWriter(new FileStream(@"기본정보\" + sCode.Trim() + ".txt", FileMode.Create));
                sw.Write(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "종목명").Trim() + "," + (long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "유통주식")) * 1000).ToString() + "," + axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "유통비율").Trim() + "," + axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "250최고가대비율").Trim() + "," + axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "250최저가대비율").Trim() +  (long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "상장주식")) * 1000).ToString() + "," + Math.Abs(int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "현재가"))).ToString());
                sw.Close();

                nRequestCnt++;
                if (nRequestCnt >= nStockLength)
                {
                    testTextBox.AppendText("요청 끝! 종료\r\n");
                    Application.Exit();
                }
            }
        } // END ---- TR 이벤트 핸들러

    }
}
