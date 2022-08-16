using System.Collections.Generic;
using System.IO;

namespace MJTradier
{
    public partial class Form1
    {
        // ============================================
        // 각 종목이 가지는 개인 구조체
        // ============================================
        public struct EachStock
        {
            // ----------------------------------
            // 공용 변수
            // ----------------------------------
            public bool isExcluded; // 실시간 제외대상확인용 bool변수
            public int nLastRecordTime; // 마지막 기록시간
            public long lTotalTradePrice; // 거래대금(매수+매도대금)
            public long lTotalTradeVolume; // 거래수량
            public double fTotalTradeVolume; // 거래수량비율
            public long lTotalBuyPrice; // 매수대금(매수-매도대금)
            public long lTotalBuyVolume; // 매수수량
            public double fTotalBuyVolume; // 매수수량비율
            public long lMarketCap; // 시가총액
            public int nCnt; // 인덱스 
            public long lMinuteTradePrice;
            public double fMinuteTradeVolume;
            public long lMinuteBuyPrice;
            public double fMinuteBuyVolume;
            public long lMinuteTradeVolume;
            public long lMinuteBuyVolume;
            public double fMinutePower;
            public int nMinuteCnt;
            public int nMinuteUpDown;
            public int nJumpCnt;

            // ----------------------------------
            // 매수가격설정 변수
            // ----------------------------------
            // TODO.

            public BuyedManager myTradeManager;
            public MaOverN maOverN;
            public TimeLineManager timeLines1m;
            public CrushManager crushManager;
            public RankSystem rankSystem;

            // ----------------------------------
            // 기본정보 변수
            // ----------------------------------
            public string sRealScreenNum; // 실시간화면번호
            public string sCode; // 종목번호
            public string sCodeName; // 종목명
            public int nMarketGubun; // 코스닥번호면 코스닥, 코스피번호면 코스피
            public string sMarketGubunTag; // 코스닥번호면 "KOSDAQ", 코스피번호면 "KOSPI"
            public long lShareOutstanding; // 유통주식수
            public long lTotalNumOfStock;  // 상장주식수
            public int nYesterdayEndPrice; // 전날 종가 


            // ----------------------------------
            // 매매요청용 변수
            // ----------------------------------
            

            // 매매취소용 변수

            // ----------------------------------
            // 매매관련 변수
            // ----------------------------------
            public int nHoldingsCnt; // 보유종목수
            public double fAccumBuyedRatio; // 최대매수기준 현재 비율(추매 시 1을 넘을 수 있음) // 추가??
            public int[] arrMyStrategy; // 나의 전략을 담는 배열, 인덱스는 자신만의 전략마다 임의로 설정

            // ----------------------------------
            // 초기 변수
            // ----------------------------------
            public bool isFirstCheck;    // 초기설정용 bool 변수
            public int nTodayStartPrice; // 시초가
            public int nStartGap;    // 갭 가격
            public double fStartGap; // 갭 등락율

            // ----------------------------------
            // 주식호가 변수
            // ----------------------------------
            public int nTotalBuyHogaVolume; // 총매수호가수량
            public int nTotalSellHogaVolume; // 총매도호가수량
            public int nThreeSellHogaVolume; // 매도1~3호가수량
            public int nTotalHogaVolume; //  총호가수량
            public double fHogaRatio; // 매수매도대비율

            // ----------------------------------
            // 호가상태 변수
            // ----------------------------------
            public CurStatus totalHogaVolumeStatus;
            public CurStatus hogaRatioStatus;
            public CurStatus hogaSpeedStatus;
            public int nHogaCnt; // 호가카운트
            public int nPrevHogaUpdateTime; // 호가이전조정시간

            // ----------------------------------
            // 주식체결 변수
            // ----------------------------------
            public int nFs; // 최우선 매도호가
            public int nFb; // 최우선 매수호가
            public double fDiff; // 최우선 매수호가와 최우선 매도호가의 값 차이
            public int nTv;  // 체결량 
            public double fTs; // 체결강도
            public double fPowerWithoutGap; // 시초가 등락율
            public double fPower; // 전일종가 등락률 
            public double fPrevPowerWithoutGap; // 이전 시초가 등락율;

