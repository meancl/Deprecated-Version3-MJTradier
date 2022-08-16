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


        // ----------------------------------------------
        // DB화되면 변경해야함
        private const string sBasicInfoPath = @"기본정보\"; // 기본정보를 받아올 폴더
        public StreamReader sr; // 기본정보 파일입력 모듈
        public int srCnt; // 해당 파일이 없을경우 TR요청 번째수
        private string[] kosdaqCodes; // 코스닥 종목들을 저장한 문자열 배열
        private string[] kospiCodes; //  코스피 종목들을 저장한 문자열 배열 

        private int[] eachStockIdxArray; // Array[개인구조체 종목코드] => 개인구조체 인덱스
        public const int INIT_CODEIDX_NUM = -1; // eachStockIdxArray 초기화 상수
        EachStock[] ea;  // 각 주식이 가지는 실시간용 구조체(개인구조체)
        public int nCurIdx; // 현재사용중인 개인구조체의 인덱스
        private int nEachStockIdx; // eachStockIdxArray[개인구조체 종목코드] <= nEachStockIdx++;
        public int nStockLength; // 관리대상종목 수
        
        private const int MAX_STOCK_NUM = 1000000; // 최종주식종목 수 0 ~ 999999 
        public const int MAX_STOCK_HOLDINGS_NUM = 200; // 보유주식을 저장하는 구조체 최대 갯수
        private const byte KOSDAQ_ID = 0;  // 코스닥을 증명하는 상수
        private const byte KOSPI_ID = 1; // 코스피를 증명하는 상수
        public const int NUM_SEP_PER_SCREEN = 100; // 한 화면번호 당 가능요청 수
        public const int STRATEGY_NUM = 20; // 전략 갯수

        // ============================================
        // 주식종목들을 특정 txt파일에서 읽어
        // 코스닥, 코스피 변수에 string[] 형식으로 각각 저장
        // 코스닥, 코스피 종목갯수의 합만큼의 eachStockArray구조체 배열을 생성 
        // 단일개체(모든 종목들이 공용으로 사용하는 개체) 초기화
        // ============================================
        private void MappingFileToStockCodes()
        {
            kosdaqCodes = File.ReadAllLines("today_kosdaq_stock_code.txt");
            kospiCodes = File.ReadAllLines("today_kospi_stock_code.txt");

            testTextBox.AppendText("Kosdaq : " + kosdaqCodes.Length.ToString() + "\r\n"); //++
            testTextBox.AppendText("Kospi : " + kospiCodes.Length.ToString() + "\r\n"); //++

            nStockLength = kosdaqCodes.Length + kospiCodes.Length;

            eachStockIdxArray = new int[MAX_STOCK_NUM]; // 개인구조체 포인터 어레이 할당 000000~999999( 총 1000000개 )

            for (int i = 0; i < MAX_STOCK_NUM; i++)
            {
                eachStockIdxArray[i] = INIT_CODEIDX_NUM;
            }
            ea = new EachStock[nStockLength]; // 개인구조체 할당 
            holdingsArray = new Holdings[MAX_STOCK_HOLDINGS_NUM];
            stockDashBoard.stockPanel = new StockPiece[nStockLength];

        }


        // ============================================
        // string형  코스닥, 코스피 종목코드의 배열 string[n] 변수에서
        // 한 화면번호 당 (최대)100개씩 넣고 주식체결 fid를 넣고
        // 실시간 데이터 요청을 진행
        // 코스닥과 코스피 배열에서 100개가 안되는 나머지 종목들은 코스닥,코스피 각 다른 화면번호에 실시간 데이터 요청
        // ============================================
        private void SubscribeRealData()
        {
            testTextBox.AppendText("구독 시작..\r\n"); //++
            int kosdaqIndex = 0;
            int kosdaqCodesLength = kosdaqCodes.Length;
            int kosdaqIterNum = kosdaqCodesLength / NUM_SEP_PER_SCREEN;
            int kosdaqRestNum = kosdaqCodesLength % NUM_SEP_PER_SCREEN;
            string strKosdaqCodeList;
            const string sFID = "41;228"; // 체결강도. 실시간 목록 FID들 중 겹치는게 가장 적은 FID
            string sScreenNum;
            // ------------------------------------------------------
            // 코스닥 실시간 등록
            // ------------------------------------------------------
            // 100개 단위
            for (int kosdaqIterIdx = 0; kosdaqIterIdx < kosdaqIterNum; kosdaqIterIdx++)
            {
                sScreenNum = SetRealScreenNo();
                strKosdaqCodeList = ConvertStrCodeList(kosdaqCodes, kosdaqIndex, kosdaqIndex + NUM_SEP_PER_SCREEN, KOSDAQ_ID, sScreenNum);
                axKHOpenAPI1.SetRealReg(sScreenNum, strKosdaqCodeList, sFID, "0");
                kosdaqIndex += NUM_SEP_PER_SCREEN;
            }
            // 나머지
            if (kosdaqRestNum > 0)
            {
                sScreenNum = SetRealScreenNo();
                strKosdaqCodeList = ConvertStrCodeList(kosdaqCodes, kosdaqIndex, kosdaqIndex + kosdaqRestNum, KOSDAQ_ID, sScreenNum);
                axKHOpenAPI1.SetRealReg(sScreenNum, strKosdaqCodeList, sFID, "0");
            }

            int kospiIndex = 0;
            int kospiCodesLength = kospiCodes.Length;
            int kospiIterNum = kospiCodesLength / NUM_SEP_PER_SCREEN;
            int kospiRestNum = kospiCodesLength % NUM_SEP_PER_SCREEN;
            string strKospiCodeList;

            // ------------------------------------------------------
            // 코스피 실시간 등록
            // ------------------------------------------------------
            // 100개 단위
            for (int kospiIterIdx = 0; kospiIterIdx < kospiIterNum; kospiIterIdx++)
            {
                sScreenNum = SetRealScreenNo();
                strKospiCodeList = ConvertStrCodeList(kospiCodes, kospiIndex, kospiIndex + NUM_SEP_PER_SCREEN, KOSPI_ID, sScreenNum);
                axKHOpenAPI1.SetRealReg(sScreenNum, strKospiCodeList, sFID, "0");
                kospiIndex += NUM_SEP_PER_SCREEN;
            }
            // 나머지
            if (kospiRestNum > 0)
            {
                sScreenNum = SetRealScreenNo();
                strKospiCodeList = ConvertStrCodeList(kospiCodes, kospiIndex, kospiIndex + kospiRestNum, KOSPI_ID, sScreenNum);
                axKHOpenAPI1.SetRealReg(sScreenNum, strKospiCodeList, sFID, "0");
            }
            testTextBox.AppendText("구독 완료..\r\n"); //++

        } // End ---- SubscribeRealData




        // ============================================
        // 매개변수 : 
        //  1.  string[] codes : 주식종목코드 배열
        //  2.  s : 배열의 시작 인덱스
        //  3.  e : 배열의 끝 인덱스 (포함 x)
        //  4.  marketGubun : 코스닥, 코스피 구별변수
        //  5.  sScreenNum : 실시간 화면번호
        //
        // 키움 실시간 신청메소드의 두번째 인자인 strCodeList는
        // 종목코드1;종목코드2;종목코드3;....;종목코드n(;마지막은 생략가능) 형식으로 넘겨줘야하기 때문에
        // s부터 e -1 인덱스까지 string 변수에 추가하며 사이사이 ';'을 붙여준다
        //
        // 실시간메소드에서 각 종목의 구조체를 사용하기 위해 초기화과정이 필요한데
        // 이 메소드에서 같이 진행해준다.
        // ============================================
        private string ConvertStrCodeList(string[] codes, int s, int e, int marketGubun, string sScreenNum)
        {
            string sCodeList = "";
            string sEachBasicInfo;
            string[] sBasicInfoSplited;

            for (int j = s; j < e; j++)
            {
                int codeIdx = int.Parse(codes[j]);

                ////// eachStockIdx 설정 부분 ///////
                eachStockIdxArray[codeIdx] = nEachStockIdx++;
                
                ////// eachStock 초기화 부분 ////////// 
                nCurIdx = eachStockIdxArray[codeIdx]; // 개인구조체인덱스 설정
                ea[nCurIdx].sRealScreenNum = sScreenNum; // 화면번호 설정
                ea[nCurIdx].sCode = codes[j]; // 종목코드 설정
                ea[nCurIdx].nMarketGubun = marketGubun; // 장구분 설정
                if (ea[nCurIdx].nMarketGubun == KOSPI_ID) // 장구분 문자열 설정
                    ea[nCurIdx].sMarketGubunTag = "KOSPI";
                else
                    ea[nCurIdx].sMarketGubunTag = "KOSDAQ";
                ea[nCurIdx].arrMyStrategy = new int[STRATEGY_NUM]; // 개인구조체 전략배열 초기화 
                ea[nCurIdx].crushManager.crushList = new List<Crush>(); // 개인구조체 전고점 리스트 초기화
                ea[nCurIdx].rankSystem.arrRanking = new Ranking[SubTimeToTimeAndSec(MARKET_END_TIME, MARKET_START_TIME) / MINUTE_SEC]; // 개인구조체 게시판순위 초기화
                ea[nCurIdx].myTradeManager.Init(); // 개인구조체 매매관리자 초기화

                // 게시판의 각 종목 초기화
                tmpStockPiece.sCode = ea[nCurIdx].sCode;
                tmpStockPiece.nCodeIdx = codeIdx;
                tmpStockPiece.nEaIdx = nCurIdx;
                stockDashBoard.stockPanel[nCurIdx] = tmpStockPiece;


                ea[nCurIdx].timeLines1m.nTimeDegree = MINUTE_SEC;
                ea[nCurIdx].timeLines1m.arrTimeLine = new TimeLine[BRUSH + SubTimeToTimeAndSec(MARKET_END_TIME, MARKET_START_TIME) / ea[nCurIdx].timeLines1m.nTimeDegree];

                // DB화 진행
                bool isEmpty = false;
                try
                {
                    sr = new System.IO.StreamReader(sBasicInfoPath + codes[j] + ".txt");
                    sEachBasicInfo = sr.ReadLine();
                    isEmpty = true;
                    sBasicInfoSplited = sEachBasicInfo.Split(',');

                    ea[nCurIdx].sCodeName = sBasicInfoSplited[0];
                    ea[nCurIdx].lShareOutstanding = long.Parse(sBasicInfoSplited[1]);
                    ea[nCurIdx].lTotalNumOfStock = long.Parse(sBasicInfoSplited[2]);
                    ea[nCurIdx].nYesterdayEndPrice = Math.Abs(int.Parse(sBasicInfoSplited[3]));

                    sr.Close();
                }
                catch (Exception ex)
                {
                    if (isEmpty)
                        sr.Close();
                    srCnt += 1;
                    RequestBasicStockInfo(codes[j]);
                    testTextBox.AppendText(codes[j] + " 종목은 기존파일이 없어서 " + srCnt.ToString() + "번째 TR요청\r\n");
                    Delay(1000);
                }

                string sDate = DateTime.Now.ToString("yyyy-MM-dd"); //삭제예정

                sCodeList += codes[j];
                if (j < e - 1)
                    sCodeList += ';';
            }
            return sCodeList;
        } // End ---- ConvertStrCodeList 


    }
}
