using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MJTradier
{
    public partial class Form1
    {
        public const int BRUSH = 20;
        public Random rand = new Random(); // real
        public Queue<TradeRequestSlot> tradeQueue = new Queue<TradeRequestSlot>(); // 매매신청을 담는 큐, 매매컨트롤러가 사용할 큐 // real
        TradeRequestSlot curSlot; // 임시로 사용하능한 매매요청, 매매컨트롤러 변수 // real
        public bool isMarketStart; // true면 장중, false면 장시작전,장마감후 // real
        public int nSharedTime; // 모든 종목들이 공유하는 현재시간( 주식체결 실시간 데이터만 기록, 호가잔량 실시간은 시간이 정렬돼있지 않음 ) // real
        public bool isForCheckHoldings; // 현재잔고를 확인만 하기위한 기능 // real
        public int nMaxPriceForEachStock = MILLION;   // 각 종목이 한번에 최대 살 수 있는 금액 ex. 삼백만원 // real
        public DateTime dtBeforeOrderTime; // real
        public DateTime dtCurOrderTime; // real
        public int nReqestCount; // real
        public bool isSendOrder; // real
        public bool isForbidTrade; // real
        public int nFirstTime; // real
        public StockPiece tmpStockPiece; // real
        public StockDashBoard stockDashBoard; // real
        public int nPrevBoardUpdateTime; // real
        public int nTimeLineIdx = BRUSH; // real
        public double[] arrFSlope = new double[PICKUP_CNT]; // real
        public double[] arrRecentFSlope = new double[PICKUP_CNT]; // real
        public double[] arrHourFSlope = new double[PICKUP_CNT]; // real
        StreamWriter DashSw = new StreamWriter(new FileStream(sMessageLogPath + "게시판.txt", FileMode.Create)); // real

        


        public const int EYES_CLOSE_CRUSH_NUM = 3;

        public const double fPushWeight = 0.8;
        public const double fRoughPushWeight = 0.6;
        public const int SHORT_UPDATE_TIME = 20;
        public const int MAX_REQ_SEC = 150; // 최대매수요청시간
        public const int BUY_SLOT_MAX_NUM = 5; // 최대 매매블록 갯수

        // ------------------------------------------------------
        // 추세 상수
        // ------------------------------------------------------
        public const int PICKUP_CNT = 300;
        public const int PICK_BEFORE = 15; // 15분전
        public const int HOUR_BEFORE = 60; // 1시간전
        public const int TEN_SEC_PICKUP_CNT = 300;
        public const int TEN_SEC_PICK_BEFORE = 30; // 5분전
        public const int TEN_SEC_TEN_BEFORE = 60; // 10분전
        public const int MIN_DENOM = 1; // 기울기를 구할때 최소 분모는 MIN_DENOM.
        public const int MAX_DENOM = 90; // 기울기를 구할때 최대 분모. 쓸지 안쓸지 모름
        public const int COMMON_DENOM = 30; // 기울기를 구할때 공통분모. 현재 공통분모로 사용중
        public const int HOUR_COMMON_DENOM = 20; // 기울기를 구할때 공통분모. 현재 공통분모로 사용중
        public const int RECENT_COMMON_DENOM = 10; // 기울기를 구할때 공통분모. 현재 공통분모로 사용중

        // ------------------------------------------------------
        // 이평선 상수
        // ------------------------------------------------------
        public const int MA5M = 5;
        public const int MA20M = 20;
        public const int MA1H = 60;
        public const int MA2H = 120;
        public const int MA_EXCEED_CNT = 30; // ma가 현재값 위나 아래 한 공간에 계속 머무는 횟수가 MA_EXCEED_CNT이었다가 다른 공간으로 넘어가면 역전된다는 의미

        public const int TEN_SEC_MA2M = 18;
        public const int TEN_SEC_MA10M = 60;
        public const int TEN_SEC_MA20M = 120;
        // ------------------------------------------------------
        // 임시파일저장명 상수
        // ------------------------------------------------------
        private const string sMessageLogPath = @"로그\";
        private const string sEachPath = @"로그\매수후정보\"; // 두두두둥

        // ------------------------------------------------------
        // 대응영역 상수
        // ------------------------------------------------------
        public const int PREEMPTION_ACCESS_SEC = 120;
        public const int PREEMPTION_UPDATE_SEC = 10;
        public const int RESPITE_UPDATE_SEC = 10;
        public const int RESPITE_LIMIT_SEC = 600;
        public const double RESPITE_INIT = -100;

        // ============================================
        // 실시간 이벤트발생시 핸들러메소드 
        // ============================================
        private void OnReceiveRealDataHandler(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveRealDataEvent e)
        {


            string sCode = e.sRealKey; // 종목코드

            bool isHogaJanRyang = false;
            bool isZooSikCheGyul = false;

            // bool 변수로 주식호가잔량과 주식체결을 위에서 하는 이유는  
            // 시간초기화를 먼저해서 게시판 업데이트를 해당시간에서 가장 빠르게 진행하기 위함.
            if (e.sRealType.Equals("주식호가잔량"))
            {
                isHogaJanRyang = true;
            }
            else if (e.sRealType.Equals("주식체결"))
            {
                isZooSikCheGyul = true;
                nSharedTime = Math.Abs(int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 20))); // 현재시간

                if (nFirstTime == 0) // 첫 시간이 설정되지 않았다면 
                {
                    nFirstTime = nSharedTime - nSharedTime % MINUTE_KIWOOM; // x시간 00분 00 초 형태로 만든다
                }

                if (nPrevBoardUpdateTime == 0) // 이전업데이트 시간이 초기화되지 않았다면 
                {
                    nPrevBoardUpdateTime = nFirstTime; // 장초반시간으로 업데이트한다.
                }
            }
            else if (e.sRealType.Equals("장시작시간"))
            {
                string sGubun = axKHOpenAPI1.GetCommRealData(e.sRealKey, 215); // 장운영구분 0 :장시작전, 3 : 장중, 4 : 장종료
                string sTime = axKHOpenAPI1.GetCommRealData(e.sRealKey, 20); // 체결시간
                string sTimeRest = axKHOpenAPI1.GetCommRealData(e.sRealKey, 214); // 잔여시간
                if (sGubun.Equals("0")) // 장시작 전
                {
                    testTextBox.AppendText(sTimeRest.ToString() + " : 장시작전\r\n"); ;//++

                    isForCheckHoldings = true;
                    RequestHoldings(0);
                }
                else if (sGubun.Equals("3")) // 장 중
                {
                    testTextBox.AppendText("장중\r\n");//++
                    isMarketStart = true;
                    nFirstTime = int.Parse(sTime);
                    nFirstTime -= nFirstTime % MINUTE_KIWOOM;
                    nSharedTime = nFirstTime;
                    nPrevBoardUpdateTime = nFirstTime;
                    RequestHoldings(0); // 장시작하고 잔여종목 전량매도
                }
                else
                {
                    if (sGubun.Equals("2")) // 장 종료 10분전 동시호가
                    {
                        testTextBox.AppendText(sTimeRest.ToString() + " : 장종료전\r\n");//++
                        isMarketStart = false;
                        RequestHoldings(0); // 장 끝나기 전 잔여종목 전량매도
                    }
                    else if (sGubun.Equals("4")) // 장 종료
                    {
                        testTextBox.AppendText("장종료\r\n");//++
                        isMarketStart = false;
                        isForCheckHoldings = true;
                        RequestHoldings(0);
                        RequestTradeResult();
                        DashSw.Close();
                        // 두두두둥
                        for(int i = 0;  i < nStockLength; i++)
                        {
                            for(int j = 0; j < ea[i].myTradeManager.nIdx; i++)
                            {
                                ea[i].myTradeManager.arrBuyedSlots[j].eachSw.Close();
                            }
                        }
                    }
                }
            }

            if (!isMarketStart) // 장 시작하면 찾아와라.. 
                return;


            // =============================================================
            // 정기적 업데이트 
            // =============================================================
            // nPrevBoardUpdateTime은 장시작시간 + MINUTE_SEC 에 처음 접근이 가능해야함.
            if (SubTimeToTimeAndSec(nSharedTime, nPrevBoardUpdateTime) >= MINUTE_SEC) // 매 분마다 업데이트 진행 
            {
                // ===========================================================================================================
                // 게시판 Part
                // ===========================================================================================================
                // 매 분마다 해당 개인구조체.rankSystem에 업데이트된 순위를 삽입해준다.
                int i, j;
                // 모든 종목 개인구조체에서 거래대금,거래량,속도 등의 정보를 가져온다.
                // 분당 거래대금 등 분당시리즈는 데이터를 받은 후 초기화한다.
                // 순위합 역시 초기화한다.
                for (i = 0; i < nStockLength; i++) // 업데이트
                {
                    // 전체
                    tmpStockPiece = stockDashBoard.stockPanel[i]; // 구조체는 값형식이라 직접대입을 할 수 없다.(클래스로 하면 되긴 하는데 부가적인 성능부하가 생길거같아서) 
                    tmpStockPiece.lTotalTradePrice = ea[tmpStockPiece.nEaIdx].lTotalTradePrice; // 1. 거래대금 
                    tmpStockPiece.fTotalTradeVolume = ea[tmpStockPiece.nEaIdx].fTotalTradeVolume; // 2. 상대적거래수량
                    tmpStockPiece.lTotalBuyPrice = ea[tmpStockPiece.nEaIdx].lTotalBuyPrice; // 3. 매수대금
                    tmpStockPiece.fTotalBuyVolume = ea[tmpStockPiece.nEaIdx].fTotalBuyVolume; // 4. 상대적매수수량
                    tmpStockPiece.nAccumCount = ea[tmpStockPiece.nEaIdx].nCnt; // 5. 누적카운트
                    tmpStockPiece.fTotalPowerWithOutGap = ea[tmpStockPiece.nEaIdx].fPowerWithoutGap; // 6. 손익률
                    tmpStockPiece.lMarketCap = ea[tmpStockPiece.nEaIdx].lMarketCap; // 7. 시가총액
                    tmpStockPiece.nSummationRank = 0; // 8. 순위합 초기화

                    // 분당
                    tmpStockPiece.lMinuteTradePrice = ea[tmpStockPiece.nEaIdx].lMinuteTradePrice; // 1. 분간 거래대금
                    tmpStockPiece.fMinuteTradeVolume = ea[tmpStockPiece.nEaIdx].fMinuteTradeVolume; // 2. 분간 상대적거래수량
                    tmpStockPiece.lMinuteBuyPrice = ea[tmpStockPiece.nEaIdx].lMinuteBuyPrice; // 3. 분간 매수대금
                    tmpStockPiece.fMinuteBuyVolume = ea[tmpStockPiece.nEaIdx].fMinuteBuyVolume; // 4. 분간 상대적매수수량
                    tmpStockPiece.fMinutePower = ea[tmpStockPiece.nEaIdx].fMinutePower; // 5. 분간 손익율
                    tmpStockPiece.nMinuteCnt = ea[tmpStockPiece.nEaIdx].nMinuteCnt; // 6. 분간 카운트
                    tmpStockPiece.nMinuteUpDown = ea[tmpStockPiece.nEaIdx].nMinuteUpDown; // 7. 분간 위아래
                    tmpStockPiece.nSummationMinuteRank = 0; // 8. 분간 순위합 초기화

                    // 분간 데이터재료 초기화
                    ea[tmpStockPiece.nEaIdx].lMinuteTradePrice = 0; // 1
                    ea[tmpStockPiece.nEaIdx].fMinuteTradeVolume = 0; // 2
                    ea[tmpStockPiece.nEaIdx].lMinuteBuyPrice = 0; // 3
                    ea[tmpStockPiece.nEaIdx].fMinuteBuyVolume = 0; // 4
                    ea[tmpStockPiece.nEaIdx].lMinuteTradeVolume = 0; // 2- 재료
                    ea[tmpStockPiece.nEaIdx].lMinuteBuyVolume = 0; // 4- 재료
                    ea[tmpStockPiece.nEaIdx].fMinutePower = 0; // 5
                    ea[tmpStockPiece.nEaIdx].nMinuteCnt = 0; // 6
                    ea[tmpStockPiece.nEaIdx].nMinuteUpDown = 0; // 7

                    stockDashBoard.stockPanel[i] = tmpStockPiece;


                }
                stockDashBoard.nDashBoardCnt++;

                // -------------------------------------
                // -------------------------------------
                // 전체 순위설정
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.lTotalTradePrice.CompareTo(x.lTotalTradePrice)); // 1. 거래대금 내림차순 ( 대금은 높을 수 록 좋다)
                //stockDashBoard.stockPanel = stockDashBoard.stockPanel.OrderByDescending<StockPiece, double>(x => x.lTotalTradePrice).ToArray<StockPiece>();
                for (i = 0; i < nStockLength; i++) // 거래대금 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nTotalTradePriceRanking = i + 1; // 1위부터 ~ 2000위까지
                    stockDashBoard.stockPanel[i].nSummationRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.fTotalTradeVolume.CompareTo(x.fTotalTradeVolume)); // 2. 상대적거래수량 내림차순 ( 수량은 높을 수 록 좋다)
                for (i = 0; i < nStockLength; i++) // 상대적거래수량 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nTotalTradeVolumeRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.lTotalBuyPrice.CompareTo(x.lTotalBuyPrice)); // 3. 매수대금 내림차순 ( 대금은 높을 수 록 좋다)
                for (i = 0; i < nStockLength; i++) // 매수대금 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nTotalBuyPriceRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.fTotalBuyVolume.CompareTo(x.fTotalBuyVolume)); // 4. 상대적매수수량 내림차순( 수량은 높을 수 록 좋다)
                for (i = 0; i < nStockLength; i++) // 상대적매수수량 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nTotalBuyVolumeRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.nAccumCount.CompareTo(x.nAccumCount)); // 5. 누적카운트 내림차순(속도는 빠를 수 록 좋다)
                for (i = 0; i < nStockLength; i++) // 누적카운트 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nAccumCountRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.fTotalPowerWithOutGap.CompareTo(x.fTotalPowerWithOutGap)); // 6. 손익률 내림차순(가격은 오를 수 록 좋다)
                for (i = 0; i < nStockLength; i++) // 손익률 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nPowerRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.lMarketCap.CompareTo(x.lMarketCap));  // 7. 시가총액 내림차순( 가격은 높을 수 록 좋다 )
                for (i = 0; i < nStockLength; i++) // 시가총액 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nMarketCapRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationRank += i + 1;
                }
                // -----------------
                // 바로 위까지 순위합을 구하고 이제 정렬
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => x.nSummationRank.CompareTo(y.nSummationRank));  // 8. 순위합 기준 오름차순( 순위는 낮을 수 록 좋다 )
                for (i = 0; i < nStockLength; i++)  // 전체순위합 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nSummationRanking = i + 1;
                }


                // -------------------------------------
                // -------------------------------------
                // 분간 순위설정
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.lMinuteTradePrice.CompareTo(x.lMinuteTradePrice));  // 1. 분당 거래대금 내림차순 ( 대금은 높을 수 록 좋다 )
                for (i = 0; i < nStockLength; i++) // 분당 거래대금 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nMinuteTradePriceRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationMinuteRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.fMinuteTradeVolume.CompareTo(x.fMinuteTradeVolume));  // 2. 분당 상대적거래수량 내림차순( 수량은 높을 수록 좋다 )
                for (i = 0; i < nStockLength; i++) // 분당 상대적거래수량 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nMinuteTradeVolumeRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationMinuteRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.lMinuteBuyPrice.CompareTo(x.lMinuteBuyPrice));  // 3. 분당 매수대금 내림차순 ( 대금은 높을 수 록 좋다 )
                for (i = 0; i < nStockLength; i++) // 분당 매수대금 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nMinuteBuyPriceRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationMinuteRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.fMinuteBuyVolume.CompareTo(x.fMinuteBuyVolume));  // 4. 분당 상대적매수수량 내림차순( 수량은 높을 수록 좋다 )
                for (i = 0; i < nStockLength; i++) // 분당 상대적매수수량 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nMinuteBuyVolumeRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationMinuteRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.fMinutePower.CompareTo(x.fMinutePower));  // 5. 분당 손익율 내림차순( 손익율은 높을 수록 좋다 )
                for (i = 0; i < nStockLength; i++) // 분당 손익율 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nMinutePowerRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationMinuteRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.nMinuteCnt.CompareTo(x.nMinuteCnt));  // 6. 분당 카운트 내림차순( 속도는 빠를 수 록 좋다 )
                for (i = 0; i < nStockLength; i++) // 분당 카운트 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nMinuteCountRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationMinuteRank += i + 1;
                }
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => y.nMinuteUpDown.CompareTo(x.nMinuteUpDown));  // 7. 분당 위아래 내림차순( 위아래가 많을 수 록 좋다 )
                for (i = 0; i < nStockLength; i++) // 분당 카운트 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nMinuteUpDownRanking = i + 1;
                    stockDashBoard.stockPanel[i].nSummationMinuteRank += i + 1;
                }
                // -----------------
                // 바로 위까지 분당 순위합을 구하고 이제 정렬
                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => x.nSummationMinuteRank.CompareTo(y.nSummationMinuteRank));  // 8. 분당 순위합 기준 오름차순( 순위는 낮을 수 록 좋다 )
                for (i = 0; i < nStockLength; i++)  // 분당순위합 순위설정
                {
                    ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nMinuteSummationRanking = i + 1;
                }




                int nRankIdx;
                for (i = 0; i < nStockLength; i++) // 순위 지정
                {
                    nRankIdx = stockDashBoard.stockPanel[i].nEaIdx;
                    ea[nRankIdx].rankSystem.nTime = nSharedTime;

                    Ranking tmpRank;
                    tmpRank.nRecordTime = ea[nRankIdx].rankSystem.nTime; // 기록시간 = 해당시간

                    // 전체
                    tmpRank.nTotalTradePriceRanking = ea[nRankIdx].rankSystem.nTotalTradePriceRanking; // 1. 거래대금 순위 지정
                    tmpRank.nTotalTradeVolumeRanking = ea[nRankIdx].rankSystem.nTotalTradeVolumeRanking; // 2. 상대적거래수량 순위 지정
                    tmpRank.nTotalBuyPriceRanking = ea[nRankIdx].rankSystem.nTotalBuyPriceRanking; // 3. 매수대금 순위 지정
                    tmpRank.nTotalBuyVolumeRanking = ea[nRankIdx].rankSystem.nTotalBuyVolumeRanking; // 4. 상대적매수수량 순위 지정
                    tmpRank.nAccumCountRanking = ea[nRankIdx].rankSystem.nAccumCountRanking; // 5. 누적카운트 순위 지정
                    tmpRank.nPowerRanking = ea[nRankIdx].rankSystem.nPowerRanking; // 6. 손익률 순위 지정
                    tmpRank.nMarketCapRanking = ea[nRankIdx].rankSystem.nMarketCapRanking; // 7. 시가총액 순위 지정
                    tmpRank.nSummationRanking = ea[nRankIdx].rankSystem.nSummationRanking; // 8. 전체 순위합 순위 지정

                    ea[nRankIdx].rankSystem.nSummationMove = ea[nRankIdx].rankSystem.nSummationRanking - ea[nRankIdx].rankSystem.nPrevSummationRanking; // 순위 변동 지정
                    ea[nRankIdx].rankSystem.nPrevSummationRanking = ea[nRankIdx].rankSystem.nSummationRanking; // 현재 총순위 기록

                    // 분당
                    tmpRank.nMinuteTradePriceRanking = ea[nRankIdx].rankSystem.nMinuteTradePriceRanking; // 1. 분당 거래대금 순위 지정 
                    tmpRank.nMinuteTradeVolumeRanking = ea[nRankIdx].rankSystem.nMinuteTradeVolumeRanking; // 2. 분당 상대적거래수량 순위 지정 
                    tmpRank.nMinuteBuyPriceRanking = ea[nRankIdx].rankSystem.nMinuteBuyPriceRanking; // 3. 분당 매수대금 순위 지정 
                    tmpRank.nMinuteBuyVolumeRanking = ea[nRankIdx].rankSystem.nMinuteBuyVolumeRanking; // 4. 분당 상대적매수수량 순위 지정 
                    tmpRank.nMinutePowerRanking = ea[nRankIdx].rankSystem.nMinutePowerRanking; // 5. 분당 손익율 순위 지정
                    tmpRank.nMinuteCountRanking = ea[nRankIdx].rankSystem.nMinuteCountRanking; // 6. 분당 카운트 순위 지정
                    tmpRank.nMinuteUpDownRanking = ea[nRankIdx].rankSystem.nMinuteUpDownRanking; // 7. 분당 위아래 순위 지정
                    tmpRank.nMinuteRanking = ea[nRankIdx].rankSystem.nMinuteSummationRanking; // 8. 분당 순위합 순위 지정

                    if (ea[nRankIdx].rankSystem.nSummationRanking <= 10) // 10위권 내
                        ea[nRankIdx].rankSystem.nRankHold10++;
                    else
                        ea[nRankIdx].rankSystem.nRankHold10 = 0;

                    if (ea[nRankIdx].rankSystem.nSummationRanking <= 20) // 20위권 내
                        ea[nRankIdx].rankSystem.nRankHold20++;
                    else
                        ea[nRankIdx].rankSystem.nRankHold20 = 0;

                    if (ea[nRankIdx].rankSystem.nSummationRanking <= 50) // 50위권 내
                        ea[nRankIdx].rankSystem.nRankHold50++;
                    else
                        ea[nRankIdx].rankSystem.nRankHold50 = 0;

                    if (ea[nRankIdx].rankSystem.nSummationRanking <= 100) // 100위권 내
                        ea[nRankIdx].rankSystem.nRankHold100++;
                    else 
                        ea[nRankIdx].rankSystem.nRankHold100 = 0;

                    if (ea[nRankIdx].rankSystem.nSummationRanking <= 200) // 200위권 내
                        ea[nRankIdx].rankSystem.nRankHold200++;
                    else
                        ea[nRankIdx].rankSystem.nRankHold200 = 0;
                    // 별도로 ea[stockDashBoard.stockPanel[i].nEaIdx].rankSystem.nCurIdx를 작업 안해줘도 된다, 0이었다가 1이 됐으니 이상태에서
                    // 누적순위 / nCurIdx하면 된다.
                    // 전체 누적합

                    ea[nRankIdx].rankSystem.arrRanking[ea[nRankIdx].rankSystem.nCurIdx++] = tmpRank; // arrRanking[0]은 장시작시간 + MINUTE_SEC
                }


                Array.Sort<StockPiece>(stockDashBoard.stockPanel, (x, y) => x.nSummationRank.CompareTo(y.nSummationRank));  // sWriter 기입용 정렬 ( 종합순위 오름차순 )




                // ===========================================================================================================
                // 각 개인구조체 업데이트
                // ===========================================================================================================
                for (i = 0; i < nStockLength; i++)
                {

                    if (!ea[i].isFirstCheck)
                    {
                        ea[i].nJumpCnt++;
                        continue;
                    }

                    // ===========================================================================================================
                    // 타임라인 Part
                    // ===========================================================================================================
                    {
                        ea[i].timeLines1m.nRealDataIdx = ea[i].timeLines1m.nPrevTimeLineIdx; // 지금은 nRealDataIdx인것
                        ea[i].timeLines1m.nPrevTimeLineIdx++; // 다음 페이즈로 넘어간다는 느낌, 갯수를 기준으로 할 때 유용함
                        ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nTimeIdx = nTimeLineIdx; // 배열원소에 현재 타임라인 인덱스 삽입
                        ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nTime = AddTimeBySec(nFirstTime, (nTimeLineIdx - BRUSH) * MINUTE_SEC); // 9:00 ~~
                        if (ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs == 0)
                        {
                            if (ea[i].timeLines1m.nFsPointer == 0)
                            {
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nStartFs = ea[i].nTodayStartPrice;
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs = ea[i].nTodayStartPrice;
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nMaxFs = ea[i].nTodayStartPrice;
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nMinFs = ea[i].nTodayStartPrice;
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nUpFs = ea[i].nTodayStartPrice;
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nDownFs = ea[i].nTodayStartPrice;
                            }
                            else
                            {
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nStartFs = ea[i].timeLines1m.nFsPointer;
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs = ea[i].timeLines1m.nFsPointer;
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nMaxFs = ea[i].timeLines1m.nFsPointer;
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nMinFs = ea[i].timeLines1m.nFsPointer;
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nUpFs = ea[i].timeLines1m.nFsPointer;
                                ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nDownFs = ea[i].timeLines1m.nFsPointer;
                            }
                        }

                        // -----------------------------------------------------------------
                        // 초기화 영역( 조건 설정을 위한 변수들의 )
                        // -----------------------------------------------------------------
                        {
                            if (ea[i].timeLines1m.nMaxUpFs < ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nUpFs) // 최대값 구하기
                                ea[i].timeLines1m.nMaxUpFs = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nUpFs;
                        }

                    } // END ---- 타임라인


                    // ===========================================================================================================
                    // 추세 Part 
                    // ===========================================================================================================
                    {
                        int nPick1, nPick2, nPick3, nPick4, nPick5, nPick6;
                        int nLeftPick1, nRightPick2, nLeftPick3, nRightPick4, nLeftPick5, nRightPick6;

                        // 기울기 확인
                        for (j = 0; j < PICKUP_CNT; j++)
                        {

                            nPick1 = nPick2 = nPick3 = nPick4 = nPick5 = nPick6 = 0;
                            nLeftPick1 = nRightPick2 = nLeftPick3 = nRightPick4 = nLeftPick5 = nRightPick6 = 0;

                            int nRecentPickBefore = Min(PICK_BEFORE, ea[i].timeLines1m.nPrevTimeLineIdx);
                            int nHourPickBefore = Min(HOUR_BEFORE, ea[i].timeLines1m.nPrevTimeLineIdx);

                            while ((nPick1 == nPick2) || (nPick3 == nPick4) || (nPick5 == nPick6)) // 같으면 기울기를 구할 수 없기 때문에 서로 다를때까지 반복한다.
                            {
                                if (nPick1 == nPick2)
                                {
                                    nPick1 = rand.Next(ea[i].timeLines1m.nPrevTimeLineIdx);  // 0 ~ 현재인덱스
                                    nPick2 = rand.Next(ea[i].timeLines1m.nPrevTimeLineIdx);  // 0 ~ 현재인덱스
                                    nLeftPick1 = Min(nPick1, nPick2);
                                    nRightPick2 = Max(nPick1, nPick2);
                                }
                                if(nPick3 == nPick4)
                                {
                                    nPick3 = rand.Next(ea[i].timeLines1m.nPrevTimeLineIdx - nRecentPickBefore, ea[i].timeLines1m.nPrevTimeLineIdx);
                                    nPick4 = rand.Next(ea[i].timeLines1m.nPrevTimeLineIdx - nRecentPickBefore, ea[i].timeLines1m.nPrevTimeLineIdx);
                                    nLeftPick3 = Min(nPick3, nPick4);
                                    nRightPick4 = Max(nPick3, nPick4);
                                }
                                if(nPick5 == nPick6)
                                {
                                    nPick5 = rand.Next(ea[i].timeLines1m.nPrevTimeLineIdx - nHourPickBefore, ea[i].timeLines1m.nPrevTimeLineIdx);
                                    nPick6 = rand.Next(ea[i].timeLines1m.nPrevTimeLineIdx - nHourPickBefore, ea[i].timeLines1m.nPrevTimeLineIdx);
                                    nLeftPick5 = Min(nPick5, nPick6);
                                    nRightPick6 = Max(nPick5, nPick6);
                                }
                            }


                            // 왜 gap으로 나누냐면.... 간편해서
                            // Q. 그러면 종목들 간 값의 차이가 나지 않느냐?? 9900짜리는 50으로 나누고 10000짜리는 100으로 나누는데??
                            // A. 100으로 나눈다면 기울기값은 낮아진다. => 패널티를 받게된다. => 종목들 간 gap으로 나누는건 균형을 맞춘다(?? : 원래 한 단계를 넘을때 더 많은 저항이 생길 수 있으니까 패널티를 주는게 괜찮은거 같다는게 내 의견.)
                            double fSlope = (double)(ea[i].timeLines1m.arrTimeLine[nRightPick2].nLastFs - ea[i].timeLines1m.arrTimeLine[nLeftPick1].nLastFs) / GetBetweenMinAndMax( ea[i].timeLines1m.arrTimeLine[nRightPick2].nTimeIdx - ea[i].timeLines1m.arrTimeLine[nLeftPick1].nTimeIdx, MIN_DENOM, MAX_DENOM); 
                            fSlope /= GetAutoGap(ea[i].nMarketGubun, ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs);

                            double fRecentSlope = (double)(ea[i].timeLines1m.arrTimeLine[nRightPick4].nLastFs - ea[i].timeLines1m.arrTimeLine[nLeftPick3].nLastFs) / GetBetweenMinAndMax( ea[i].timeLines1m.arrTimeLine[nRightPick4].nTimeIdx - ea[i].timeLines1m.arrTimeLine[nLeftPick3].nTimeIdx, MIN_DENOM, MAX_DENOM);
                            fRecentSlope /= GetAutoGap(ea[i].nMarketGubun, ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs);

                            double fHourSlope = (double)(ea[i].timeLines1m.arrTimeLine[nRightPick6].nLastFs - ea[i].timeLines1m.arrTimeLine[nLeftPick5].nLastFs) / GetBetweenMinAndMax(ea[i].timeLines1m.arrTimeLine[nRightPick6].nTimeIdx - ea[i].timeLines1m.arrTimeLine[nLeftPick5].nTimeIdx, MIN_DENOM, MAX_DENOM); 
                            fHourSlope /= GetAutoGap(ea[i].nMarketGubun, ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs);

                            arrFSlope[j] = fSlope;
                            arrRecentFSlope[j] = fRecentSlope;
                            arrHourFSlope[j] = fHourSlope;
                        }

                        // 초기기울기
                        ea[i].timeLines1m.fInitSlope = (double)(ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs - ea[i].nTodayStartPrice) / COMMON_DENOM;
                        ea[i].timeLines1m.fInitSlope /= GetAutoGap(ea[i].nMarketGubun, ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs);
                        // 최대값 기울기
                        ea[i].timeLines1m.fMaxSlope = (double)(ea[i].timeLines1m.nMaxUpFs - ea[i].nTodayStartPrice) / COMMON_DENOM;
                        ea[i].timeLines1m.fMaxSlope /= GetAutoGap(ea[i].nMarketGubun, ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs);

                        // arrFSlope 모두 채움
                        Array.Sort(arrFSlope, (x, y) => y.CompareTo(x)); // 아마 내림차순
                        ea[i].timeLines1m.fTotalMedian = (arrFSlope[PICKUP_CNT / 2 - 1] + arrFSlope[PICKUP_CNT / 2]) / 2; // 중위수 구함
                        if (ea[i].timeLines1m.fTotalMedian == 0)
                            ea[i].timeLines1m.fTotalMedian = arrFSlope.Sum() / PICKUP_CNT;

                        // arrRecentFSlope 모두 채움
                        Array.Sort(arrRecentFSlope, (x, y) => y.CompareTo(x)); // 아마 내림차순
                        ea[i].timeLines1m.fRecentMedian = (arrRecentFSlope[PICKUP_CNT / 2 - 1] + arrRecentFSlope[PICKUP_CNT / 2]) / 2; // 중위수 구함
                        if (ea[i].timeLines1m.fRecentMedian == 0)
                            ea[i].timeLines1m.fRecentMedian = arrRecentFSlope.Sum() / PICKUP_CNT;

                        // arrHourFSlope 모두 채움
                        Array.Sort(arrHourFSlope, (x, y) => y.CompareTo(x)); // 아마 내림차순
                        ea[i].timeLines1m.fHourMedian = (arrHourFSlope[PICKUP_CNT / 2 - 1] + arrHourFSlope[PICKUP_CNT / 2]) / 2; // 중위수 구함
                        if (ea[i].timeLines1m.fHourMedian == 0)
                            ea[i].timeLines1m.fHourMedian = arrHourFSlope.Sum() / PICKUP_CNT;

                        ea[i].timeLines1m.fInitAngle = GetAngleBetween(0, ea[i].timeLines1m.fInitSlope);
                        ea[i].timeLines1m.fMaxAngle = GetAngleBetween(0, ea[i].timeLines1m.fMaxSlope);
                        ea[i].timeLines1m.fTotalMedianAngle = GetAngleBetween(0, ea[i].timeLines1m.fTotalMedian);
                        ea[i].timeLines1m.fRecentMedianAngle = GetAngleBetween(0, ea[i].timeLines1m.fRecentMedian);
                        ea[i].timeLines1m.fHourMedianAngle = GetAngleBetween(0, ea[i].timeLines1m.fHourMedian);
                    } // END---- 추세


                    // ===========================================================================================================
                    // 이평선 Part
                    // ===========================================================================================================
                    {
                        int nShareIdx, nSummation;
                        double fMaVal;


                        // -----------
                        // 20분 이평선
                        nShareIdx = MA20M - nTimeLineIdx - 1;
                        nSummation = 0;
                        if (nShareIdx <= 0) // 0보다 작다면 더미데이터가 아니라 전부 리얼데이터로 채울 수 있다는 의미
                        {
                            for (j = nTimeLineIdx; j > nTimeLineIdx - MA20M; j--)
                            {
                                nSummation += ea[i].timeLines1m.arrTimeLine[j].nLastFs;
                            }
                        }
                        else // 부족하다는 의미
                        {
                            for (j = 0; j <= nTimeLineIdx; j++)
                            {
                                nSummation += ea[i].timeLines1m.arrTimeLine[j].nLastFs;
                            }
                            for (j = 0; j < nShareIdx; j++)
                            {
                                nSummation += ea[i].nTodayStartPrice; // 0~ BRUSH -1 까지는 다 같지만 그냥 0번째 데이터를 넣어줌
                            }
                        }
                        fMaVal = (double)nSummation / MA20M; // 현재의 N이동평균선 값
                        ea[i].maOverN.fCurMa20m = fMaVal;

                        if (ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nDownFs > fMaVal) // 현재의 저가보다 이평선이 아래에 있다면
                        {
                            ea[i].maOverN.nDownCntMa20m++; // 아래가 좋은거임
                            if (ea[i].maOverN.nUpCntMa20m > MA_EXCEED_CNT)// 오래동안 위였다가 가격이 ma를 뚫고 올라간다는 말
                            {
                                // TODO.
                            }
                            ea[i].maOverN.nUpCntMa20m = 0;
                        }
                        else
                        {
                            ea[i].maOverN.nUpCntMa20m++;
                            if (ea[i].maOverN.nDownCntMa20m > MA_EXCEED_CNT) // 오랫동안 아래였다가 가격이 ma를 뚫고 내려갔다는 말
                            {
                                // TODO.
                            }
                            ea[i].maOverN.nDownCntMa20m = 0;
                        }

                        if (ea[i].maOverN.fMaxMa20m == 0 || ea[i].maOverN.fMaxMa20m < fMaVal)
                        {
                            ea[i].maOverN.fMaxMa20m = fMaVal;
                            ea[i].maOverN.nMaxMa20mTime = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nTime;
                        }

                        // -----------
                        // 60분 이평선
                        nShareIdx = MA1H - nTimeLineIdx - 1;
                        nSummation = 0;
                        if (nShareIdx <= 0) // 0보다 작다면 더미데이터가 아니라 전부 리얼데이터로 채울 수 있다는 의미
                        {
                            for (j = nTimeLineIdx; j > nTimeLineIdx - MA1H; j--)
                            {
                                nSummation += ea[i].timeLines1m.arrTimeLine[j].nLastFs;
                            }
                        }
                        else // 부족하다는 의미
                        {
                            for (j = 0; j <= nTimeLineIdx; j++)
                            {
                                nSummation += ea[i].timeLines1m.arrTimeLine[j].nLastFs;
                            }
                            for (j = 0; j < nShareIdx; j++)
                            {
                                nSummation += ea[i].nTodayStartPrice; // 0~ BRUSH -1 까지는 다 같지만 그냥 0번째 데이터를 넣어줌
                            }
                        }
                        fMaVal = (double)nSummation / MA1H; // 현재의 N이동평균선 값
                        ea[i].maOverN.fCurMa1h = fMaVal;

                        if (ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nDownFs > fMaVal) // 현재의 저가보다 이평선이 아래에 있다면
                        {
                            ea[i].maOverN.nDownCntMa1h++; // 아래가 좋은거임
                            if (ea[i].maOverN.nUpCntMa1h > MA_EXCEED_CNT)// 오래동안 위였다가 가격이 ma를 뚫고 올라간다는 말
                            {
                                // TODO.
                            }
                            ea[i].maOverN.nUpCntMa1h = 0;
                        }
                        else
                        {
                            ea[i].maOverN.nUpCntMa1h++;
                            if (ea[i].maOverN.nDownCntMa1h > MA_EXCEED_CNT) // 오랫동안 아래였다가 가격이 ma를 뚫고 내려갔다는 말
                            {
                                // TODO.
                            }
                            ea[i].maOverN.nDownCntMa1h = 0;
                        }

                        if (ea[i].maOverN.fMaxMa1h == 0 || ea[i].maOverN.fMaxMa1h < fMaVal)
                        {
                            ea[i].maOverN.fMaxMa1h = fMaVal;
                            ea[i].maOverN.nMaxMa1hTime = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nTime;
                        }

                        // -----------
                        // 120분 이평선
                        nShareIdx = MA2H - nTimeLineIdx - 1;
                        nSummation = 0;
                        if (nShareIdx <= 0) // 0보다 작다면 더미데이터가 아니라 전부 리얼데이터로 채울 수 있다는 의미
                        {
                            for (j = nTimeLineIdx; j > nTimeLineIdx - MA2H; j--)
                            {
                                nSummation += ea[i].timeLines1m.arrTimeLine[j].nLastFs;
                            }
                        }
                        else // 부족하다는 의미
                        {
                            for (j = 0; j <= nTimeLineIdx; j++)
                            {
                                nSummation += ea[i].timeLines1m.arrTimeLine[j].nLastFs;
                            }
                            for (j = 0; j < nShareIdx; j++)
                            {
                                nSummation += ea[i].nTodayStartPrice; // 0~ BRUSH -1 까지는 다 같지만 그냥 0번째 데이터를 넣어줌
                            }
                        }
                        fMaVal = (double)nSummation / MA2H; // 현재의 N이동평균선 값
                        ea[i].maOverN.fCurMa2h = fMaVal;

                        if (ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nDownFs > fMaVal) // 현재의 저가보다 이평선이 아래에 있다면
                        {
                            ea[i].maOverN.nDownCntMa2h++; // 아래가 좋은거임
                            if (ea[i].maOverN.nUpCntMa2h > MA_EXCEED_CNT)// 오래동안 위였다가 가격이 ma를 뚫고 올라간다는 말
                            {
                                // TODO.
                            }
                            ea[i].maOverN.nUpCntMa2h = 0;
                        }
                        else
                        {
                            ea[i].maOverN.nUpCntMa2h++;
                            if (ea[i].maOverN.nDownCntMa2h > MA_EXCEED_CNT) // 오랫동안 아래였다가 가격이 ma를 뚫고 내려갔다는 말
                            {
                                // TODO.
                            }
                            ea[i].maOverN.nDownCntMa2h = 0;
                        }

                        if (ea[i].maOverN.fMaxMa2h == 0 || ea[i].maOverN.fMaxMa2h < fMaVal)
                        {
                            ea[i].maOverN.fMaxMa2h = fMaVal;
                            ea[i].maOverN.nMaxMa2hTime = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nTime;
                        }
                    }// END ---- 이평선


                    // ===========================================================================================================
                    // 전고점 Part
                    // ===========================================================================================================
                    {
                        if (ea[i].crushManager.nCrushMaxPrice < ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs) // 최근기록된 maxPrice보다 현재 종가가 더 높다면
                        {
                            // ----------------------------------------
                            // 전고점 조건 
                            if ((double)(ea[i].crushManager.nCrushMaxPrice - ea[i].crushManager.nCrushMinPrice) / ea[i].nYesterdayEndPrice > 0.01 && // 우선 종가대비 전고점과 전저점의 폭이 1퍼센트가 넘어야하고
                                ea[i].crushManager.nCrushMinTime > ea[i].crushManager.nCrushMaxTime) // minTime은 maxTime보다 이후여야한다. minTime == maxTime일 가능성이 있기 때문에.
                            {
                                Crush tmpCrush;
                                tmpCrush.nCnt = ea[i].crushManager.nCurCnt++;
                                tmpCrush.fMaxMinPower = (double)(ea[i].crushManager.nCrushMaxPrice - ea[i].crushManager.nCrushMinPrice) / ea[i].nYesterdayEndPrice;
                                tmpCrush.fCurMinPower = (double)(ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs - ea[i].crushManager.nCrushMinPrice) / ea[i].nYesterdayEndPrice;
                                tmpCrush.nMaxMinTime = SubTimeToTimeAndSec(ea[i].crushManager.nCrushMinTime, ea[i].crushManager.nCrushMaxTime);
                                tmpCrush.nMaxCurTime = SubTimeToTimeAndSec(ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nTime, ea[i].crushManager.nCrushMaxTime);
                                tmpCrush.nMinCurTime = SubTimeToTimeAndSec(ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nTime, ea[i].crushManager.nCrushMinTime);
                                tmpCrush.nMinPrice = ea[i].crushManager.nCrushMinPrice;
                                tmpCrush.nMaxPrice = ea[i].crushManager.nCrushMaxPrice;
                                if (tmpCrush.nCnt == 0)
                                {
                                    tmpCrush.fUpperNow = (double)(ea[i].crushManager.nCrushMinPrice - ea[i].crushManager.nCrushOnlyMinPrice) / ea[i].nYesterdayEndPrice;
                                }
                                else
                                {
                                    tmpCrush.fUpperNow = (double)(ea[i].crushManager.nCrushMinPrice - ea[i].crushManager.crushList[tmpCrush.nCnt - 1].nMinPrice) / ea[i].nYesterdayEndPrice;
                                }

                                ea[i].crushManager.crushList.Add(tmpCrush);
                            }

                            ea[i].crushManager.nCrushMaxPrice = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs;
                            ea[i].crushManager.nCrushMaxTime = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nTime;
                            // 전고점에서 minTime은 항상 maxTime보다 높아야하니까 max가 앞서갈때는 minTime을 같이 세팅해준다.
                            ea[i].crushManager.nCrushMinPrice = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs;
                            ea[i].crushManager.nCrushMinTime = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nTime;
                        }
                        if (ea[i].crushManager.nCrushMinPrice > ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs)    // 최근 기록된 minPrice보다 현재종가가 낮다면
                        {
                            ea[i].crushManager.nCrushMinPrice = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs;
                            ea[i].crushManager.nCrushMinTime = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nTime;
                        }
                        if (ea[i].crushManager.nCrushOnlyMinPrice > ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs) // 최근 기록된 only minPrice보다 현재종가가 낮다면
                        {
                            ea[i].crushManager.nCrushOnlyMinPrice = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nLastFs;
                            ea[i].crushManager.nCrushOnlyMinTime = ea[i].timeLines1m.arrTimeLine[nTimeLineIdx].nTime;
                        }
                    }

                    if (ea[i].crushManager.crushList.Count != ea[i].crushManager.nPrevCrushCnt) // 전고점카운트가 오를때마다
                    {
                        int nCrushUpperCnt = 0;
                        int nBadPoint = 0;
                        ea[i].crushManager.nUpCnt = 0;
                        ea[i].crushManager.nDownCnt = 0;
                        ea[i].crushManager.nSpecialDownCnt = 0;

                        for (j = 0; j < ea[i].crushManager.crushList.Count; j++)
                        {
                            if (ea[i].crushManager.crushList[j].fUpperNow > 0) // 올랐다
                            {
                                nCrushUpperCnt++;
                                ea[i].crushManager.nUpCnt++;
                            }
                            else
                            {
                                if (j >= ea[i].crushManager.crushList.Count - EYES_CLOSE_CRUSH_NUM)
                                {
                                    if (j == ea[i].crushManager.crushList.Count - 1) // 바로 이전에 전고점에서 하락이었으면 badcount 하나더 플러스
                                    {
                                        nBadPoint++;
                                        ea[i].crushManager.nSpecialDownCnt++;
                                    }
                                    ea[i].crushManager.nDownCnt++;
                                    nBadPoint++;
                                }
                            }
                        }
                    } // END --- 전고점 
                }// END ---- 개인구조체 업데이트

                // 기록
                int nR = 3;
                StreamWriter tmpSw = new StreamWriter(new FileStream(sMessageLogPath + DateTime.Now.ToString("yyyy/MM/dd-") + nSharedTime.ToString() + ".txt", FileMode.Create));
                for (i = 0; i < nStockLength; i++) // 업데이트
                {
                    nRankIdx = stockDashBoard.stockPanel[i].nEaIdx;

                    tmpSw.WriteLine(nSharedTime + "\t" + ea[nRankIdx].sCode + "\t" + ea[nRankIdx].sCodeName + "\t" + ea[nRankIdx].sMarketGubunTag + "\t" +
                        "##각도##" + "\t" +
                        Math.Round(ea[nRankIdx].timeLines1m.fTotalMedianAngle, nR) + "\t" + Math.Round(ea[nRankIdx].timeLines1m.fHourMedianAngle, nR) + "\t" + Math.Round(ea[nRankIdx].timeLines1m.fRecentMedianAngle, nR) + "\t" + Math.Round(ea[nRankIdx].timeLines1m.fInitAngle, nR) + "\t" + Math.Round(ea[nRankIdx].timeLines1m.fMaxAngle, nR) + "\t" +
                        "##전체순위##" + "\t" + ea[nRankIdx].rankSystem.nSummationRanking + "\t" +
                        "##분당순위##" + "\t" + ea[nRankIdx].rankSystem.nMinuteSummationRanking + "\t" +
                        "##순위변동##" + "\t" + ea[nRankIdx].rankSystem.nSummationMove + "\t" + 
                        "##상승중##" + "\t" +
                        ea[nRankIdx].maOverN.nDownCntMa20m + "\t" + ea[nRankIdx].maOverN.nDownCntMa1h + "\t" + ea[nRankIdx].maOverN.nDownCntMa2h + "\t" +
                        "## 20/60 ##" + "\t" + (ea[nRankIdx].maOverN.fCurMa20m > ea[nRankIdx].maOverN.fCurMa1h).ToString() + "\t" +
                        "## 60/120 ##" + "\t" + (ea[nRankIdx].maOverN.fCurMa1h > ea[nRankIdx].maOverN.fCurMa2h).ToString() + "\t" +
                        "##순위 유지기간##" + "\t" +
                        ea[nRankIdx].rankSystem.nRankHold10 + "\t" + ea[nRankIdx].rankSystem.nRankHold20 + "\t" + ea[nRankIdx].rankSystem.nRankHold50 + "\t" + ea[nRankIdx].rankSystem.nRankHold100 + "\t" + ea[nRankIdx].rankSystem.nRankHold200 + "\t" +
                        "##하락중##" + "\t" +
                        ea[nRankIdx].maOverN.nUpCntMa20m + "\t" + ea[nRankIdx].maOverN.nUpCntMa1h + "\t" + ea[nRankIdx].maOverN.nUpCntMa2h + "\t" +
                        "##나머지 순위##" + "\t" +
                        ea[nRankIdx].rankSystem.nTotalTradePriceRanking + "\t" + ea[nRankIdx].rankSystem.nTotalTradeVolumeRanking + "\t" +
                        ea[nRankIdx].rankSystem.nTotalBuyPriceRanking + "\t" + ea[nRankIdx].rankSystem.nTotalBuyVolumeRanking + "\t" +
                        ea[nRankIdx].rankSystem.nAccumCountRanking + "\t" + ea[nRankIdx].rankSystem.nPowerRanking + "\t" +
                        ea[nRankIdx].rankSystem.nMarketCapRanking 
                        );

                    DashSw.WriteLine(nSharedTime + "\t" + ea[nRankIdx].sCode + "\t" + ea[nRankIdx].sCodeName + "\t" + ea[nRankIdx].sMarketGubunTag + "\t" +
                        "##각도##" + "\t" +
                        Math.Round(ea[nRankIdx].timeLines1m.fTotalMedianAngle, nR) + "\t" + Math.Round(ea[nRankIdx].timeLines1m.fHourMedianAngle, nR) + "\t" + Math.Round(ea[nRankIdx].timeLines1m.fRecentMedianAngle, nR) + "\t" + Math.Round(ea[nRankIdx].timeLines1m.fInitAngle, nR) + "\t" + Math.Round(ea[nRankIdx].timeLines1m.fMaxAngle, nR) + "\t" +
                        "##전체순위##" + "\t" + ea[nRankIdx].rankSystem.nSummationRanking + "\t" +
                        "##분당순위##" + "\t" + ea[nRankIdx].rankSystem.nMinuteSummationRanking + "\t" +
                        "##순위변동##" + "\t" + ea[nRankIdx].rankSystem.nSummationMove + "\t" +
                        "##상승중##" + "\t" +
                        ea[nRankIdx].maOverN.nDownCntMa20m + "\t" + ea[nRankIdx].maOverN.nDownCntMa1h + "\t" + ea[nRankIdx].maOverN.nDownCntMa2h + "\t" +
                        "## 20/60 ##" + "\t" + (ea[nRankIdx].maOverN.fCurMa20m > ea[nRankIdx].maOverN.fCurMa1h).ToString() + "\t" +
                        "## 60/120 ##" + "\t" + (ea[nRankIdx].maOverN.fCurMa1h > ea[nRankIdx].maOverN.fCurMa2h).ToString() + "\t" +
                        "##순위 유지기간##" + "\t" +
                        ea[nRankIdx].rankSystem.nRankHold10 + "\t" + ea[nRankIdx].rankSystem.nRankHold20 + "\t" + ea[nRankIdx].rankSystem.nRankHold50 + "\t" + ea[nRankIdx].rankSystem.nRankHold100 + "\t" + ea[nRankIdx].rankSystem.nRankHold200 + "\t" +
                        "##하락중##" + "\t" +
                        ea[nRankIdx].maOverN.nUpCntMa20m + "\t" + ea[nRankIdx].maOverN.nUpCntMa1h + "\t" + ea[nRankIdx].maOverN.nUpCntMa2h + "\t" +
                        "##나머지 순위##" + "\t" +
                        ea[nRankIdx].rankSystem.nTotalTradePriceRanking + "\t" + ea[nRankIdx].rankSystem.nTotalTradeVolumeRanking + "\t" +
                        ea[nRankIdx].rankSystem.nTotalBuyPriceRanking + "\t" + ea[nRankIdx].rankSystem.nTotalBuyVolumeRanking + "\t" +
                        ea[nRankIdx].rankSystem.nAccumCountRanking + "\t" + ea[nRankIdx].rankSystem.nPowerRanking + "\t" +
                        ea[nRankIdx].rankSystem.nMarketCapRanking
                        );
                }

                // 시간 업데이트
                nPrevBoardUpdateTime = AddTimeBySec(nPrevBoardUpdateTime, MINUTE_SEC);
                nTimeLineIdx++;
                tmpSw.Close();

            }

            // =============================================================
            // 매매 컨트롤러
            // =============================================================
            if (tradeQueue.Count > 0) // START ---- 매매 컨트롤러
            {

                if (isForbidTrade) // 거래정지상태 (주문이 단위시간 한계수량에 도달해서)
                {
                    dtCurOrderTime = DateTime.Now;
                    if ((dtCurOrderTime - dtBeforeOrderTime).Seconds >= 1) // 1초가 지나면 거래 풀어줌.
                    {
                        isForbidTrade = false;
                        dtBeforeOrderTime = dtCurOrderTime;
                        nReqestCount = 0;
                        testTextBox.AppendText("거래정지상태 풀림\r\n");
                    }
                }

                if (!isForbidTrade)  // 거래정지가 아니라면
                {
                    curSlot = tradeQueue.Dequeue(); // 우선 디큐한다
                    ea[curSlot.nEaIdx].myTradeManager.nCurBuyedSlotIdx = curSlot.nBuyedSlotIdx; // 매수의 경우 (가용매매블록 갯수 한개 : 0 , 한개이상 :  buyedSlotCntArray 현재인덱스)
                                                                                                // 매도의 경우 (가용매매블록 갯수 한개 : 0 , 한개이상 :  buyedSlotCntArray 매도해당 인덱스)

                    if (SubTimeToTimeAndSec(nSharedTime, curSlot.nRqTime) <= IGNORE_REQ_SEC) // 현재시간 - 요청시간 < 10초 : 요청시간이 너무 길어진 요청의 처리를 위한 분기문  
                    {

                        if (curSlot.nOrderType == NEW_BUY || curSlot.nOrderType == NEW_SELL) // 신규매수매도 신규매수:1 신규매도:2
                        {
                            // 아직 매수중이거나 매도중일때는
                            if ((ea[curSlot.nEaIdx].myTradeManager.nBuyReqCnt > 0) || (ea[curSlot.nEaIdx].myTradeManager.nSellReqCnt > 0)) // 현재 거래중이면
                            {
                                tradeQueue.Enqueue(curSlot); // 디큐했던 슬롯을 다시 인큐한다.
                            }
                            else // 거래중이 아닐때 (단, 매수취소는 예외)
                            {
                                if (curSlot.nOrderType == NEW_BUY) // 신규매수
                                {
                                    int nEstimatedPrice = curSlot.nOrderPrice; // 종목의 요청했던 최우선매도호가를 받아온다.
                                                                               // 반복해서 가격을 n칸 올린다.
                                    if (ea[curSlot.nEaIdx].nMarketGubun == KOSDAQ_ID) // 코스닥일 경우
                                    {
                                        for (int eyeCloseIdx = 0; eyeCloseIdx < EYES_CLOSE_NUM; eyeCloseIdx++)
                                            nEstimatedPrice += GetKosdaqGap(nEstimatedPrice);
                                    }
                                    else if (ea[curSlot.nEaIdx].nMarketGubun == KOSPI_ID) // 코스피의 경우
                                    {
                                        for (int eyeCloseIdx = 0; eyeCloseIdx < EYES_CLOSE_NUM; eyeCloseIdx++)
                                            nEstimatedPrice += GetKospiGap(nEstimatedPrice);
                                    }

                                    double fCurLimitPriceFee = nEstimatedPrice; // 현재 매매수수료 미포함

                                    int nNumToBuy = (int)(nCurDepositCalc / fCurLimitPriceFee); // 현재 예수금으로 살 수 있을 만큼
                                    int nMaxNumToBuy = (int)(nMaxPriceForEachStock * curSlot.fRequestRatio / fCurLimitPriceFee); // 최대매수가능금액으로 살 수 있을 만큼 

                                    ////////////////////////// 테스트용 ///////////////////////////////////

                                    if (nNumToBuy > nMaxNumToBuy) // 최대매수가능수를 넘는다면
                                        nNumToBuy = nMaxNumToBuy; // 최대매수가능수로 세팅

                                    // 구매수량이 있고 현재종목의 최우선매도호가가 요청하려는 지정가보다 낮을 경우 구매요청을 걸 수 있다.
                                    if ((nNumToBuy > 0) && (ea[curSlot.nEaIdx].nFs < nEstimatedPrice))
                                    {
                                        if (curSlot.sHogaGb.Equals(MARKET_ORDER)) // 시장가모드 : 시장가로 하면 키움에서 상한가값으로 계산해서 예수금만큼 살 수 가 없다
                                        {
                                            if (ea[curSlot.nEaIdx].myTradeManager.nIdx < BUY_SLOT_MAX_NUM) // 개인 구매횟수를 넘기지 않았다면
                                            {

                                                ea[curSlot.nEaIdx].myTradeManager.nCurRqPrice = curSlot.nOrderPrice; // 주문요청금액 설정
                                                ea[curSlot.nEaIdx].myTradeManager.nCurLimitPrice = nEstimatedPrice; // 지정상한가 설정
                                                ea[curSlot.nEaIdx].myTradeManager.fTargetPercent = curSlot.fTargetPercent; // 익절퍼센트 설정, 단계식매매일경우 사용 안함
                                                ea[curSlot.nEaIdx].myTradeManager.fBottomPercent = curSlot.fBottomPercent; // 손절퍼센트 설정, 단계식매매일경우 사용 안함
                                                ea[curSlot.nEaIdx].myTradeManager.isStepByStepTrade = curSlot.isStepByStepTrade; // 매매방법 설정
                                                ea[curSlot.nEaIdx].myTradeManager.nCurRqTime = nSharedTime; // 현재시간설정
                                                ea[curSlot.nEaIdx].myTradeManager.nStrategyIdx = curSlot.nStrategyIdx; // 전략인덱스
                                                ea[curSlot.nEaIdx].myTradeManager.nSequence = curSlot.nSequence; // 순번인덱스
                                                ea[curSlot.nEaIdx].myTradeManager.nOrderVolume = nNumToBuy;
                                                ea[curSlot.nEaIdx].myTradeManager.fTradeRatio = curSlot.fRequestRatio;

                                                testTextBox.AppendText(nSharedTime.ToString() + " : " + curSlot.sCode + " 매수신청 전송 \r\n"); //++
                                                int nBuyReqResult = axKHOpenAPI1.SendOrder(curSlot.sRQName, SetTradeScreenNo(), sAccountNum,
                                                    curSlot.nOrderType, curSlot.sCode, nNumToBuy, nEstimatedPrice,
                                                    "00", curSlot.sOrgOrderId); // 높은 매도호가에 지정가로 걸어 시장가처럼 사게 한다
                                                                                // 최우선매도호가보다 높은 가격에 지정가를 걸면 현재매도호가에 구매하게 된다.

                                                if (nBuyReqResult == 0) // 요청이 성공하면
                                                {
                                                    isSendOrder = true;
                                                    ea[curSlot.nEaIdx].myTradeManager.nBuyReqCnt++; // 구매횟수 증가
                                                    testTextBox.AppendText(nSharedTime.ToString() + ", " + curSlot.sCode + ", " + curSlot.nOrderPrice.ToString() + ", " + nNumToBuy.ToString() + " 매수신청 전송 성공 \r\n"); //++

                                                }
                                                else // 요청 실패
                                                {
                                                    // isSendOrder = false;
                                                    ea[curSlot.nEaIdx].arrMyStrategy[curSlot.nStrategyIdx]--;
                                                    testTextBox.AppendText(nSharedTime.ToString() + ", " + curSlot.sCode + ", " + curSlot.nOrderPrice.ToString() + ", " + nNumToBuy.ToString() + " 매수신청 전송 실패!! \r\n"); //++
                                                }
                                            }
                                            else  // 개인 구매횟수를 넘겼다면
                                                testTextBox.AppendText(curSlot.sCode + " 종목의 구매횟수를 초과했습니다.\r\n"); //++
                                        }
                                        else if (curSlot.sHogaGb.Equals(PENDING_ORDER))
                                        {

                                        }
                                    }
                                    else // 보유금액이 없거나 가격이 너무 올라버린 경우
                                    {
                                        ea[curSlot.nEaIdx].arrMyStrategy[curSlot.nStrategyIdx]--;
                                    }
                                } // END ---- 신규매수
                                else if (curSlot.nOrderType == NEW_SELL) // 신규매도
                                {
                                    if (curSlot.sHogaGb.Equals(MARKET_ORDER)) // 시장가매도
                                    {

                                        testTextBox.AppendText(nSharedTime.ToString() + " : " + curSlot.sCode + " 매도신청 전송 \r\n"); //++
                                        int nSellReqResult = axKHOpenAPI1.SendOrder(curSlot.sRQName, SetTradeScreenNo(), sAccountNum,
                                                curSlot.nOrderType, curSlot.sCode, curSlot.nQty, 0,
                                                curSlot.sHogaGb, curSlot.sOrgOrderId);

                                        if (nSellReqResult == 0) // 요청이 성공하면
                                        {
                                            isSendOrder = true;
                                            testTextBox.AppendText(curSlot.sCode + " 매도신청 전송 성공 \r\n"); //++
                                            ea[curSlot.nEaIdx].myTradeManager.nSellReqCnt++; // 매도요청전송이 성공하면 매도횟수를 증가한다.
                                        }
                                        else
                                        {
                                            testTextBox.AppendText(curSlot.sCode + " 매도신청 전송 실패 \r\n"); //++
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[curSlot.nBuyedSlotIdx].isSelling = false;
                                        }
                                    }
                                    else if (curSlot.sHogaGb.Equals(PENDING_ORDER)) // 지정가매도
                                    {

                                    }
                                } // END ---- 신규매도
                            }
                        } // End ---- 신규매수매도
                        else if (curSlot.nOrderType == BUY_CANCEL) // 매수취소 매수취소는 매수중일때만 요청되고 매수와 함께 슬롯입장이 가능하다. 매도중일때는 안된다.
                        {
                            // 구매중일때만 매수취소가 가능하니 buySlotArray의 인덱스는 매수취소종목의 마지막인덱스로 확정되니
                            // 건들지 않는다
                            if (!ea[curSlot.nEaIdx].myTradeManager.arrBuyedSlots[curSlot.nBuyedSlotIdx].isAllBuyed) // 그와중에 매수가 완료되면 매수취소는 삭제된다.
                            {
                                testTextBox.AppendText(nSharedTime.ToString() + " : " + curSlot.sCode + " 매수취소신청 전송 \r\n"); //++
                                int nCancelReqResult = axKHOpenAPI1.SendOrder(curSlot.sRQName, SetTradeScreenNo(), sAccountNum,
                                    curSlot.nOrderType, curSlot.sCode, 0, 0,
                                    "", curSlot.sOrgOrderId);

                                if (nCancelReqResult != 0) // 매수취소가 성공하지 않으면
                                {
                                    ea[curSlot.nEaIdx].myTradeManager.arrBuyedSlots[curSlot.nBuyedSlotIdx].isCanceling = false; // 이래야지 매수취소를 다시 신청할 수 있다.
                                }
                                else
                                {
                                    isSendOrder = true;
                                    testTextBox.AppendText(curSlot.sCode + " 매수취소신청 전송 성공 \r\n"); //++
                                }
                            }
                        } // End ---- 매수취소
                        else if (curSlot.nOrderType == SELL_CANCEL) // 매도취소
                        {
                        }
                        else if (curSlot.nOrderType == BUY_UPDATE) // 매수정정
                        {
                        }
                        else if (curSlot.nOrderType == SELL_UPDATE) // 매도정정
                        {
                        }


                        if (isSendOrder) // 주문을 요청했을때만
                        {
                            isSendOrder = false;
                            dtCurOrderTime = DateTime.Now;
                            if ((dtCurOrderTime - dtBeforeOrderTime).Seconds >= 1) // 주문시간이 이전주문시간과 다르다 == 1초 제한이 아니다
                            {
                                dtBeforeOrderTime = dtCurOrderTime;
                                nReqestCount = 1;
                            }
                            else  // 주문시간이 이전시간과 같다 == 1초 제한 카운트 증가
                            {
                                nReqestCount++;
                                if (nReqestCount >= (LIMIT_SENDORDER_NUM - 1)) // 5번제한이지만 혹시 모르니 4번제한으로
                                {
                                    isForbidTrade = true; // 제한에 걸리면 1초가 지날때까지는 매매 금지
                                }
                            }
                        } // END ---- 주문을 요청했을때만


                    } // End ---- 현재시간 - 요청시간 < 10초
                    else // 요청시간 초과로 인한 탈락
                    {
                        if (curSlot.nOrderType == NEW_SELL)
                        {
                            ea[curSlot.nEaIdx].myTradeManager.arrBuyedSlots[curSlot.nBuyedSlotIdx].isSelling = false;
                        }
                        else if (curSlot.nOrderType == NEW_BUY)
                        {
                            ea[curSlot.nEaIdx].arrMyStrategy[curSlot.nStrategyIdx]--;
                        }
                    } // End ---- 요청시간 초과로 인한 탈락
                } // END ---- 거래정지가 아니라면
            } // END ---- 매매컨트롤러


            // =============================================================
            // 실시간 호가잔량
            // =============================================================
            if (isHogaJanRyang) // ##호가잔량##
            {
                int nCodeIdx = int.Parse(sCode);
                nCurIdx = eachStockIdxArray[nCodeIdx];

                if (isMarketStart && !ea[nCurIdx].isExcluded)
                {

                    int b4;
                    int s1 = 0, s2 = 0, s3 = 0, s4;
                    int nTotalBuyHoga = 0;
                    int nTotalSellHoga = 0;
                    bool isHogaError = false;
                    bool isSubHogaError = false;

                    ea[nCurIdx].nHogaCnt++; // 호가의 카운트
                    ea[nCurIdx].hogaSpeedStatus.fPush++;

                    try
                    {
                        s1 = Math.Abs(int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 61))); // 매도1호가잔량
                        s2 = Math.Abs(int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 62))); // 매도2호가잔량
                        s3 = Math.Abs(int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 63))); // 매도3호가잔량

                        s4 = Math.Abs(int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 64))); // 매도4호가잔량
                        b4 = Math.Abs(int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 74))); // 매수4호가잔량

                        if (s4 == 0 && b4 == 0)
                        {
                            if (!ea[nCurIdx].isViMode)
                                ea[nCurIdx].nViStartTime = nSharedTime;
                            ea[nCurIdx].isViMode = true;
                        }
                        else
                        {
                            if (ea[nCurIdx].isViMode)
                            {
                                ea[nCurIdx].isViMode = false;
                                ea[nCurIdx].nViEndTime = nSharedTime;
                                ea[nCurIdx].isViGauge = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        isSubHogaError = true;
                    }


                    if (!ea[nCurIdx].isViMode)
                    {
                        try
                        {
                            nTotalBuyHoga = Math.Abs(int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 125)));  // 매수호가총잔량
                        }
                        catch (Exception ex)
                        {
                            isHogaError = true;
                        }

                        try
                        {
                            nTotalSellHoga = Math.Abs(int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 121)));  // 매도호가총잔량
                        }
                        catch (Exception ex)
                        {
                            isHogaError = true;
                        }


                        if (!isHogaError)
                        {
                            if (!isSubHogaError)
                            {
                                ea[nCurIdx].nThreeSellHogaVolume = s1 + s2 + s3; //  매도1~3 호가잔량합
                            }
                            ea[nCurIdx].nTotalBuyHogaVolume = nTotalBuyHoga;
                            ea[nCurIdx].nTotalSellHogaVolume = nTotalSellHoga;
                            ea[nCurIdx].nTotalHogaVolume = ea[nCurIdx].nTotalBuyHogaVolume + ea[nCurIdx].nTotalSellHogaVolume;
                            ea[nCurIdx].fHogaRatio = (double)(ea[nCurIdx].nTotalSellHogaVolume - ea[nCurIdx].nTotalBuyHogaVolume) / ea[nCurIdx].nTotalHogaVolume; // -1 ~ +1 ( 매도가 많으면 ) 

                            if (ea[nCurIdx].nPrevHogaUpdateTime == 0)
                            {
                                ea[nCurIdx].nPrevHogaUpdateTime = nFirstTime;
                            }
                            int nSubTimeShort = SubTimeToTimeAndSec(nSharedTime, ea[nCurIdx].nPrevHogaUpdateTime);
                            int nHogaUpdatesShort = nSubTimeShort / SHORT_UPDATE_TIME;

                            if (ea[nCurIdx].totalHogaVolumeStatus.fPush == 0)
                                ea[nCurIdx].totalHogaVolumeStatus.fPush = ea[nCurIdx].nTotalHogaVolume;
                            else
                                ea[nCurIdx].totalHogaVolumeStatus.fPush = ea[nCurIdx].totalHogaVolumeStatus.fPush * (1 - fPushWeight) + ea[nCurIdx].nTotalHogaVolume * fPushWeight;

                            if (ea[nCurIdx].hogaRatioStatus.fPush == 0)
                                ea[nCurIdx].hogaRatioStatus.fPush = ea[nCurIdx].fHogaRatio;
                            else
                                ea[nCurIdx].hogaRatioStatus.fPush = ea[nCurIdx].hogaRatioStatus.fPush * (1 - fPushWeight) + ea[nCurIdx].fHogaRatio * fPushWeight;

                            for (int idxUpdate = 0; idxUpdate < nHogaUpdatesShort; idxUpdate++)
                            {
                                if (ea[nCurIdx].totalHogaVolumeStatus.fVal == 0)
                                    ea[nCurIdx].totalHogaVolumeStatus.fVal = ea[nCurIdx].totalHogaVolumeStatus.fPush;
                                else
                                    ea[nCurIdx].totalHogaVolumeStatus.fVal = ea[nCurIdx].totalHogaVolumeStatus.fPush * fPushWeight + ea[nCurIdx].totalHogaVolumeStatus.fVal * (1 - fPushWeight);
                                ea[nCurIdx].totalHogaVolumeStatus.fPush = 0;

                                if (ea[nCurIdx].hogaRatioStatus.fVal == 0)
                                    ea[nCurIdx].hogaRatioStatus.fVal = ea[nCurIdx].hogaRatioStatus.fPush;
                                else
                                    ea[nCurIdx].hogaRatioStatus.fVal = ea[nCurIdx].hogaRatioStatus.fPush * fPushWeight + ea[nCurIdx].hogaRatioStatus.fVal * (1 - fPushWeight);
                                ea[nCurIdx].hogaRatioStatus.fPush = 0;

                                if (ea[nCurIdx].hogaSpeedStatus.fVal == 0)
                                    ea[nCurIdx].hogaSpeedStatus.fVal = ea[nCurIdx].hogaSpeedStatus.fPush;
                                else
                                    ea[nCurIdx].hogaSpeedStatus.fVal = ea[nCurIdx].hogaSpeedStatus.fPush * fPushWeight + ea[nCurIdx].hogaSpeedStatus.fVal * (1 - fPushWeight);
                                ea[nCurIdx].hogaSpeedStatus.fPush = 0;

                                ea[nCurIdx].nPrevHogaUpdateTime = nSharedTime;
                            }

                            int nTimeHogaPassedShort = SubTimeToTimeAndSec(nSharedTime, ea[nCurIdx].nPrevHogaUpdateTime); // 시간이 지났다!
                            double fTimeHogaPassedWeightShort = (double)nTimeHogaPassedShort / SHORT_UPDATE_TIME; // 시간이 얼만큼 지났느냐 0 ~ ( nUpdateTime -1) /nUpdateTime

                            if (ea[nCurIdx].hogaSpeedStatus.fVal == 0)
                                ea[nCurIdx].hogaSpeedStatus.fCur = (double)ea[nCurIdx].hogaSpeedStatus.fPush / SHORT_UPDATE_TIME;
                            else
                                ea[nCurIdx].hogaSpeedStatus.fCur = (ea[nCurIdx].hogaSpeedStatus.fPush * fTimeHogaPassedWeightShort + ea[nCurIdx].hogaSpeedStatus.fVal * (1 - fTimeHogaPassedWeightShort)) / SHORT_UPDATE_TIME; // 현재호가속도

                            if (ea[nCurIdx].hogaRatioStatus.fVal == 0)
                                ea[nCurIdx].hogaRatioStatus.fCur = ea[nCurIdx].hogaRatioStatus.fPush;
                            else
                                ea[nCurIdx].hogaRatioStatus.fCur = ea[nCurIdx].hogaRatioStatus.fPush * fTimeHogaPassedWeightShort + ea[nCurIdx].hogaRatioStatus.fVal * (1 - fTimeHogaPassedWeightShort); // 현재호가대비율

                            if (ea[nCurIdx].totalHogaVolumeStatus.fVal == 0)
                                ea[nCurIdx].totalHogaVolumeStatus.fCur = ea[nCurIdx].totalHogaVolumeStatus.fPush;
                            else
                                ea[nCurIdx].totalHogaVolumeStatus.fCur = (ea[nCurIdx].totalHogaVolumeStatus.fPush * fTimeHogaPassedWeightShort + ea[nCurIdx].totalHogaVolumeStatus.fVal * (1 - fTimeHogaPassedWeightShort)); // 현재총호가잔량


                        }
                    }
                }
            }
            // =============================================================
            // 실시간 주식체결
            // =============================================================
            else if (isZooSikCheGyul) // ##주식체결##
            {
                int nCodeIdx = int.Parse(sCode);
                nCurIdx = eachStockIdxArray[nCodeIdx];
                ea[nCurIdx].nLastRecordTime = nSharedTime;


                if (!ea[nCurIdx].isExcluded) // 제외댔으면 접근 금지
                {
                    if (nSharedTime >= SHUTDOWN_TIME) // 3시가 넘었으면
                    {
                        testTextBox.AppendText("3시가 지났다\r\n");
                        isMarketStart = false; // 장 중 시그널을 off한다
                        for (int nScreenNum = REAL_SCREEN_NUM_START; nScreenNum <= REAL_SCREEN_NUM_END; nScreenNum++)
                        {
                            // 실시간 체결에 할당된 화면번호들에 대해 다 디스커넥트한다
                            // 실시간 체결만 받고있는 화면번호들만이 아니라 전부를 디스커넥트하는 이유는
                            // 전부를 디스커넥트하는 잠깐의 시간동안 잔여 실시간주식체결데이터들이 처리되는 것을 기다리는 기능도 있다.
                            axKHOpenAPI1.DisconnectRealData(nScreenNum.ToString());
                        }

                        RequestHoldings(0); // 잔고현황을 체크한다. 이때 nShutDown이 양수이기 때문에 남아있는 주식들이 있으면 전량 매도한다.
                        return;
                    }

                    int nFs, nFb, nTv;
                    double fTs, fPower;

                    try
                    {
                        nFs = Math.Abs(int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 27))); // 최우선매도호가
                        nFb = Math.Abs(int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 28))); // 최우선매수호가
                        nTv = int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 15)); // 거래량
                        fTs = double.Parse(axKHOpenAPI1.GetCommRealData(sCode, 228)); // 체결강도
                        fPower = double.Parse(axKHOpenAPI1.GetCommRealData(sCode, 12)) / 100; // 등락율

                        ea[nCurIdx].nFs = nFs;
                        ea[nCurIdx].nFb = nFb;
                        ea[nCurIdx].nTv = nTv;
                        ea[nCurIdx].fTs = fTs;
                        ea[nCurIdx].fPower = fPower;
                    }
                    catch (Exception ex)
                    {
                        return;
                    }


                    ea[nCurIdx].nCnt++; // 인덱스를 올린다.

                    if (ea[nCurIdx].nFs == 0 && ea[nCurIdx].nFb == 0)  // 둘 다 데이터가 없는경우는 가격초기화가 불가능하기 return
                        return;
                    else
                    {
                        // 둘다 제대로 받아졌거나 , 둘 중 하나가 안받아졌거나
                        if (ea[nCurIdx].nFs == 0) // fs가 안받아졌으면 fb 가격에 fb갭 한칸을 더해서 설정
                        {
                            ea[nCurIdx].nFs = ea[nCurIdx].nFb + GetAutoGap(ea[nCurIdx].nMarketGubun, ea[nCurIdx].nFb);
                        }
                        if (ea[nCurIdx].nFb == 0) // fb가 안받아졌으면 fs 가격에 (fs-1)갭 한칸을 마이너스해서 설정
                        {
                            // fs-1 인 이유는 fs가 1000원이라하면 fb는 999여야하는데 갭을 받을때 5를 받게되니 fb가 995가 되어버린다.이는 오류!
                            ea[nCurIdx].nFb = ea[nCurIdx].nFs - GetAutoGap(ea[nCurIdx].nMarketGubun, ea[nCurIdx].nFs - 1);
                        }

                    }
                    ea[nCurIdx].fDiff = (ea[nCurIdx].nFs - ea[nCurIdx].nFb) / GetAutoGap(ea[nCurIdx].nMarketGubun, ea[nCurIdx].nFs);

                    // 이상 데이터 감지
                    // fs와 fb의 가격차이가 2퍼가 넘을경우 이상데이터라 생각하고 리턴한다.
                    // 미리 리턴하는 이유는 이런 이상 데이터로는 전략에 사용하지 않기위해서 전략찾는 부분 위에서 리턴여부를 검증한다.
                    // if ((ea[nCurIdx].nFs - ea[nCurIdx].nFb) / ea[nCurIdx].nFb > 0.02)
                    //    return;


                    // 처음가격과 시간등을 맞추려는 변수이다.
                    if (!ea[nCurIdx].isFirstCheck) // 개인 초기작업
                    {

                        //if (ea[nCurIdx].nFs < 1000) // 1000원도 안한다면 폐기처분
                        //{
                        //    axKHOpenAPI1.SetRealRemove(ea[nCurIdx].sRealScreenNum, ea[nCurIdx].sCode);
                        //    ea[nCurIdx].isExcluded = true;
                        //}

                        ea[nCurIdx].isFirstCheck = true; // 가격설정이 끝났으면 이종목의 초기체크는 완료 설정
                        int nStartGap = int.Parse(axKHOpenAPI1.GetCommRealData(sCode, 11)); // 어제종가와 비교한 가격변화

                        ea[nCurIdx].nStartGap = nStartGap; // 갭

                        if (ea[nCurIdx].nYesterdayEndPrice == 0)
                        {
                            ea[nCurIdx].nYesterdayEndPrice = ea[nCurIdx].nFs - nStartGap; // 시초가에서 변화를 제거하면 어제 종가가 나옴
                            ea[nCurIdx].nTodayStartPrice = ea[nCurIdx].nFs; // 오늘 시초가
                        }
                        else
                        {
                            ea[nCurIdx].nTodayStartPrice = ea[nCurIdx].nYesterdayEndPrice + ea[nCurIdx].nStartGap;
                        }
                        ea[nCurIdx].fStartGap = (double)ea[nCurIdx].nStartGap / ea[nCurIdx].nYesterdayEndPrice; // 갭의 등락율

                        for (int i = 0; i < BRUSH + ea[nCurIdx].nJumpCnt; i++)
                        {
                            ea[nCurIdx].timeLines1m.nRealDataIdx = ea[nCurIdx].timeLines1m.nPrevTimeLineIdx; // 지금은 nRealDataIdx인것
                            ea[nCurIdx].timeLines1m.nPrevTimeLineIdx++; // 다음 페이즈로 넘어간다는 느낌

                            ea[nCurIdx].timeLines1m.arrTimeLine[i].nStartFs = ea[nCurIdx].nTodayStartPrice; // nYesterDayEndPrice로 하지 않는 이유는 갭의 영향을 안받기 위해서이다.
                            ea[nCurIdx].timeLines1m.arrTimeLine[i].nLastFs = ea[nCurIdx].nTodayStartPrice;
                            ea[nCurIdx].timeLines1m.arrTimeLine[i].nMaxFs = ea[nCurIdx].nTodayStartPrice;
                            ea[nCurIdx].timeLines1m.arrTimeLine[i].nMinFs = ea[nCurIdx].nTodayStartPrice;
                            ea[nCurIdx].timeLines1m.arrTimeLine[i].nUpFs = ea[nCurIdx].nTodayStartPrice;
                            ea[nCurIdx].timeLines1m.arrTimeLine[i].nDownFs = ea[nCurIdx].nTodayStartPrice;
                            ea[nCurIdx].timeLines1m.arrTimeLine[i].nTimeIdx = ea[nCurIdx].timeLines1m.nRealDataIdx; // 배열원소에 현재 타임라인 인덱스 삽입
                            ea[nCurIdx].timeLines1m.arrTimeLine[i].nTime = AddTimeBySec(nFirstTime, (ea[nCurIdx].timeLines1m.nRealDataIdx - BRUSH) * MINUTE_SEC); // PADDING의 경우 장식작시간보다 아래로 설정
                        }

                        int nCutSharedT = nSharedTime - nSharedTime % MINUTE_KIWOOM;
                        ea[nCurIdx].crushManager.nCrushMaxPrice = ea[nCurIdx].nTodayStartPrice;
                        ea[nCurIdx].crushManager.nCrushMaxTime = nCutSharedT;
                        ea[nCurIdx].crushManager.nCrushMinPrice = ea[nCurIdx].nTodayStartPrice;
                        ea[nCurIdx].crushManager.nCrushMinTime = nCutSharedT;
                        ea[nCurIdx].crushManager.nCrushOnlyMinPrice = ea[nCurIdx].nTodayStartPrice;
                        ea[nCurIdx].crushManager.nCrushOnlyMinTime = nCutSharedT;

                    } // END ---- 개인 초기작업


                    if (ea[nCurIdx].nCnt == 1)  // 첫데이터는  tv가 너무 높을 수 있느니 패스
                        return;

                    if (ea[nCurIdx].isViMode)
                    {
                        ea[nCurIdx].isViMode = false;
                        ea[nCurIdx].nViEndTime = nSharedTime;
                        ea[nCurIdx].isViGauge = true;
                    }

                    ea[nCurIdx].fPowerWithoutGap = ea[nCurIdx].fPower - ea[nCurIdx].fStartGap;
                    double fPowerDiff = ea[nCurIdx].fPowerWithoutGap - ea[nCurIdx].fPrevPowerWithoutGap;
                    ea[nCurIdx].fPrevPowerWithoutGap = ea[nCurIdx].fPowerWithoutGap;



                    // ==================================================
                    // 거래대금 및 수량 기록, 정리
                    // ==================================================
                    ea[nCurIdx].lTotalTradePrice += ea[nCurIdx].nFs * Math.Abs(ea[nCurIdx].nTv); // 1. 거래대금
                    ea[nCurIdx].lTotalTradeVolume += Math.Abs(ea[nCurIdx].nTv);
                    ea[nCurIdx].fTotalTradeVolume = (double)ea[nCurIdx].lTotalTradeVolume / ea[nCurIdx].lTotalNumOfStock; // 2. 상대적거래수량
                    ea[nCurIdx].lTotalBuyPrice += ea[nCurIdx].nFs * ea[nCurIdx].nTv; // 3. 매수대금
                    ea[nCurIdx].lTotalBuyVolume += ea[nCurIdx].nTv;
                    ea[nCurIdx].fTotalBuyVolume = (double)ea[nCurIdx].lTotalBuyVolume / ea[nCurIdx].lTotalNumOfStock; // 4. 상대적매수수량
                    ea[nCurIdx].lMarketCap = ea[nCurIdx].lTotalNumOfStock * ea[nCurIdx].nFs; // 7. 시가총액
                    ea[nCurIdx].lMinuteTradePrice += ea[nCurIdx].nFs * Math.Abs(ea[nCurIdx].nTv); // 8. 분당 거래대금
                    ea[nCurIdx].lMinuteTradeVolume += Math.Abs(ea[nCurIdx].nTv);
                    ea[nCurIdx].fMinuteTradeVolume = (double)ea[nCurIdx].lMinuteTradeVolume / ea[nCurIdx].lTotalNumOfStock; // 9. 분당 상대적거래수량
                    ea[nCurIdx].lMinuteBuyPrice += ea[nCurIdx].nFs * ea[nCurIdx].nTv; // 10. 분당 매수대금
                    ea[nCurIdx].lMinuteBuyVolume += ea[nCurIdx].nTv;
                    ea[nCurIdx].fMinuteBuyVolume = (double)ea[nCurIdx].lMinuteBuyVolume / ea[nCurIdx].lTotalNumOfStock; // 11. 분당 상대적거래수량
                    ea[nCurIdx].fMinutePower += fPowerDiff; // 12. 분당 손익율
                    ea[nCurIdx].nMinuteCnt++; // 분당 카운트
                    if (fPowerDiff != 0)
                        ea[nCurIdx].nMinuteUpDown++;


                    // =========================================================
                    // 개인구조체 현재 타임값들 기록
                    // =========================================================
                    if (ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nStartFs == 0)
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nStartFs = ea[nCurIdx].nFs;

                    ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nLastFs = ea[nCurIdx].nFs;

                    if (ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nStartFs < ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nLastFs)
                    {
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nUpFs = ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nLastFs;
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nDownFs = ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nStartFs;
                    }
                    else
                    {
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nUpFs = ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nStartFs;
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nDownFs = ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nLastFs;
                    }

                    if (ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nMaxFs == 0 || ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nMaxFs < ea[nCurIdx].nFs)
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nMaxFs = ea[nCurIdx].nFs;

                    if (ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nMinFs == 0 || ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nMinFs > ea[nCurIdx].nFs)
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nMinFs = ea[nCurIdx].nFs;

                    ea[nCurIdx].timeLines1m.nFsPointer = ea[nCurIdx].nFs;

                    if (ea[nCurIdx].nTv > 0)
                    {
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nTotalVolume += ea[nCurIdx].nTv;
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nTotalPrice += ea[nCurIdx].nTv * ea[nCurIdx].nFs;
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nBuyPrice += ea[nCurIdx].nTv * ea[nCurIdx].nFs;
                    }
                    else // 체결량이 0인 경우는 없다
                    {
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nTotalVolume -= ea[nCurIdx].nTv;
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nTotalPrice -= ea[nCurIdx].nTv * ea[nCurIdx].nFs;
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nSellPrice -= ea[nCurIdx].nTv * ea[nCurIdx].nFs;
                    }
                    ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].nCount++;

                    if (fPowerDiff > 0)
                    {
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].fAccumUpPower += fPowerDiff;
                    }
                    else if (fPowerDiff < 0)
                    {
                        ea[nCurIdx].timeLines1m.arrTimeLine[ea[nCurIdx].timeLines1m.nPrevTimeLineIdx].fAccumDownPower -= fPowerDiff;
                    }




                    ////////////////////////////////////////////////////////////////////
                    ////////////////////////////////////////////////////////////////////
                    ///////////// 점수 Part /////////////////////////////////////////////

                    // 일정시간마다 fJar 값을 감소시킨다. 이 일정시간을 어떻게 매길것인 지는 고민해볼 문제
                    // 시간 당 ... 
                    if (ea[nCurIdx].nPrevSpeedUpdateTime == 0)
                    {
                        ea[nCurIdx].nPrevSpeedUpdateTime = nFirstTime;
                    }
                    int nTimeUpdate = SubTimeToTimeAndSec(nSharedTime, ea[nCurIdx].nPrevSpeedUpdateTime) / SHORT_UPDATE_TIME;
                    if (ea[nCurIdx].isViGauge)
                    {
                        ea[nCurIdx].speedStatus.fPush = 0;
                        ea[nCurIdx].tradeStatus.fPush = 0;
                        ea[nCurIdx].pureTradeStatus.fPush = 0;
                        ea[nCurIdx].nPrevSpeedUpdateTime = nSharedTime;
                    }
                    else
                    {
                        for (int idxUpdate = 0; idxUpdate < nTimeUpdate; idxUpdate++)
                        {
                            // 스피드 업데이트
                            if (ea[nCurIdx].speedStatus.fVal == 0) // 초기 데이터의 경우
                                ea[nCurIdx].speedStatus.fVal = ea[nCurIdx].speedStatus.fPush;
                            else
                                ea[nCurIdx].speedStatus.fVal = ea[nCurIdx].speedStatus.fPush * fPushWeight + ea[nCurIdx].speedStatus.fVal * (1 - fPushWeight);
                            ea[nCurIdx].speedStatus.fPush = 0; // 속도푸쉬 초기화


                            // 체결량 업데이트
                            if (ea[nCurIdx].tradeStatus.fVal == 0)
                                ea[nCurIdx].tradeStatus.fVal = ea[nCurIdx].tradeStatus.fPush;
                            else
                                ea[nCurIdx].tradeStatus.fVal = ea[nCurIdx].tradeStatus.fPush * fPushWeight + ea[nCurIdx].tradeStatus.fVal * (1 - fPushWeight);
                            ea[nCurIdx].tradeStatus.fPush = 0;

                            // 순체결량 업데이트
                            if (ea[nCurIdx].pureTradeStatus.fVal == 0)
                                ea[nCurIdx].pureTradeStatus.fVal = ea[nCurIdx].pureTradeStatus.fPush;
                            else
                                ea[nCurIdx].pureTradeStatus.fVal = ea[nCurIdx].pureTradeStatus.fPush * fPushWeight + ea[nCurIdx].pureTradeStatus.fVal * (1 - fPushWeight);
                            ea[nCurIdx].pureTradeStatus.fPush = 0;

                            // 매수체결량 업데이트
                            if (ea[nCurIdx].pureBuyStatus.fVal == 0)
                                ea[nCurIdx].pureBuyStatus.fVal = ea[nCurIdx].pureBuyStatus.fPush;
                            else
                                ea[nCurIdx].pureBuyStatus.fVal = ea[nCurIdx].pureBuyStatus.fPush * fPushWeight + ea[nCurIdx].pureBuyStatus.fVal * (1 - fPushWeight);
                            ea[nCurIdx].pureBuyStatus.fPush = 0;

                            ea[nCurIdx].nPrevSpeedUpdateTime = nSharedTime; // 업데이트시간 초기화 


                            // 엄격한 방법의 업데이트시간 초기화
                            // ex) nUpdateTime = 20, nTimeUpdate = 38, nPrevSpeedUpdateTime = 0
                            // nPrevSpeedUpdateTime = 20 다음 update까지 2(40 - 38)초 남았음
                            // ea[nCurIdx].nPrevSpeedUpdateTime = AddTimeBySec(ea[nCurIdx].nPrevSpeedUpdateTime, nUpdateTime);
                        }
                    }


                    // 가격은 매초당 조금씩 줄여나가게 한다
                    // 1분이 지났을 시 0.47퍼센트
                    if (ea[nCurIdx].nPrevPowerUpdateTime == 0)
                    {
                        ea[nCurIdx].nPrevPowerUpdateTime = nFirstTime;
                    }
                    int nTimeGap = SubTimeToTimeAndSec(nSharedTime, ea[nCurIdx].nPrevPowerUpdateTime);
                    if (ea[nCurIdx].isViGauge)
                    {
                        ea[nCurIdx].isViGauge = false;
                        ea[nCurIdx].nPrevPowerUpdateTime = nSharedTime;
                    }
                    else
                    {
                        for (int idxTimeGap = 0; idxTimeGap < nTimeGap; idxTimeGap++)
                        {
                            // 가격변화 업데이트
                            ea[nCurIdx].fPowerJar *= 0.995;
                            ea[nCurIdx].fPlusCnt07 *= 0.7;
                            ea[nCurIdx].fMinusCnt07 *= 0.7;
                            ea[nCurIdx].fPlusCnt09 *= 0.9;
                            ea[nCurIdx].fMinusCnt09 *= 0.9;

                            ea[nCurIdx].nPrevPowerUpdateTime = nSharedTime;
                        }
                    }
                    // 파워는 최우선매수호가와 초기가격의 손익률로 계산한다


                    int nTimePassed = SubTimeToTimeAndSec(nSharedTime, ea[nCurIdx].nPrevSpeedUpdateTime); // 지난시간 = 현재시간 - 이전시간 
                    double fTimePassedWeight = (double)nTimePassed / SHORT_UPDATE_TIME; // 업데이트한 지 얼마 안됐을경우 지난시간이 지극히 낮고 nSpeedPush는 0에 가까울 것이다.
                    double fTimePassedPushWeight = fTimePassedWeight * fPushWeight; // fPushWeight에서 지난시간의 크기만큼만 곱해 현재정보(nSpeedPush) 적용

                    //  속도 실시간 처리
                    ea[nCurIdx].speedStatus.fPush++;
                    if (ea[nCurIdx].speedStatus.fVal == 0)
                        ea[nCurIdx].speedStatus.fCur = (double)ea[nCurIdx].speedStatus.fPush / SHORT_UPDATE_TIME;
                    else
                        ea[nCurIdx].speedStatus.fCur = (ea[nCurIdx].speedStatus.fPush * fTimePassedPushWeight + ea[nCurIdx].speedStatus.fVal * (1 - fTimePassedPushWeight)) / SHORT_UPDATE_TIME;

                    // 체결량 실시간 처리
                    ea[nCurIdx].tradeStatus.fPush += Math.Abs(ea[nCurIdx].nTv);

                    if (ea[nCurIdx].tradeStatus.fVal == 0)
                        ea[nCurIdx].tradeStatus.fCur = ea[nCurIdx].tradeStatus.fPush;
                    else
                        ea[nCurIdx].tradeStatus.fCur = ea[nCurIdx].tradeStatus.fPush * fTimePassedPushWeight + ea[nCurIdx].tradeStatus.fVal * (1 - fTimePassedPushWeight);

                    // 순체결량 실시간 처리
                    ea[nCurIdx].pureTradeStatus.fPush += ea[nCurIdx].nTv;
                    if (ea[nCurIdx].pureTradeStatus.fVal == 0)
                        ea[nCurIdx].pureTradeStatus.fCur = ea[nCurIdx].pureTradeStatus.fPush;
                    else
                        ea[nCurIdx].pureTradeStatus.fCur = ea[nCurIdx].pureTradeStatus.fPush * fTimePassedPushWeight + ea[nCurIdx].pureTradeStatus.fVal * (1 - fTimePassedPushWeight);

                    if (ea[nCurIdx].nTv > 0)
                        ea[nCurIdx].pureBuyStatus.fPush += ea[nCurIdx].nTv;
                    if (ea[nCurIdx].pureBuyStatus.fVal == 0)
                        ea[nCurIdx].pureBuyStatus.fCur = ea[nCurIdx].pureBuyStatus.fPush;
                    else
                        ea[nCurIdx].pureBuyStatus.fCur = ea[nCurIdx].pureBuyStatus.fPush * fTimePassedPushWeight + ea[nCurIdx].pureBuyStatus.fVal * (1 - fTimePassedPushWeight);

                    ea[nCurIdx].fSharePerTrade = ea[nCurIdx].lShareOutstanding / ea[nCurIdx].tradeStatus.fCur; // 0에 가까울 수  록 좋음
                    if (ea[nCurIdx].totalHogaVolumeStatus.fVal == 0)
                    {
                        ea[nCurIdx].fSharePerHoga = BILLION;
                        ea[nCurIdx].fHogaPerTrade = BILLION;
                    }
                    else
                    {
                        ea[nCurIdx].fSharePerHoga = ea[nCurIdx].lShareOutstanding / ea[nCurIdx].totalHogaVolumeStatus.fVal; // 0에 가까울 수 록 좋음
                        ea[nCurIdx].fHogaPerTrade = ea[nCurIdx].totalHogaVolumeStatus.fVal / ea[nCurIdx].tradeStatus.fCur; // 0에 가까울 수 록 좋음

                    }
                    /// 점수 공식 
                    if ((ea[nCurIdx].tradeStatus.fCur * ea[nCurIdx].nFs) < MILLION) // 현체결량이 100만원 이하면
                        ea[nCurIdx].fTradePerPure = ea[nCurIdx].pureTradeStatus.fCur / (MILLION / (double)ea[nCurIdx].nFs);
                    else
                    {
                        if (Math.Abs(ea[nCurIdx].pureTradeStatus.fCur) > ea[nCurIdx].tradeStatus.fCur)
                        {
                            ea[nCurIdx].fTradePerPure = ea[nCurIdx].pureTradeStatus.fCur / (ea[nCurIdx].tradeStatus.fCur + Math.Abs(ea[nCurIdx].pureTradeStatus.fCur));
                        }
                        else
                            ea[nCurIdx].fTradePerPure = ea[nCurIdx].pureTradeStatus.fCur / ea[nCurIdx].tradeStatus.fCur; // 절대값 1에 가까울 수 록 좋음 -면 매도, +면 매수
                    }
                    // 가격변화 실시간 처리
                    ea[nCurIdx].fPowerJar += fPowerDiff;

                    if (fPowerDiff > 0)
                    {
                        ea[nCurIdx].fPlusCnt07++;
                        ea[nCurIdx].fPlusCnt09++;
                    }
                    else if (fPowerDiff < 0)
                    {
                        ea[nCurIdx].fMinusCnt07++;
                        ea[nCurIdx].fMinusCnt09++;
                    }




                    //=====================================================
                    // 전략매매, 전략 사용해서 매매하는 부분
                    //=====================================================
                    if (ea[nCurIdx].TMPisOrderSignal)
                    {
                        ea[nCurIdx].TMPisOrderSignal = false;

                        curSlot.nStrategyIdx = 0; // 0번째 전략
                        curSlot.nOrderType = NEW_BUY;
                        curSlot.nRqTime = nSharedTime;
                        curSlot.nOrderPrice = ea[nCurIdx].nFs;
                        curSlot.nSequence = ++ea[nCurIdx].arrMyStrategy[curSlot.nStrategyIdx]; // 0번째 전략 + 1번째 매수 의미
                        curSlot.nBuyedSlotIdx = ea[nCurIdx].myTradeManager.nIdx;
                        curSlot.nEaIdx = nCurIdx;
                        curSlot.fRequestRatio = NORMAL_TRADE_RATIO;
                        curSlot.sHogaGb = MARKET_ORDER;
                        curSlot.sRQName = "임시입력매수";
                        curSlot.sCode = ea[nCurIdx].sCode;
                        curSlot.isStepByStepTrade = true;
                        curSlot.sOrgOrderId = "";

                        tradeQueue.Enqueue(curSlot);
                        testTextBox.AppendText("시간 : " + nSharedTime.ToString() + ", 종목코드 : " + sCode + ", 종목명 : " + ea[nCurIdx].sCodeName + ", 현재가 : " + ea[nCurIdx].nFs.ToString() + " 전략 : " + curSlot.nStrategyIdx.ToString() + " 깜짝오름매수신청 \r\n"); //++
                    }





                    // ==============================================================================
                    // 당일해당종목 매매 관리창
                    // ==============================================================================
                    // 추매, 유예, 선점 등의 부분
                    int nBuySlotIdx = ea[nCurIdx].myTradeManager.nIdx; // 매수블록갯수
                    if (nBuySlotIdx > 0) // 보유종목이 있다면 추가매수를 할것인지 분할매도를 할것인지 전량매도를 할것인 지 등등을 결정해야함.
                    {
                        int nBuyPrice;
                        double fYield;

                        for (int checkSellIterIdx = 0; checkSellIterIdx < nBuySlotIdx; checkSellIterIdx++) // 매매블록갯수만큼 반복함
                        {
                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isAllBuyed) // 구매가 완료됐다면
                            {
                                // 다 팔리지 않았고 파는 중이 아니라면
                                if (!ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isAllSelled && !ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isSelling)
                                {
                                    bool isSell = false;

                                    nBuyPrice = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBuyPrice; // 처음 초기화됐을때는 0인데 체결이 된 상태에서만 접근 가능하니 사졌을 때의 평균매입가
                                    fYield = (double)(ea[nCurIdx].nFb - nBuyPrice) / nBuyPrice; // 현재 최우선매수호가 와의 손익률을 구한다
                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fPower = fYield; // 수수료 미포함 손익율
                                    fYield -= STOCK_TAX + STOCK_FEE + STOCK_FEE; // 거래세와 거래수수료 차감
                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fPowerWithFee = fYield; // 수수료 포함 손익율


                                    // 타임라인, 이평선 정리 ( 10초 마다)
                                    int nTenSecPointer = SubTimeToTimeAndSec(nSharedTime, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthTime) / ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nTimeDegree + BRUSH; // 현재시간과 9시를 뺀 결과를 분단위로 받음
                                    if (nTenSecPointer != ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx)
                                    {
                                        int nSecIdxDiff = nTenSecPointer - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx;

                                        for (int eachStockIterIdx = 0; eachStockIterIdx < nSecIdxDiff; eachStockIterIdx++)
                                        {

                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx; // 지금은 nRealDataIdx인것
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx++; // 다음 페이즈로 넘어간다는 느낌

                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nTimeIdx = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx; // 배열원소에 현재 타임라인 인덱스 삽입
                                            int nCurTimeIdx = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx;

                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nTime = AddTimeBySec(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthTime, (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx - BRUSH) * ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nTimeDegree); // PADDING의 경우 장식작시간보다 아래로 설정
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nTimeIdx = nCurTimeIdx; // 이러면 장시작시간의 인덱스가 BRUSH 가 된다. 밀려나도 괜춚, 어차피 장시작시간은 수치가 다 강해서 어떻게든 수치를 줄일 필요가 있고 nTimeIdx는 분모로써 작용하니 괜춚

                                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nLastFs == 0) // 기록이 아예없는 경우 ( 초기 or vi or 거래없는경우)
                                            {
                                                if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nFsPointer == 0)
                                                {
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nStartFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthPrice;
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nLastFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthPrice;
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nMaxFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthPrice;
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nMinFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthPrice;
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nUpFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthPrice;
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nDownFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthPrice;
                                                }
                                                else
                                                {
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nStartFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nFsPointer;
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nLastFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nFsPointer;
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nMaxFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nFsPointer;
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nMinFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nFsPointer;
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nUpFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nFsPointer;
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nDownFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nFsPointer;
                                                }
                                            }

                                            // ---------------------------------------------------
                                            // 초기화를 위한 라인
                                            {
                                                if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nMaxUpFs < ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nUpFs) // 최대값 구하기
                                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nMaxUpFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nCurTimeIdx].nUpFs;
                                            }
                                        } // END ---- 반복문

                                        int j;

                                        // -------------------------------------------------
                                        // 추세
                                        // -------------------------------------------------
                                        {
                                            int nPick1, nPick2, nPick3, nPick4, nPick5, nPick6;
                                            int nLeftPick1, nRightPick2, nLeftPick3, nRightPick4, nLeftPick5, nRightPick6;

                                            // 기울기 확인
                                            for (j = 0; j < TEN_SEC_PICKUP_CNT; j++)
                                            {

                                                nPick1 = nPick2 = nPick3 = nPick4 = nPick5 = nPick6 = 0;
                                                nLeftPick1 = nRightPick2 = nLeftPick3 = nRightPick4 = nLeftPick5 = nRightPick6 = 0;

                                                int nRecentPickBefore = Min(TEN_SEC_PICK_BEFORE, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx);
                                                int nTenPickBefore = Min(TEN_SEC_TEN_BEFORE, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx);

                                                while ((nPick1 == nPick2) || (nPick3 == nPick4) || (nPick5 == nPick6)) // 같으면 기울기를 구할 수 없기 때문에 서로 다를때까지 반복한다.
                                                {
                                                    if (nPick1 == nPick2)
                                                    {
                                                        nPick1 = rand.Next(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx);  // 0 ~ 현재인덱스
                                                        nPick2 = rand.Next(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx);  // 0 ~ 현재인덱스
                                                        nLeftPick1 = Min(nPick1, nPick2);
                                                        nRightPick2 = Max(nPick1, nPick2);
                                                    }
                                                    if (nPick3 == nPick4)
                                                    {
                                                        nPick3 = rand.Next(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx - nRecentPickBefore, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx);
                                                        nPick4 = rand.Next(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx - nRecentPickBefore, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx);
                                                        nLeftPick3 = Min(nPick3, nPick4);
                                                        nRightPick4 = Max(nPick3, nPick4);
                                                    }
                                                    if (nPick5 == nPick6)
                                                    {
                                                        nPick5 = rand.Next(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx - nTenPickBefore, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx);
                                                        nPick6 = rand.Next(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx - nTenPickBefore, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx);
                                                        nLeftPick5 = Min(nPick5, nPick6);
                                                        nRightPick6 = Max(nPick5, nPick6);
                                                    }
                                                }

                                                // 왜 gap으로 나누냐면.... 간편해서
                                                // Q. 그러면 종목들 간 값의 차이가 나지 않느냐?? 9900짜리는 50으로 나누고 10000짜리는 100으로 나누는데??
                                                // A. 100으로 나눈다면 기울기값은 낮아진다. => 패널티를 받게된다. => 종목들 간 gap으로 나누는건 균형을 맞춘다(?? : 원래 한 단계를 넘을때 더 많은 저항이 생길 수 있으니까 패널티를 주는게 괜찮은거 같다는게 내 의견.)
                                                double fSlope = (double)(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nRightPick2].nLastFs - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nLeftPick1].nLastFs) / GetBetweenMinAndMax(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nRightPick2].nTimeIdx - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nLeftPick1].nTimeIdx, MIN_DENOM, MAX_DENOM);
                                                fSlope /= GetAutoGap(ea[nCurIdx].nMarketGubun, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nLastFs);

                                                double fRecentSlope = (double)(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nRightPick4].nLastFs - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nLeftPick3].nLastFs) / GetBetweenMinAndMax(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nRightPick4].nTimeIdx - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nLeftPick3].nTimeIdx, MIN_DENOM, MAX_DENOM); ;
                                                fRecentSlope /= GetAutoGap(ea[nCurIdx].nMarketGubun, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nLastFs);

                                                double fHourSlope = (double)(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nRightPick6].nLastFs - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nLeftPick5].nLastFs) / GetBetweenMinAndMax(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nRightPick6].nTimeIdx - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[nLeftPick5].nTimeIdx, MIN_DENOM, MAX_DENOM); ;
                                                fHourSlope /= GetAutoGap(ea[nCurIdx].nMarketGubun, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nLastFs);

                                                arrFSlope[j] = fSlope;
                                                arrRecentFSlope[j] = fRecentSlope;
                                                arrHourFSlope[j] = fHourSlope;
                                            }

                                            // 초기기울기
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fInitSlope = (double)(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nLastFs - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthPrice) / HOUR_COMMON_DENOM;
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fInitSlope /= GetAutoGap(ea[nCurIdx].nMarketGubun, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nLastFs);
                                            // 최대값 기울기
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fMaxSlope = (double)(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nMaxUpFs - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthPrice) / HOUR_COMMON_DENOM;
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fMaxSlope /= GetAutoGap(ea[nCurIdx].nMarketGubun, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nLastFs);

                                            // arrFSlope 모두 채움
                                            Array.Sort(arrFSlope, (x, y) => y.CompareTo(x)); // 아마 내림차순
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedian = (arrFSlope[TEN_SEC_PICKUP_CNT / 2 - 1] + arrFSlope[TEN_SEC_PICKUP_CNT / 2]) / 2; // 중위수 구함
                                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedian == 0)
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedian = arrFSlope.Sum() / TEN_SEC_PICKUP_CNT;
                                            // arrRecentFSlope 모두 채움
                                            Array.Sort(arrRecentFSlope, (x, y) => y.CompareTo(x)); // 아마 내림차순
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fRecentMedian = (arrRecentFSlope[TEN_SEC_PICKUP_CNT / 2 - 1] + arrRecentFSlope[TEN_SEC_PICKUP_CNT / 2]) / 2; // 중위수 구함
                                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fRecentMedian == 0)
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fRecentMedian = arrRecentFSlope.Sum() / TEN_SEC_PICKUP_CNT;
                                            // arrHourFSlope 모두 채움
                                            Array.Sort(arrHourFSlope, (x, y) => y.CompareTo(x)); // 아마 내림차순
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fHourMedian = (arrHourFSlope[TEN_SEC_PICKUP_CNT / 2 - 1] + arrHourFSlope[TEN_SEC_PICKUP_CNT / 2]) / 2; // 중위수 구함
                                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fHourMedian == 0)
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fHourMedian = arrHourFSlope.Sum() / TEN_SEC_PICKUP_CNT;

                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fInitAngle = GetAngleBetween(0, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fInitSlope);
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fMaxAngle = GetAngleBetween(0, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fMaxSlope);
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle = GetAngleBetween(0, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedian);
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fRecentMedianAngle = GetAngleBetween(0, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fRecentMedian);
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fHourMedianAngle = GetAngleBetween(0, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fHourMedian);

                                        } // END---- 추세

                                        // ------------------------------------------------------
                                        // 이평선
                                        // ------------------------------------------------------
                                        {
                                            int nShareIdx, nSummation;
                                            double fMaVal;


                                            // -----------
                                            // 2분 이평선
                                            nShareIdx = TEN_SEC_MA2M - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx - 1;
                                            nSummation = 0;
                                            if (nShareIdx <= 0) // 0보다 작다면 더미데이터가 아니라 전부 리얼데이터로 채울 수 있다는 의미
                                            {
                                                for (j = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx; j > ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx - TEN_SEC_MA2M; j--)
                                                {
                                                    nSummation += ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[j].nLastFs;
                                                }
                                            }
                                            else // 부족하다는 의미
                                            {
                                                for (j = 0; j <= ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx; j++)
                                                {
                                                    nSummation += ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[j].nLastFs;
                                                }
                                                for (j = 0; j < nShareIdx; j++)
                                                {
                                                    nSummation += ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthPrice; // 0~ BRUSH -1 까지는 다 같지만 그냥 0번째 데이터를 넣어줌
                                                }
                                            }
                                            fMaVal = (double)nSummation / TEN_SEC_MA2M; // 현재의 N이동평균선 값
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fCurMa20m = fMaVal;

                                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nDownFs > fMaVal) // 현재의 저가보다 이평선이 아래에 있다면
                                            {
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa20m++; // 아래가 좋은거임
                                                if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa20m > MA_EXCEED_CNT)// 오래동안 위였다가 가격이 ma를 뚫고 올라간다는 말
                                                {
                                                    // TODO.
                                                }
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa20m = 0;
                                            }
                                            else
                                            {
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa20m++;
                                                if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa20m > MA_EXCEED_CNT) // 오랫동안 아래였다가 가격이 ma를 뚫고 내려갔다는 말
                                                {
                                                    // TODO.
                                                }
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa20m = 0;
                                            }

                                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fMaxMa20m == 0 || ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fMaxMa20m < fMaVal)
                                            {
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fMaxMa20m = fMaVal;
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nMaxMa20mTime = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nTime;
                                            }


                                            // -----------
                                            // 10분 이평선
                                            nShareIdx = TEN_SEC_MA10M - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx - 1;
                                            nSummation = 0;
                                            if (nShareIdx <= 0) // 0보다 작다면 더미데이터가 아니라 전부 리얼데이터로 채울 수 있다는 의미
                                            {
                                                for (j = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx; j > ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx - TEN_SEC_MA10M; j--)
                                                {
                                                    nSummation += ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[j].nLastFs;
                                                }
                                            }
                                            else // 부족하다는 의미
                                            {
                                                for (j = 0; j <= ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx; j++)
                                                {
                                                    nSummation += ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[j].nLastFs;
                                                }
                                                for (j = 0; j < nShareIdx; j++)
                                                {
                                                    nSummation += ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthPrice; // 0~ BRUSH -1 까지는 다 같지만 그냥 0번째 데이터를 넣어줌
                                                }
                                            }
                                            fMaVal = (double)nSummation / TEN_SEC_MA10M; // 현재의 N이동평균선 값
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fCurMa1h = fMaVal;

                                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nDownFs > fMaVal) // 현재의 저가보다 이평선이 아래에 있다면
                                            {
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa1h++; // 아래가 좋은거임
                                                if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa1h > MA_EXCEED_CNT)// 오래동안 위였다가 가격이 ma를 뚫고 올라간다는 말
                                                {
                                                    // TODO.
                                                }
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa1h = 0;
                                            }
                                            else
                                            {
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa1h++;
                                                if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa1h > MA_EXCEED_CNT) // 오랫동안 아래였다가 가격이 ma를 뚫고 내려갔다는 말
                                                {
                                                    // TODO.
                                                }
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa1h = 0;
                                            }

                                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fMaxMa1h == 0 || ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fMaxMa1h < fMaVal)
                                            {
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fMaxMa1h = fMaVal;
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nMaxMa1hTime = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nTime;
                                            }


                                            // -----------
                                            // 20분 이평선
                                            nShareIdx = TEN_SEC_MA20M - ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx - 1;
                                            nSummation = 0;
                                            if (nShareIdx <= 0) // 0보다 작다면 더미데이터가 아니라 전부 리얼데이터로 채울 수 있다는 의미
                                            {
                                                for (j = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx; j > ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx - TEN_SEC_MA20M; j--)
                                                {
                                                    nSummation += ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[j].nLastFs;
                                                }
                                            }
                                            else // 부족하다는 의미
                                            {
                                                for (j = 0; j <= ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx; j++)
                                                {
                                                    nSummation += ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[j].nLastFs;
                                                }
                                                for (j = 0; j < nShareIdx; j++)
                                                {
                                                    nSummation += ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthPrice; // 0~ BRUSH -1 까지는 다 같지만 그냥 0번째 데이터를 넣어줌
                                                }
                                            }
                                            fMaVal = (double)nSummation / TEN_SEC_MA20M; // 현재의 N이동평균선 값
                                            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fCurMa2h = fMaVal;

                                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nDownFs > fMaVal) // 현재의 저가보다 이평선이 아래에 있다면
                                            {
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa2h++; // 아래가 좋은거임
                                                if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa2h > MA_EXCEED_CNT)// 오래동안 위였다가 가격이 ma를 뚫고 올라간다는 말
                                                {
                                                    // TODO.
                                                }
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa2h = 0;
                                            }
                                            else
                                            {
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa2h++;
                                                if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa2h > MA_EXCEED_CNT) // 오랫동안 아래였다가 가격이 ma를 뚫고 내려갔다는 말
                                                {
                                                    // TODO.
                                                }
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa2h = 0;
                                            }

                                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fMaxMa2h == 0 || ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fMaxMa2h < fMaVal)
                                            {
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fMaxMa2h = fMaVal;
                                                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nMaxMa2hTime = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nRealDataIdx].nTime;
                                            }
                                        }// END ---- 이평선

                                        int nR = 3;
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].eachSw.WriteLine(nSharedTime + "\t" + ea[nCurIdx].sCode + "\t" + ea[nCurIdx].sCodeName + "\t" + ea[nCurIdx].sMarketGubunTag + "\t" +
                                                                                                "##각도##" + "\t" +
                                                                                               Math.Round(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle, nR) + "\t" +
                                                                                               Math.Round(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fHourMedianAngle, nR) + "\t" +
                                                                                               Math.Round(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fRecentMedianAngle, nR) + "\t" +
                                                                                               Math.Round(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fInitAngle, nR) + "\t" +
                                                                                               Math.Round(ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fMaxAngle, nR) + "\t" +
                                                                                               "##이평선업##" + "\t" +
                                                                                               ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa20m + "\t" +
                                                                                               ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa1h + "\t" +
                                                                                               ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa2h + "\t" +
                                                                                               "##이평선다운##" + "\t" +
                                                                                               ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa20m + "\t" +
                                                                                               ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa1h + "\t" +
                                                                                               ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nUpCntMa2h
                                                                                               );

                                    }// END ---- 타임라인( 각도, 이평선 ) 업데이트 정리


                                    // --------------------------------------------------
                                    // 다음 타임라인 정보 저장
                                    // --------------------------------------------------
                                    if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nStartFs == 0)
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nStartFs = ea[nCurIdx].nFs;

                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nLastFs = ea[nCurIdx].nFs;

                                    if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nStartFs < ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nLastFs)
                                    {
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nUpFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nLastFs;
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nDownFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nStartFs;
                                    }
                                    else
                                    {
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nUpFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nStartFs;
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nDownFs = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nLastFs;
                                    }

                                    if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nMaxFs == 0 || ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nMaxFs < ea[nCurIdx].nFs)
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nMaxFs = ea[nCurIdx].nFs;

                                    if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nMinFs == 0 || ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nMinFs > ea[nCurIdx].nFs)
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nMinFs = ea[nCurIdx].nFs;

                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nFsPointer = ea[nCurIdx].nFs;

                                    if (ea[nCurIdx].nTv > 0)
                                    {
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nTotalVolume += ea[nCurIdx].nTv;
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nTotalPrice += ea[nCurIdx].nTv * ea[nCurIdx].nFs;
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nBuyPrice += ea[nCurIdx].nTv * ea[nCurIdx].nFs;
                                    }
                                    else // 체결량이 0인 경우는 없다
                                    {
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nTotalVolume -= ea[nCurIdx].nTv;
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nTotalPrice -= ea[nCurIdx].nTv * ea[nCurIdx].nFs;
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nSellPrice -= ea[nCurIdx].nTv * ea[nCurIdx].nFs;
                                    }
                                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].nCount++;

                                    if (fPowerDiff > 0)
                                    {
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].fAccumUpPower += fPowerDiff;
                                    }
                                    else if (fPowerDiff < 0)
                                    {
                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.arrTimeLine[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.nPrevTimeLineIdx].fAccumDownPower -= fPowerDiff;
                                    }

                                    

                                    //////////////////////////////////////////////////////////////////////////////////
                                    /////  가격변동, 시간변동, 가격가속도, 시간가속도, 호가속도, 체결속도, 체결량, 호가매수매도대비, vi_cnt  등등 고려해서 ( 가능성을 봐야함 .. ) 
                                    /////  매니징한다. ex) 더 일찍 판다던가 팔지않고 기다린다던가 아니면 추가매수를 한다던가 방식
                                    ////////////////////////////////////////////////////////////////////////////////////
                                    /// 대응의 영역
                                    /// 
                                    //{
                                    //    if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isStepByStepTrade) // 단계별 매매기법
                                    //    {

                                    //        //조건 테스트 영역
                                    //        {
                                    //            //------------------------------
                                    //            //각도 분기문
                                    //            //------------------------------
                                    //             //일반 각도
                                    //            if (ea[nCurIdx].timeLines1m.fTotalMedianAngle >= 10)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle10p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle10p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fTotalMedianAngle >= 20)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle20p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle20p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fTotalMedianAngle >= 30)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle30p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle30p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fTotalMedianAngle >= 40)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle40p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle40p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fTotalMedianAngle >= 50)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle50p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle50p = false;

                                    //            if (ea[nCurIdx].timeLines1m.fTotalMedianAngle <= -10)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle10m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle10m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fTotalMedianAngle <= -20)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle20m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle20m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fTotalMedianAngle <= -30)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle30m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle30m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fTotalMedianAngle <= -40)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle40m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle40m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fTotalMedianAngle <= -50)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle50m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle50m = false;


                                    //            //십오분 각도
                                    //            if (ea[nCurIdx].timeLines1m.fRecentMedianAngle >= 10)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle10p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle10p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fRecentMedianAngle >= 20)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle20p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle20p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fRecentMedianAngle >= 30)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle30p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle30p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fRecentMedianAngle >= 40)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle40p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle40p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fRecentMedianAngle >= 50)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle50p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle50p = false;

                                    //            if (ea[nCurIdx].timeLines1m.fRecentMedianAngle <= -10)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle10m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle10m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fRecentMedianAngle <= -20)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle20m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle20m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fRecentMedianAngle <= -30)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle30m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle30m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fRecentMedianAngle <= -40)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle40m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle40m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fRecentMedianAngle <= -50)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle50m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRecentAngle50m = false;


                                    //            //한시간 각도
                                    //            if (ea[nCurIdx].timeLines1m.fHourMedianAngle >= 10)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle10p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle10p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fHourMedianAngle >= 20)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle20p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle20p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fHourMedianAngle >= 30)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle30p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle30p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fHourMedianAngle >= 40)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle40p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle40p = false;
                                    //            if (ea[nCurIdx].timeLines1m.fHourMedianAngle >= 50)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle50p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle50p = false;

                                    //            if (ea[nCurIdx].timeLines1m.fHourMedianAngle <= -10)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle10m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle10m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fHourMedianAngle <= -20)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle20m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle20m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fHourMedianAngle <= -30)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle30m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle30m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fHourMedianAngle <= -40)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle40m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle40m = false;
                                    //            if (ea[nCurIdx].timeLines1m.fHourMedianAngle <= -50)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle50m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isHourAngle50m = false;


                                    //            //------------------------------
                                    //            //이평선 분기문
                                    //            //------------------------------
                                    //            if (ea[nCurIdx].maOverN.fCurMa20m > ea[nCurIdx].maOverN.fCurMa1h)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMa20mOverMa1h = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMa20mOverMa1h = false;
                                    //            if (ea[nCurIdx].maOverN.fCurMa1h > ea[nCurIdx].maOverN.fCurMa2h)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMa1hOverMa2h = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMa1hOverMa2h = false;
                                    //            if (ea[nCurIdx].maOverN.nDownCntMa20m == 0)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa20m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa20m = false;
                                    //            if (ea[nCurIdx].maOverN.nDownCntMa1h == 0)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa1h = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa1h = false;
                                    //            if (ea[nCurIdx].maOverN.nDownCntMa2h == 0)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa2h = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa2h = false;

                                    //            //------------------------------
                                    //            //순위 분기문
                                    //            //------------------------------
                                    //            if (ea[nCurIdx].rankSystem.nSummationRanking <= 10)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isOverallRankIn10 = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isOverallRankIn10 = false;
                                    //            if (ea[nCurIdx].rankSystem.nSummationRanking <= 20)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isOverallRankIn20 = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isOverallRankIn20 = false;
                                    //            if (ea[nCurIdx].rankSystem.nSummationRanking <= 50)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isOverallRankIn50 = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isOverallRankIn50 = false;
                                    //            if (ea[nCurIdx].rankSystem.nSummationRanking <= 100)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isOverallRankIn100 = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isOverallRankIn100 = false;
                                    //            if (ea[nCurIdx].rankSystem.nMinuteSummationRanking <= 5)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMinuteRankIn5 = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMinuteRankIn5 = false;
                                    //            if (ea[nCurIdx].rankSystem.nMinuteSummationRanking <= 10)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMinuteRankIn10 = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMinuteRankIn10 = false;
                                    //            if (ea[nCurIdx].rankSystem.nMinuteSummationRanking <= 20)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMinuteRankIn20 = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMinuteRankIn20 = false;
                                    //            if (ea[nCurIdx].rankSystem.nMinuteSummationRanking <= 30)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMinuteRankIn30 = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMinuteRankIn30 = false;
                                    //            if (ea[nCurIdx].rankSystem.nSummationMove >= 30)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove30p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove30p = false;
                                    //            if (ea[nCurIdx].rankSystem.nSummationMove >= 100)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove100p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove100p = false;
                                    //            if (ea[nCurIdx].rankSystem.nSummationMove >= 200)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove200p = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove200p = false;
                                    //            if (ea[nCurIdx].rankSystem.nSummationMove <= -30)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove30m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove30m = false;
                                    //            if (ea[nCurIdx].rankSystem.nSummationMove <= -100)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove100m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove100m = false;
                                    //            if (ea[nCurIdx].rankSystem.nSummationMove <= -200)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove200m = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isRankMove200m = false;

                                    //            //------------------------------
                                    //            //매수후 각도 분기문
                                    //            // ------------------------------
                                    //             //일반 각도
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle >= 5)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle5pAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle5pAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle >= 10)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle10pAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle10pAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle >= 15)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle15pAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle15pAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle >= 20)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle20pAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle20pAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle >= 25)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle25pAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle25pAfterPurchase = false;

                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle <= -5)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle5mAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle5mAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle <= -10)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle10mAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle10mAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle <= -15)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle15mAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle15mAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle <= -20)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle20mAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle20mAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].tenLineManager.fTotalMedianAngle <= -25)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle25mAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isTotalAngle25mAfterPurchase = false;


                                    //            //------------------------------
                                    //            //매수후 이평선 분기문
                                    //            // ------------------------------
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fCurMa20m > ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fCurMa1h)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMa20mOverMa1hAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMa20mOverMa1hAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fCurMa1h > ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.fCurMa2h)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMa1hOverMa2hAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isMa1hOverMa2hAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa20m == 0)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa20mAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa20mAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa1h == 0)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa1hAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa1hAfterPurchase = false;
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].maOverN.nDownCntMa2h == 0)
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa2hAfterPurchase = true;
                                    //            else
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].myCondition.isDownFsOverMa2hAfterPurchase = false;

                                    //        }

                                    //        // ---------------------------------------------------------
                                    //        // 선점 조건 확인
                                    //        // ----------------------------------------------------------
                                    //        if (SubTimeToTimeAndSec(nSharedTime, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nBirthTime) >= PREEMPTION_ACCESS_SEC)  //선점의 영역, 일정시간동안 접근불가
                                    //        {
                                    //            // PREEMPTION_UPDATE_SEC 마다 검사할 부분
                                    //            if (SubTimeToTimeAndSec(nSharedTime, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nPreemptionPrevUpdateTime) >= PREEMPTION_UPDATE_SEC)
                                    //            {
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nPreemptionPrevUpdateTime = nSharedTime;

                                    //            }

                                    //            // 매 tick마다 검사할 부분
                                    //            {
                                    //                if ((false) ||
                                    //                    (false)
                                    //                    )
                                    //                {
                                    //                    isSell = true;
                                    //                }

                                    //            }
                                    //        }


                                    //        if (fYield <= ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fBottomPer) // 처분라인
                                    //        {

                                    //            bool isSellForce = false; // respiteSignal과 관계없이 무조건 매도를 해야하는 경우

                                    //            // ---------------------------------------------------------
                                    //            // 유예 조건 확인
                                    //            // ----------------------------------------------------------
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isRespiteSignal) // 유예시그널
                                    //            {
                                    //                if (SubTimeToTimeAndSec(nSharedTime, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nRespitePrevUpdateTime) >= RESPITE_LIMIT_SEC) // 순간최대 유예기간                                                        {
                                    //                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isRespiteSignal = false;
                                    //                else if (SubTimeToTimeAndSec(nSharedTime, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nRespitePrevUpdateTime) >= RESPITE_UPDATE_SEC) // 업데이트 유예기간 
                                    //                {
                                    //                    if ((false) ||
                                    //                         (false)
                                    //                       ) // 쪽지 시험
                                    //                    {
                                    //                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isRespiteSignal = false;
                                    //                    }
                                    //                }
                                    //            } // END ---- 유예시그널

                                    //            if (!ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isRespiteSignal) // 유예중이 아닐때
                                    //            {
                                    //                if ((false) ||
                                    //                    (false)
                                    //                    ) // 유예조건, 수능시험
                                    //                {
                                    //                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isRespiteSignal = true; // 유예시그널 on
                                    //                    ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nRespitePrevUpdateTime = nSharedTime; // 유예이전업데이트시간 on
                                    //                    if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fRespiteCriticalLine == RESPITE_INIT) // 이전 유예가 끝났다면 (상승선을 한번 터치하면 이전 유예는 사라진다)
                                    //                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fRespiteCriticalLine = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fPowerWithFee - 0.02; // 새로운 유예 첫 손절선 등록
                                    //                    if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nRespiteFirstTime == 0)// 이전 유예가 끝났다면
                                    //                    {
                                    //                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nRespiteFirstTime = nSharedTime; // 새로운 유예 첫시간을 등록 
                                    //                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nEachRespiteCount++; // 독립적인 유예의 카운트 등록
                                    //                    }
                                    //                }
                                    //            }

                                    //            if ((ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isRespiteSignal && ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fRespiteCriticalLine > ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fPowerWithFee) ||// 손해선 밑으로 내려온 상황
                                    //                (ea[nCurIdx].maOverN.nUpCntMa2h >= 20) ||// 20분 이상 120분평선을 탈출한 상황 
                                    //                (false)
                                    //                ) // 무조건 팔아야하는 조건들
                                    //            {
                                    //                isSellForce = true;
                                    //            }

                                    //            // -------------------------------------
                                    //            // 유예 최종결과
                                    //            // -------------------------------------
                                    //            if (!ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isRespiteSignal || isSellForce) // 유예중이 아니라면
                                    //            {
                                    //                isSell = true;
                                    //            } // End --- 유예 최종결과

                                    //        } // END ---- 처분라인
                                    //        else if (fYield >= ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fTargetPer) // 상승라인
                                    //        {
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nSequence == ea[nCurIdx].arrMyStrategy[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nStrategyIdx]) // 해당전략의 마지막 시퀀스인덱스번째만 접근 가능( 처음의 경우(첫매수가 끝나고 나서 ) nSequence가 1이고 전략의 번째수가 1임 )
                                    //            {
                                    //                // ---------------------------------------------------------
                                    //                // 추매 조건 확인
                                    //                // ----------------------------------------------------------
                                    //                if ((false) &&
                                    //                    (false)
                                    //                   ) // 추매조건 
                                    //                {
                                    //                    curSlot.nSequence = ++ea[nCurIdx].arrMyStrategy[ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nStrategyIdx];
                                    //                    curSlot.nStrategyIdx = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nStrategyIdx;
                                    //                    curSlot.nOrderType = NEW_BUY;
                                    //                    curSlot.nRqTime = nSharedTime;
                                    //                    curSlot.nOrderPrice = ea[nCurIdx].nFs;
                                    //                    curSlot.nEaIdx = nCurIdx;
                                    //                    curSlot.fRequestRatio = 0.3;
                                    //                    curSlot.sHogaGb = MARKET_ORDER;
                                    //                    curSlot.sRQName = "깜짝오름매수";
                                    //                    curSlot.sCode = ea[nCurIdx].sCode;
                                    //                    curSlot.isStepByStepTrade = true;
                                    //                    curSlot.sOrgOrderId = "";

                                    //                    tradeQueue.Enqueue(curSlot);
                                    //                }
                                    //            }
                                    //            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isRespiteSignal)  // 상승선을 터치했으니 이전 유예정보를 초기화한다
                                    //            {
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isRespiteSignal = false;
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fRespiteCriticalLine = RESPITE_INIT;
                                    //                ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nRespiteFirstTime = 0;
                                    //            }

                                    //            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nLastTouchLineTime = nSharedTime;
                                    //            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nCurLineIdx++;
                                    //            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fTargetPer = GetHigherCeilingTarget(ref ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nCurLineIdx); // something higher
                                    //            ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fBottomPer = GetHigherFloorTarget(ref ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nCurLineIdx); // something higher
                                    //        } // END---- 상승라인
                                    //    } // END ---- 단계형 매매
                                    //    else //일괄형 매매
                                    //    {

                                    //        if (fYield >= ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fTargetPer) // 손익률이 익절퍼센트를 넘기면
                                    //        {
                                    //            isSell = true;
                                    //            curSlot.sRQName = "일괄익절매도";
                                    //            testTextBox.AppendText(nSharedTime.ToString() + " : " + sCode + " 익절매도신청 \r\n"); //++
                                    //        }
                                    //        else if (fYield <= ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].fBottomPer) // 손익률이 손절퍼센트보다 낮으면
                                    //        {
                                    //            isSell = true;
                                    //            curSlot.sRQName = "일괄손절매도";
                                    //            testTextBox.AppendText(nSharedTime.ToString() + " : " + sCode + " 손절매도신청 \r\n"); //++
                                    //        }

                                    //    } // END---- 일괄형 매매


                                    //    if (isSell) // 매도 시그널  on
                                    //    {
                                    //        curSlot.nOrderType = NEW_SELL; // 신규매도
                                    //        curSlot.sCode = sCode;
                                    //        curSlot.nQty = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nCurVolume; // 이 레코드에 있는 전량을 판매한다
                                    //        curSlot.sHogaGb = "03";
                                    //        curSlot.sOrgOrderId = "";
                                    //        curSlot.nBuyedSlotIdx = checkSellIterIdx; // 나중에 요청전송이 실패할때 다시 취소하기 위해 적어놓는 변수
                                    //        curSlot.nEaIdx = nCurIdx; // 현재 종목의 인덱스
                                    //        curSlot.nRqTime = nSharedTime; // 현재시간 설정

                                    //        tradeQueue.Enqueue(curSlot); // 매매요청큐에 인큐한다
                                    //        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isSelling = true;

                                    //    }
                                    //} // END---- 대응의 영역

                                }
                            } // END ---- 구매가 완료됐다면
                            else // 구매가 완료되지 않았다면 매수취소 가능성이 있다.
                            {
                                if (!ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isCanceling) // 취소 가능하다면
                                {
                                    // 현재 최우선매도호가가 지정상한가를 넘었거나 매매 요청시간과 현재시간이 너무 오래 차이난다면(= 매수가 너무 오래걸린다 = 거래량이 낮고 머 별거 없다)
                                    if ((ea[nCurIdx].nFs > ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nOrderPrice) || (SubTimeToTimeAndSec(nSharedTime, ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].nRequestTime) >= MAX_REQ_SEC)) // 지정가를 초과하거나 오래걸린다면
                                    {
                                        curSlot.sRQName = "매수취소";
                                        curSlot.nOrderType = 3; // 매수취소
                                        curSlot.sCode = sCode;
                                        curSlot.sOrgOrderId = ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].sCurOrgOrderId; // 현재 매수의 원주문번호를 넣어준다.
                                        curSlot.nRqTime = nSharedTime; // 현재시간설정
                                        curSlot.nBuyedSlotIdx = checkSellIterIdx;

                                        tradeQueue.Enqueue(curSlot); // 매매신청큐에 인큐

                                        ea[nCurIdx].myTradeManager.arrBuyedSlots[checkSellIterIdx].isCanceling = true; // 현재 매수취소 불가능상태로 만든다
                                        testTextBox.AppendText(nSharedTime.ToString() + " : " + sCode + " 매수취소신청 \r\n"); //++

                                    }
                                }
                            } // END ---- 구매가 완료되지 않았다면
                        } // END ---- 반복적 확인 종료


                    } // END ---- 보유종목이 있다면

                }
            }// End ---- e.sRealType.Equals("주식체결")
        }// END ---- 실시간데이터핸들러

    }
}