            // ----------------------------------
            // 체결상태 변수
            // ----------------------------------
            public int nPrevSpeedUpdateTime; // 이전기본(속도, 체결량, 순체결량)조정 시간
            public int nPrevPowerUpdateTime; // 이전가격조정 시간
            public CurStatus speedStatus;
            public CurStatus tradeStatus;
            public CurStatus pureTradeStatus;
            public CurStatus pureBuyStatus;
            public double fCntPerTime; // 시간 대비 누적카운트 
            public double fPlusCnt09; // 초당0.9 상승카운트
            public double fMinusCnt09; // 초당0.9 하락카운트
            public double fPlusCnt07; // 초당0.7 상승카운트
            public double fMinusCnt07; // 초당0.7 하락카운트
            public double fPowerJar; // 초당0.995 가격변화카운트
            public double fSharePerTrade; // 체결량 대비 유통주
            public double fTradePerPure; // 체결량차 대비 체결량
            public double fHogaPerTrade; // 체결량 대비 호가 
            public double fSharePerHoga; // 호가 대비 유통주

            // ----------------------------------
            // Vi관련 변수
            // ----------------------------------
            public bool isViMode; // 현재 vi상태인가? 장 초반에 vi인척하는 딜레이상태가 존재하는 듯 보임
            public bool isViGauge; // vi가 종료되면 그동안 허비된 공백시간만큼의 조정을 제외하기 위한 변수
            public int nViStartTime; // vi 시작시간
            public int nViEndTime; // vi 종료시간 nViEndTime - nViStartTime  >= 2min 인 지 확인하여 vi인지 vi인척하는 놈인지 체크 


            // ----------------------------------
            // 임시 변수
            // ----------------------------------
            public bool TMPisOrderSignal;
            
        }


        // ============================================
        // 매매요청 큐에 저장하기 위한 구조체변수
        // ============================================
        public struct TradeRequestSlot
        {
            // ----------------------------------
            // 공용 인자들
            // ----------------------------------
            public int nRqTime; // 주문요청시간
            public double fTargetPercent; // 익절 퍼센트 
            public double fBottomPercent; // 손절 퍼센트 
            public int nEaIdx; // 개인구조체인덱스
            public bool isStepByStepTrade; // true면 단계별 상승매매, false면 익절,손절 일괄매매
            public int nStrategyIdx; // 전략 인덱스

            // ----------------------------------
            // 매수 인자들
            // ----------------------------------
            public double fRequestRatio; // 매수신청시 최대매수가 기준 비율 
            public int nSequence; // 추매용 확인 인덱스( 몇번째 추매인 지 : 2번부터가 추매1번 )

            // ----------------------------------
            // 매도 인자들
            // ----------------------------------
            public int nBuyedSlotIdx; // 구매열람인덱스 , 매도요청이 실패하면 해당인덱스를 통해 다시 요청할 수 있게 하기 위한 변수

            // ----------------------------------
            // SendOrder 인자들
            // ----------------------------------
            public string sRQName; // 사용자 구분명
            public string sScreenNo; // 화면번호
            public string sAccNo; // 계좌번호 10자리
            public int nOrderType; // 주문유형 1:신규매수, 2:신규매도 3:매수취소, 4:매도취소, 5:매수정정, 6:매도정정
            public string sCode; // 종목코드(6자리)
            public int nQty; // 주문수량
            public int nOrderPrice; // 주문가격
            public string sHogaGb; // 거래구분 (00:지정가, 03:시장가, ...)
            public string sOrgOrderId;  // 원주문번호. 신규주문에는 공백 입력, 정정/취소시 입력합니다.
        }

        public struct CurStatus
        {
            public double fVal; // 과거
            public double fPush; // 현재
            public double fCur; // 결과
        }
        
        // ============================================
        // 종합 개인구조체 매매블록 구조체
        // ============================================
        public struct BuyedManager
        {
            
            public int nIdx;
            public BuyedSlot[] arrBuyedSlots;

