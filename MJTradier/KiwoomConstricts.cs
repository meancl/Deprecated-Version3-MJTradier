using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MJTradier
{
    public partial class Form1
    {

        // ------------------------------------------------------
        // 화면번호 상수
        // ------------------------------------------------------
        public const int REAL_SCREEN_NUM_START = 1000; // 실시간 시작화면번호
        public const int REAL_SCREEN_NUM_END = 1100; // 실시간 마지막화면번호
        public const int TR_SCREEN_NUM_START = 1101; // TR 초기화면번호
        public const int TR_SCREEN_NUM_END = 2000; // TR 마지막화면번호
        public const int TRADE_SCREEN_NUM_START = 2001; // 매매 시작화면번호 
        public const int TRADE_SCREEN_NUM_END = 9999; // 매매 마지막화면전호

        // -------------------------------------------------------
        // 화면번호 변수
        // -------------------------------------------------------
        private int nTrScreenNum = TR_SCREEN_NUM_START;
        private int nRealScreenNum = REAL_SCREEN_NUM_START;
        private int nTradeScreenNum = TRADE_SCREEN_NUM_START;



        // ============================================
        // 매매용 화면번호 재설정 메소드
        // ============================================
        private string SetTradeScreenNo()
        {
            if (nTradeScreenNum > TRADE_SCREEN_NUM_END)
                nTradeScreenNum = TRADE_SCREEN_NUM_START;

            string sTradeScreenNum = nTradeScreenNum.ToString();
            nTradeScreenNum++;
            return sTradeScreenNum;

        }

        // ============================================
        // 실시간용 화면번호 재설정 메소드
        // ============================================
        private string SetRealScreenNo()
        {
            if (nRealScreenNum > REAL_SCREEN_NUM_END)
                nRealScreenNum = REAL_SCREEN_NUM_START;

            string sRealScreenNum = nRealScreenNum.ToString();
            nRealScreenNum++;
            return sRealScreenNum;
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


    }
}
