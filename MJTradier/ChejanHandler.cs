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

        public int nCurDeposit;  // 현재 예수금 // chejan
        public int nCurDepositCalc; // 계산하기 위한 예수금... 으음 // chejan
        public char[] charsToTrim = { '+', '-', ' ' }; // 키움API 데이터에 +, -, 공백이 같이 들어와짐 // chejan

        public const double STOCK_TAX = 0.0023; // 거래세 
        public const double STOCK_FEE = 0.00015; // 증권 매매수수료
        public const double VIRTUAL_STOCK_FEE = 0.0035; // 가상증권 매매수수료
        public const double TOTAL_STOCK_COMMISSION = STOCK_TAX + VIRTUAL_STOCK_FEE * 2; // 최종 거래수수료 *현재 : 거래세 + 가상증권 매매수수료 *  2( 가상증권 매수수수료 + 가상증권매매수수료 )

        // ==================================================
        // 주식주문(접수, 체결, 잔고) 이벤트발생시 핸들러메소드
        // ==================================================
        private void OnReceiveChejanDataHandler(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveChejanDataEvent e)
        {
            if (e.sGubun.Equals("0")) // 접수와 체결 
            {

                string sTradeTime = axKHOpenAPI1.GetChejanData(908); // 체결시간
                string sCode = axKHOpenAPI1.GetChejanData(9001).Substring(1); // 종목코드 전위 알파벳하나가 붙어서 삭제
                int nCodeIdx = int.Parse(sCode);
                nCurIdx = eachStockIdxArray[nCodeIdx]; // 해당 개인구조체 인덱스
                int nCurBuySlotIdx = ea[nCurIdx].myTradeManager.nCurBuyedSlotIdx; // 해당 매매블록의 인덱스
                string sOrderType = axKHOpenAPI1.GetChejanData(905).Trim(charsToTrim); // +매수, -매도, 매수취소
                string sOrderStatus = axKHOpenAPI1.GetChejanData(913).Trim(); // 주문상태(접수, 체결, 확인)
                string sOrderId = axKHOpenAPI1.GetChejanData(9203).Trim(); // 주문번호
                int nOrderVolume = Math.Abs(int.Parse(axKHOpenAPI1.GetChejanData(900))); // 주문수량
                string sCurOkTradePrice = axKHOpenAPI1.GetChejanData(914).Trim(); // 단위체결가 없을땐 ""
                string sCurOkTradeVolume = axKHOpenAPI1.GetChejanData(915).Trim(); // 단위체결량 없을땐 ""
                int nNoTradeVolume = Math.Abs(int.Parse(axKHOpenAPI1.GetChejanData(902))); // 미체결량


                // ---------------------------------------------
                // 매수 데이터 수신 순서
                // 매수접수 - 매수체결
                // 매수접수 - (매수취소) - 매수체결
                // 매수접수 - (매수취소) - 매수취소접수 - 매수취소확인 - 매수접수(매수취소확인)
                // 매수접수 - (매수취소) - 매수체결 - 매수취소접수 - 매수취소확인 - 매수체결(매수취소확인)
                // ---------------------------------------------
                if (sOrderType.Equals("매수"))
                {

                    if (sOrderStatus.Equals("체결"))
                    {
                        // 매수-체결됐으면 3가지로 나눠볼 수 있는데
                        // 1. 일반적으로 일부 체결된 경우
                        // 2. 전량 체결된 경우
                        // 3. 일부 체결된 후 나머지는 매수취소된 경우(미체결 클리어를 위해 얻어지는 경우)


                        // 문자열로 받아진 단위체결량과 단위체결가를 정수로 바꾸는 작업을 한다.
                        // 접수나 취소 때는 체결가~ 종류는 "" 공백으로 받아지기 때문에
                        // 정수 캐스팅을 하면 오류가 나기 때문이다
                        int nCurOkTradeVolume;
                        int nCurOkTradePrice;
                        try
                        {
                            nCurOkTradeVolume = Math.Abs(int.Parse(sCurOkTradeVolume)); // n단위체결량
                            nCurOkTradePrice = Math.Abs(int.Parse(sCurOkTradePrice)); // n단위체결가
                        }
                        catch (Exception ex)
                        {
                            // 혹시 문자열이 ""이라면 매수체결시 받아지는 체결 메시지다.
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].isCanceling = false; // 해당종목의 현재매수취소버튼 초기화
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].isAllBuyed = true; // 해당종목의 매수레코드의 매수완료 on
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBuyEndTime = nSharedTime; // 주문체결완료 시간 설정
                            if (ea[nCurIdx].myTradeManager.nBuyReqCnt > 0)
                                ea[nCurIdx].myTradeManager.nBuyReqCnt--; // 매수요청 카운트 감소
                            ea[nCurIdx].myTradeManager.isOrderStatus = false; // 매매중 off
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].tenLineManager.nTimeDegree = TEN_SEC;
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBirthTime = nSharedTime - nSharedTime % ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].tenLineManager.nTimeDegree;
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].tenLineManager.arrTimeLine = new TimeLine[BRUSH + SubTimeToTimeAndSec(MARKET_END_TIME, ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBirthTime) / ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].tenLineManager.nTimeDegree];
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBirthPrice = ea[nCurIdx].nFs;

                            // 두두두둥
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].eachSw = new StreamWriter(new FileStream(sEachPath + ea[nCurIdx].sCode + "-" + ea[nCurIdx].sCodeName + "-" + ea[nCurIdx].myTradeManager.nIdx.ToString() + "번째" + "--게시판.txt", FileMode.Create));
                            return;
                        }
                        // 예수금에 지정상한가와 매입금액과의 차이만큼을 다시 복구시켜준다.
                        nCurDepositCalc += (ea[nCurIdx].myTradeManager.nCurLimitPrice - nCurOkTradePrice) * nCurOkTradeVolume; // 예수금에 (추정매수가 - 실매수가) * 실매수량 더해준다. //**

                        // 이것은 현재매수 구간이기 떄문에
                        // 해당레코드의 평균매입가와 매수수량을 조정하기 위한 과정이다
                        double sum = ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBuyPrice * ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nCurVolume;
                        sum += nCurOkTradePrice * nCurOkTradeVolume;
                        ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nCurVolume += nCurOkTradeVolume;
                        ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBuyPrice = (int)(sum / ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nCurVolume);

                        if (nNoTradeVolume == 0) // 매수 전량 체결됐다면
                        {
                            // 매수가 전량 체결됐다면 
                            // 체결-매수취소와 유사하게 진행된다 하나 다른점은 매수취소완료 시그널을 건들 필요가 없다는 것이다.
                            // 현재매수취소 그리고 일부라도 체결됐으니 해당레코드에 구매됐다는 시그널을 on해주고 레코드인덱스를 한칸 늘린다
                            // 매수요청 카운트도 낮추고 현재 매매중인 시그널을 off해준다.
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].isAllBuyed = true; // 해당종목의 매수레코드의 매수완료 on
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBuyEndTime = nSharedTime; // 주문체결완료 시간 설정
                            if (ea[nCurIdx].myTradeManager.nBuyReqCnt > 0)
                                ea[nCurIdx].myTradeManager.nBuyReqCnt--; // 매수요청 카운트 감소
                            ea[nCurIdx].myTradeManager.isOrderStatus = false; // 매매중 off

                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].tenLineManager.nTimeDegree = TEN_SEC;
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBirthTime = nSharedTime - nSharedTime % ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].tenLineManager.nTimeDegree;
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].tenLineManager.arrTimeLine = new TimeLine[BRUSH + SubTimeToTimeAndSec(MARKET_END_TIME, ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBirthTime) / ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].tenLineManager.nTimeDegree];
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBirthPrice = ea[nCurIdx].nFs;
                            testTextBox.AppendText(sTradeTime + " : " + sCode + " 매수 체결완료 \r\n"); //++


                            // 두두두둥
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].eachSw = new StreamWriter(new FileStream(sEachPath + ea[nCurIdx].sCode + "-" + ea[nCurIdx].sCodeName + "-" + ea[nCurIdx].myTradeManager.nIdx.ToString() + "번째" + "--게시판.txt", FileMode.Create));

                        }
                    } //  END ---- 매수체결 끝
                    else if (sOrderStatus.Equals("접수"))
                    {
                        if (nNoTradeVolume == 0) // 전량 매수취소가 완료됐다면
                        {
                            // 접수-매수취소는
                            // 체결이 하나도 안된상태에서 매수주문이 모두 매수취소 된 상황이다
                            // 많은 설정을 할 필요가 없다
                            // 여기서는 isAllBuyed와 현재레코드인덱스를 더하지 않는 이유는 체결데이터가 없기때문에
                            // 굳이 인덱스를 늘려 레코드만 증가시킨다면 실시간에서 관리함에 시간이 더 소요되기 때문이다
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].isCanceling = false; // 해당종목의 현재매수취소버튼 초기화
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].isAllBuyed = true; // 해당종목은 모두 사졌다.( 사실 사지도 못했지만 그냥 검색차단을 위해 다 true로 만들어버린다.)
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].isAllSelled = true; // 해당종목은 모두 팔렸다.
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBuyEndTime = nSharedTime; // 주문체결완료 시간 설정
                            if (ea[nCurIdx].myTradeManager.nBuyReqCnt > 0)
                                ea[nCurIdx].myTradeManager.nBuyReqCnt--; // 매수요청 카운트 감소
                            ea[nCurIdx].myTradeManager.isOrderStatus = false; // 매매중 off
                            nCurDepositCalc += ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nOrderVolume * ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nOrderPrice;
                        }
                        else // 매수 주문인경우
                        {
                            // 원주문번호만 설정해준다.
                            nCurDepositCalc -= nOrderVolume * ea[nCurIdx].myTradeManager.nCurLimitPrice; // 예수금에서 매매수수료까지 포함해서 차감

                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nBuyedSlotId = ea[nCurIdx].myTradeManager.nIdx++; // 매수레코드 수 증가
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nOrderVolume = ea[nCurIdx].myTradeManager.nOrderVolume; // 주문수량
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nRequestTime = ea[nCurIdx].myTradeManager.nCurRqTime; // 주문신청 시간
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nReceiptTime = nSharedTime; // 주문접수 시간
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nOrderPrice = ea[nCurIdx].myTradeManager.nCurLimitPrice; // 주문신청가 + Alpha 가격
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nOriginOrderPrice = ea[nCurIdx].myTradeManager.nCurRqPrice; // 주문신청할때의 가격
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].isStepByStepTrade = ea[nCurIdx].myTradeManager.isStepByStepTrade; // 스텝매매 여부 설정
                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].isStepByStepTrade) // 스텝매매 여부에 따른 익절가와 손절가 설정
                            {
                                ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].fTargetPer = GetHigherCeilingTarget(ref ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nCurLineIdx);
                                ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].fBottomPer = GetHigherFloorTarget(ref ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nCurLineIdx);
                            }
                            else
                            {
                                ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].fTargetPer = ea[nCurIdx].myTradeManager.fTargetPercent;
                                ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].fBottomPer = ea[nCurIdx].myTradeManager.fBottomPercent;
                            }
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nStrategyIdx = ea[nCurIdx].myTradeManager.nStrategyIdx; // 전략인덱스
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nSequence = ea[nCurIdx].myTradeManager.nSequence; // 순번인덱스
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].sCurOrgOrderId = sOrderId; // 현재원주문번호 설정
                            ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].fTradeRatio = ea[nCurIdx].myTradeManager.fTradeRatio;

                            ea[nCurIdx].myTradeManager.isOrderStatus = true; // 매매중 on

                            testTextBox.AppendText(sTradeTime + " : " + sCode + ", " + nOrderVolume.ToString() + "(주) 매수 접수완료 \r\n"); //++

                        }
                    } // END ---- 매수접수끝
                } // END ---- orderType.Equals("매수")
                else if (sOrderType.Equals("매도"))
                {
                    if (sOrderStatus.Equals("체결"))
                    {
                        int nOkTradeVolume = Math.Abs(int.Parse(sCurOkTradeVolume));
                        int nOkTradePrice = Math.Abs(int.Parse(sCurOkTradePrice));

                        nCurDepositCalc += (int)(nOkTradeVolume * nOkTradePrice * (1 - (STOCK_TAX + VIRTUAL_STOCK_FEE * 2)));
                        ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nCurVolume -= nOkTradeVolume;

                        if (nNoTradeVolume == 0)
                        {
                            if (ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].isSelling)
                            {
                                ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].isSelling = false;
                                ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].isAllSelled = true;
                            }
                            if (ea[nCurIdx].myTradeManager.nSellReqCnt > 0) //** 아침에 어제 매도 안된 애들이 남아있으면 sellReqCnt가 음수가 될 수 도 있으니 0이 넘어야만 차감되게끔 한다.
                                ea[nCurIdx].myTradeManager.nSellReqCnt--;

                            ea[nCurIdx].myTradeManager.isOrderStatus = false; // 매매중 off
                            testTextBox.AppendText(sTradeTime + " : " + sCode + " 매도 체결완료 \r\n"); //++
                        }
                    }
                    else if (sOrderStatus.Equals("접수"))
                    {
                        testTextBox.AppendText(sTradeTime + " : " + sCode + ", " + nOrderVolume.ToString() + "(주) 매도 접수완료 \r\n"); //++
                        ea[nCurIdx].myTradeManager.isOrderStatus = true; // 매매중 on
                        ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].sCurOrgOrderId = sOrderId; // 원주문번호
                    }
                } // END ---- orderType.Equals("매도")
                else if (sOrderType.Equals("매수취소"))
                {
                    // ----------------------------------
                    // 야기할 수 있는 문제
                    // 1. 매수취소확인후 접수,체결을 안보내준다.
                    // 2. 매수취소확인전에 접수,체결을 보내준다.
                    // ----------------------------------

                    // 매수취소에서는 매수취소완료버튼 on
                    // 매수취소수량이 있으면 그만큼 예수금 더해주면 된다
                    // 거래중, 매매완료 등등의 처리는 매수에서 완료한다.
                    if (sOrderStatus.Equals("접수"))
                    {
                        testTextBox.AppendText(sTradeTime + " : " + sCode + ", " + nOrderVolume.ToString() + "(주) 매수취소 접수완료 \r\n"); //++
                        // 매수취소 접수가 되면 거의 확정적으로 매수취소확인 따라오며 
                        // 매수취소 접수때부터 이미 매수취소된거같음.
                    }
                    else if (sOrderStatus.Equals("확인"))
                    {
                        // 매수취소확인은 사실상 매수취소 수량이 있는거고 미체결량은 0인 상태일 테지만 
                        // 예기치 못한 오류로 인해 문제가 생길 수 도 있으니
                        // 매수취소 수량과 미체결량을 검사해준다.
                        if (nNoTradeVolume < nOrderVolume && nOrderVolume > 0) // 매수취소된 수량이 있다면
                        {
                            nCurDepositCalc += (int)((nOrderVolume - nNoTradeVolume) * (ea[nCurIdx].myTradeManager.arrBuyedSlots[nCurBuySlotIdx].nOrderPrice * (1 + VIRTUAL_STOCK_FEE)));
                        }

                    }
                } // END ---- orderType.Equals("매수취소")
                else if (sOrderType.Equals("매도취소"))
                {
                }
                else if (sOrderType.Equals("매수정정"))
                {
                }
                else if (sOrderType.Equals("매도정정"))
                {
                }

            } // End ---- e.sGubun.Equals("0") : 접수,체결

            else if (e.sGubun.Equals("1")) // 잔고
            {
                string sCode = axKHOpenAPI1.GetChejanData(9001).Substring(1); // 종목코드
                int nCodeIdx = Math.Abs(int.Parse(sCode));
                nCurIdx = eachStockIdxArray[nCodeIdx];

                int nHoldingQuant = Math.Abs(int.Parse(axKHOpenAPI1.GetChejanData(930))); // 보유수량
                ea[nCurIdx].nHoldingsCnt = nHoldingQuant;
            } // End ---- e.sGubun.Equals("1") : 잔고
        }// End ---- 체잔 핸들러
    }
}