            // 현재 거래중... 정보
            public int nBuyReqCnt; // 현재 종목의 매수신청카운트
            public int nSellReqCnt; // 현재 종목의 매도신청카운트 
            public bool isOrderStatus; // 현재 매매중인 지 확인하는 변수;

            // 전달데이터
            public double fTargetPercent; // 익절 퍼센트
            public double fBottomPercent; // 손절 퍼센트
            public int nCurRqTime; // 매수주문했을때의 시간 요청( 매수취소를 위한 변수 )
            public int nCurRqPrice;
            public int nCurLimitPrice; // 지정가가 estimatedPrice를 초과하는 미체결 수량이 남았다면 처분하기 위한 변수
            public int nSequence;
            public int nStrategyIdx; // 전략 인덱스
            public double fTradeRatio; // 매매시 매매비율
            public int nCurBuyedSlotIdx; // 현재해당 매매블록 인덱스, 매도시 사용
            public bool isStepByStepTrade; // true면 단계별 상승매매, false면 익절,손절 일괄매매
            public int nOrderVolume; // 주문 수량

            public void Init()
            {
                arrBuyedSlots = new BuyedSlot[BUY_SLOT_MAX_NUM];
            }
        }
        // ============================================
        // 개인구조체 매매블록 구조체
        // ============================================
        public struct BuyedSlot //TODAY
        {
            public int nBuyedSlotId; // 매매블록 아이디 인덱스// 추가??
            public int nBuyPrice; // 구매한 가격
            public int nCurVolume; // 보유 주식수
            public int nOrderVolume; // 주문 수량
            public int nOrderPrice; // 주문 가격
            public int nOriginOrderPrice; // 상한패딩 붙이기 전 가격
            public int nRequestTime; // 매수요청시간 // 추가??
            public int nReceiptTime; // 매수접수시간
            public int nBuyEndTime; //  매수체결완료시간
            public double fTradeRatio; // 구매비율

            public string sCurOrgOrderId; // 원주문번호   default:""
            public bool isSelling; // 매도 중 시그널
            public bool isAllSelled; // 매도 종료(모두 팔림)
            public bool isAllBuyed; // 매수완료 시그널 ( 같은 매매블럭에 추매를 했을때 다 사졌나를 확인하기 위한 변수 ) 
            public bool isCanceling; // 현재 매수에서 매수취소가 나왔으면 더이상의 현재의 거래에서 매수취소요청을 금지하기 위한 변수

            public bool isStepByStepTrade; // true면 단계별 상승매매, false면 단순형 일괄매매
            public double fTargetPer; // 얼마에 익절할거야
            public double fBottomPer; // 얼마에 손절할거야

            public int nStrategyIdx; // 전략 인덱스
            public int nSequence; // 추매시 확인용 인덱스

            // 경과 확인용
            public double fPower; // 현재 순수 손익율
            public double fPowerWithFee; // 세금 수수료 포함 손익율
            public int nCurLineIdx; // 현재 익절선과 손절선의 인덱스

            public double fTotalMedianAngle; // 토탈 중위수 각도
            public double fHourMedianAngle; // 한시간 중위수 각도
            public double fRecentMedianAngle; // 15분 중위수 각도

            public int nBirthTime;
            public int nBirthPrice;

            public bool isRespiteSignal; // 유예중인지 확인변수
            public int nRespiteFirstTime; // 해당 유예의 첫시간
            public int nRespitePrevUpdateTime; // 해당 유예의 이전업데이트 시간
            public double fRespiteCriticalLine; // 유예 한계선
            public int nEachRespiteCount; // 독립적인 유예 횟수
            public int nPreemptionPrevUpdateTime; // 선점 최신업데이트 시간
            public int nLastTouchLineTime; // 상승선을 건드린 마지막 시간

            public TimeLineManager tenLineManager;
            public MaOverN maOverN;
            public Condition myCondition;
            public StreamWriter eachSw; // 두두두둥
        }


        // ============================================
        // 현재보유종목 열람용 구조체변수
        // ============================================
        public struct Holdings
        {
            public string sCode;
            public string sCodeName;
            public double fYield;
            public int nHoldingQty;
            public int nBuyedPrice;
            public int nCurPrice;
            public int nTotalPL;
            public int nNumPossibleToSell;
        }

        // ============================================
        // 게시판용 변수
        // ============================================
        public struct StockDashBoard
        {
            public int nDashBoardCnt;
            public StockPiece[] stockPanel; // 현재 갱신용 패널 (개인구조체마다) 
        }

        public struct StockPiece // 정렬대상을 가지고 있다
        {
            public string sCode; // 종목코드
            public int nCodeIdx; // 정수형 종목코드 
            public int nEaIdx; // 코드 인덱스

            public long lTotalTradePrice; // 거래대금
            public double fTotalTradeVolume; // 거래수량
            public long lTotalBuyPrice; // 매수대금
            public double fTotalBuyVolume; // 매수수량
            public int nAccumCount; // 누적카운트
            public double fTotalPowerWithOutGap; // 갭제외수익률 
            public long lMarketCap; // 시가총액
            public int nSummationRank; // 총 순위합


            public long lMinuteTradePrice; // 분당 거래대금
            public double fMinuteTradeVolume; // 분당 상대적거래수량
            public long lMinuteBuyPrice; // 분당 매수대금
            public double fMinuteBuyVolume; // 분당 상대적매수수량
            public double fMinutePower; // 분당 손익율(갭제외)
            public int nMinuteCnt; // 분당 카운트
            public int nMinuteUpDown; // 분당 위아래
            public int nSummationMinuteRank; // 총 분당순위합

        }

        // ============================================
        // 타임라인 변수
        // ============================================
        public struct TimeLineManager
        {
            public int nRealDataIdx;  // 실제 데이터들이 들어있는 최종인덱스
            public int nPrevTimeLineIdx; // 관리용으로 인덱스가 한칸 더 앞에 있음
            public int nTimeDegree; // 시간단위(초)
            public int nFsPointer;
            public double fTotalMedian;
            public double fTotalMedianAngle;
            public double fInitSlope;
            public double fInitAngle;
            public int nMaxUpFs;
            public double fMaxSlope;
            public double fMaxAngle;
            public double fHourMedian;
            public double fHourMedianAngle;
            public double fRecentMedian;
            public double fRecentMedianAngle;
            public TimeLine[] arrTimeLine;
        }

        public struct TimeLine
        {
            public int nTime;
            public int nTimeIdx;
            public int nMaxFs;
            public int nMinFs;
            public int nStartFs;
            public int nLastFs;
            public int nUpFs;
            public int nDownFs;
            public int nTotalVolume;
            public int nBuyVolume;
            public int nSellVolume;
            public int nTotalPrice;
            public int nBuyPrice;
            public int nSellPrice;
            public int nCount;
            public double fAccumUpPower;
            public double fAccumDownPower;

        }
        public struct CrushManager
        {
            public int nCrushMaxPrice;
            public int nCrushMaxTime;
            public int nCrushMinPrice;
            public int nCrushMinTime;
            public int nCrushOnlyMinPrice;
            public int nCrushOnlyMinTime;
            public int nPrevCrushCnt;
            public int nCurCnt;
            public int nUpCnt;
            public int nDownCnt;
            public int nSpecialDownCnt;
            public List<Crush> crushList;
        }

        public struct Crush
        {
            public int nCnt;
            public double fMaxMinPower;
            public double fCurMinPower;
            public int nMaxMinTime;
            public int nMaxCurTime;
            public int nMinCurTime;
            public int nMinPrice;
            public int nMaxPrice;
            public double fUpperNow;
        }

        public struct RankSystem
        {
            public int nTime;
            public int nCurIdx; // 누적카운트

            // 전체
            public int nTotalTradePriceRanking; // 거래대금
            public int nTotalTradeVolumeRanking; // 상대적거래수량
            public int nTotalBuyPriceRanking; // 매수대금
            public int nTotalBuyVolumeRanking; // 상대적매수수량
            public int nAccumCountRanking; // 누적카운트
            public int nPowerRanking; // 손익률
            public int nMarketCapRanking; // 시가총액
            public int nSummationRanking; // 총 순위
            public int nPrevSummationRanking; // 이전 총순위
            public int nSummationMove; // 총 순위 변동

            // 분당
            public int nMinuteTradePriceRanking; // 분당 거래대금 순위
            public int nMinuteTradeVolumeRanking; // 분당 상대적거래수량 순위
            public int nMinuteBuyPriceRanking; // 분당 매수대금 순위
            public int nMinuteBuyVolumeRanking; // 분당 상대적매수수량 순위
            public int nMinutePowerRanking; // 분당 손익율
            public int nMinuteCountRanking; // 분당 카운트
            public int nMinuteUpDownRanking; // 분당 위아래
            public int nMinuteSummationRanking; // 분당 순위

            public int nRankHold10; // 총 순위 10위권 이내 유지시간
            public int nRankHold20; // 총 순위 20위권 이내 유지시간
            public int nRankHold50; // 총 순위 50위권 이내 유지시간
            public int nRankHold100; // 총 순위 100위권 이내 유지시간
            public int nRankHold200; // 총 순위 200위권 이내 유지시간

            public Ranking[] arrRanking;
        }


        public struct Ranking
        {
            public int nRecordTime; // 기록용 시간

            // 전체
            public int nTotalTradePriceRanking; // 1. 거래대금
            public int nTotalTradeVolumeRanking; // 2. 상대적거래수량
            public int nTotalBuyPriceRanking; // 3. 매수대금
            public int nTotalBuyVolumeRanking; // 4. 상대적매수수량
            public int nAccumCountRanking; // 5. 누적카운트
            public int nPowerRanking; // 6. 손익률
            public int nMarketCapRanking; // 7. 시가총액
            public int nSummationRanking; // 8. 총 순위
             
            // 분당
            public int nMinuteTradePriceRanking; // 1. 분당 거래대금
            public int nMinuteTradeVolumeRanking; // 2. 분당 상대적 거래수량
            public int nMinuteBuyPriceRanking; // 3. 분당 매수대금
            public int nMinuteBuyVolumeRanking; // 4. 분당 상대적 매수수량
            public int nMinutePowerRanking; // 5. 분당 손익율
            public int nMinuteCountRanking; // 6. 분당 카운트
            public int nMinuteUpDownRanking; // 7. 분당 위아래 카운트
            public int nMinuteRanking; //  8. 분당 랭킹

        }

        public struct MaOverN
        {
            public double fMaxMa20m;
            public double fMaxMa1h;
            public double fMaxMa2h;

            public double fCurMa20m;
            public double fCurMa1h;
            public double fCurMa2h;

            public int nMaxMa20mTime;
            public int nMaxMa1hTime;
            public int nMaxMa2hTime;

            public int nUpCntMa20m;
            public int nUpCntMa1h;
            public int nUpCntMa2h;

            public int nDownCntMa20m;
            public int nDownCntMa1h;
            public int nDownCntMa2h;
        }
        
        // Discrete한 파트
        public struct Condition // 매매조건. 추후 유망포인트도 추가 예정
        {
            // 각도
            public bool isTotalAngle10p; // 1. 전체각도 + 10도
            public bool isTotalAngle20p; // 1. 전체각도 + 20도
            public bool isTotalAngle30p; // 1. 전체각도 + 30도
            public bool isTotalAngle40p; // 1. 전체각도 + 40도
            public bool isTotalAngle50p; // 1. 전체각도 + 50도
            public bool isTotalAngle10m; // 1. 전체각도 - 10도
            public bool isTotalAngle20m; // 1. 전체각도 - 20도
            public bool isTotalAngle30m; // 1. 전체각도 - 30도
            public bool isTotalAngle40m; // 1. 전체각도 - 40도
            public bool isTotalAngle50m; // 1. 전체각도 - 50도
            public bool isHourAngle10p; //  2. 시간각도 + 10도
            public bool isHourAngle20p; //  2. 시간각도 + 20도
            public bool isHourAngle30p; //  2. 시간각도 + 30도
            public bool isHourAngle40p; //  2. 시간각도 + 40도
            public bool isHourAngle50p; //  2. 시간각도 + 50도
            public bool isHourAngle10m; //  2. 시간각도 - 10도
            public bool isHourAngle20m; //  2. 시간각도 - 20도
            public bool isHourAngle30m; //  2. 시간각도 - 30도
            public bool isHourAngle40m; //  2. 시간각도 - 40도
            public bool isHourAngle50m; //  2. 시간각도 - 50도
            public bool isRecentAngle10p; // 3. 최근각도 + 10도
            public bool isRecentAngle20p; // 3. 최근각도 + 20도
            public bool isRecentAngle30p; // 3. 최근각도 + 30도
            public bool isRecentAngle40p; // 3. 최근각도 + 40도
            public bool isRecentAngle50p; // 3. 최근각도 + 50도
            public bool isRecentAngle10m; // 3. 최근각도 - 10도
            public bool isRecentAngle20m; // 3. 최근각도 - 20도
            public bool isRecentAngle30m; // 3. 최근각도 - 30도
            public bool isRecentAngle40m; // 3. 최근각도 - 40도
            public bool isRecentAngle50m; // 3. 최근각도 - 50도

            // 이평선
            public bool isMa20mOverMa1h; // Ma20m > Ma1h
            public bool isMa1hOverMa2h; // Ma1h > Ma2h
            public bool isDownFsOverMa20m; // nDownFs > Ma20m
            public bool isDownFsOverMa1h; // nDownFs > Ma1h
            public bool isDownFsOverMa2h; // nDownFs > Ma2h

            // 순위
            public bool isOverallRankIn10; // 종합순위 10위 이내
            public bool isOverallRankIn20; // 종합순위 20위 이내
            public bool isOverallRankIn50; // 종합순위 50위 이내
            public bool isOverallRankIn100; // 종합순위 100위 이내


            public bool isMinuteRankIn5; // 분당순위 5위 이내
            public bool isMinuteRankIn10; // 분당순위 10위 이내
            public bool isMinuteRankIn20; // 분당순위 20위 이내
            public bool isMinuteRankIn30; // 분당순위 30위 이내
            public bool isRankMove30p; // 종합순위 -> +30
            public bool isRankMove100p; // 종합순위 -> +100
            public bool isRankMove200p; // 종합순위 -> +200
            public bool isRankMove30m; // 종합순위 -> -30 , 마이너스가 좋은거임
            public bool isRankMove100m; // 종합순위 -> -100, 마이너스가 좋은거임
            public bool isRankMove200m; // 종합순위 -> -200, 마이너스가 좋은거임

            

            // 매수후 각도
            public bool isTotalAngle5pAfterPurchase; // 1. 매수후 전체각도 + 5도
            public bool isTotalAngle10pAfterPurchase; // 1. 매수후 전체각도 + 10도
            public bool isTotalAngle15pAfterPurchase; // 1. 매수후 전체각도 + 15도
            public bool isTotalAngle20pAfterPurchase; // 1. 매수후 전체각도 + 20도
            public bool isTotalAngle25pAfterPurchase; // 1. 매수후 전체각도 + 25도
            public bool isTotalAngle5mAfterPurchase; // 1. 매수후 전체각도 - 5도
            public bool isTotalAngle10mAfterPurchase; // 1. 매수후 전체각도 - 10도
            public bool isTotalAngle15mAfterPurchase; // 1. 매수후 전체각도 - 15도
            public bool isTotalAngle20mAfterPurchase; // 1. 매수후 전체각도 - 20도
            public bool isTotalAngle25mAfterPurchase; // 1. 매수후 전체각도 - 25도


            // 매수후 이평선
            public bool isMa20mOverMa1hAfterPurchase; // Ma20m > Ma1h
            public bool isMa1hOverMa2hAfterPurchase; // Ma1h > Ma2h
            public bool isDownFsOverMa20mAfterPurchase; // nDownFs > Ma20m
            public bool isDownFsOverMa1hAfterPurchase; // nDownFs > Ma1h
            public bool isDownFsOverMa2hAfterPurchase; // nDownFs > Ma2h

            
        }
    }
}
