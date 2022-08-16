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
        public Holdings[] holdingsArray; // 현재 보유주식을 담을 구조체 배열 // tr
        public int nHoldingCnt; // 총 보유주식의 수 // tr
        public int nCurHoldingsIdx; // 보유주식을 담을때 사용하는 인덱스 변수  // tr



        // ============================================
        // 계좌평가잔고내역요청 TR요청메소드
        // CommRqData 3번째 인자 sPrevNext가 0일 경우 처음 20개의 종목을 요청하고
        // 2일 경우 초기20개 초과되는 종목들을 계속해서 요청한다.
        // ============================================
        private void RequestHoldings(int sPrevNext)
        {
            if (sPrevNext == 0)
            {
                nHoldingCnt = 0;
                nCurHoldingsIdx = 0;
            }
            axKHOpenAPI1.SetInputValue("계좌번호", sAccountNum);
            axKHOpenAPI1.SetInputValue("비밀번호", "");
            axKHOpenAPI1.SetInputValue("비밀번호입력매체구분", "00");
            axKHOpenAPI1.SetInputValue("조회구분", "2"); // 1:합산 2:개별
            axKHOpenAPI1.CommRqData("계좌평가잔고내역요청", "opw00018", sPrevNext, SetTrScreenNo());
        }


        // ============================================
        // 예수금상세현황요청 TR요청메소드
        // ============================================
        private void RequestDeposit()
        {
            axKHOpenAPI1.SetInputValue("계좌번호", sAccountNum);
            axKHOpenAPI1.SetInputValue("비밀번호", "");
            axKHOpenAPI1.SetInputValue("비밀번호입력매체구분", "00");
            axKHOpenAPI1.SetInputValue("조회구분", "2");
            axKHOpenAPI1.CommRqData("예수금상세현황요청", "opw00001", 0, SetTrScreenNo());
        }

        // ============================================
        // 당일실현손익상세요청 TR요청메소드
        // ============================================
        private void RequestTradeResult()
        {
            axKHOpenAPI1.SetInputValue("계좌번호", sAccountNum);
            axKHOpenAPI1.SetInputValue("비밀번호", "");
            axKHOpenAPI1.SetInputValue("종목코드", "");
            axKHOpenAPI1.CommRqData("당일실현손익상세요청", "opt10077", 0, SetTrScreenNo());
        }

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
            if (e.sRQName.Equals("예수금상세현황요청"))
            {
                nCurDeposit = Math.Abs(int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "주문가능금액")));
                if (nCurDepositCalc == 0)
                {
                    nCurDepositCalc = nCurDeposit;
                    depositCalcLabel.Text = nCurDepositCalc.ToString() + "(원)";
                    testTextBox.AppendText("계산용예수금 세팅 완료\r\n"); //++
                }
                testTextBox.AppendText("예수금 세팅 완료\r\n"); //++
                myDepositLabel.Text = nCurDeposit.ToString() + "(원)";
            }
            else if (e.sRQName.Equals("계좌평가잔고내역요청"))
            {
                int rows = axKHOpenAPI1.GetRepeatCnt(e.sTrCode, e.sRecordName);
                nHoldingCnt += rows;

                for (int myMoneyIdx = 0; nCurHoldingsIdx < nHoldingCnt; nCurHoldingsIdx++, myMoneyIdx++)
                {
                    holdingsArray[nCurHoldingsIdx].sCode = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, myMoneyIdx, "종목번호").Trim().Substring(1);
                    holdingsArray[nCurHoldingsIdx].sCodeName = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, myMoneyIdx, "종목명").Trim();
                    holdingsArray[nCurHoldingsIdx].fYield = double.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, myMoneyIdx, "수익률(%)"));
                    holdingsArray[nCurHoldingsIdx].nHoldingQty = Math.Abs(int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, myMoneyIdx, "보유수량")));
                    holdingsArray[nCurHoldingsIdx].nBuyedPrice = Math.Abs(int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, myMoneyIdx, "매입가")));
                    holdingsArray[nCurHoldingsIdx].nCurPrice = Math.Abs(int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, myMoneyIdx, "현재가")));
                    holdingsArray[nCurHoldingsIdx].nTotalPL = Math.Abs(int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, myMoneyIdx, "평가손익")));
                    holdingsArray[nCurHoldingsIdx].nNumPossibleToSell = Math.Abs(int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, myMoneyIdx, "매매가능수량")));
                }

                if (e.sPrevNext.Equals("2"))
                {
                    RequestHoldings(2);
                }
                else // 보유잔고 확인 끝
                {
                    if (isForCheckHoldings) // 오직 확인용
                    {
                        isForCheckHoldings = false;
                        if (nHoldingCnt == 0)
                        {
                            testTextBox.AppendText("현재 보유종목이 없습니다.\r\n");//++
                        }
                        else
                        {
                            for (int myStockIdx = 0; myStockIdx < nHoldingCnt; myStockIdx++)
                            {
                                testTextBox.AppendText((myStockIdx + 1).ToString() + " 종목번호 : " + holdingsArray[myStockIdx].sCode + ", 종목명 : " + holdingsArray[myStockIdx].sCodeName + ", 보유수량 : " + holdingsArray[myStockIdx].nHoldingQty.ToString() + ", 평가손익 : " + holdingsArray[myStockIdx].nTotalPL.ToString() + "\r\n"); //++
                            }
                        }
                    }
                    else // 처분용
                    {

                        if (nHoldingCnt == 0)
                        {
                            testTextBox.AppendText("현재 보유종목이 없습니다.\r\n");//++
                        }
                        else // 보유종목이 있다면
                        {
                            for (int allSellIdx = 0; allSellIdx < nHoldingCnt; allSellIdx++)
                            {
                                int nSellReqResult = axKHOpenAPI1.SendOrder("시간초과매도", SetTradeScreenNo(), sAccountNum, 2, holdingsArray[allSellIdx].sCode, holdingsArray[allSellIdx].nNumPossibleToSell, 0, MARKET_ORDER, "");  // 전량 시장가매도

                                if (nSellReqResult != 0) // 요청이 성공하지 않으면
                                {
                                    testTextBox.AppendText(holdingsArray[allSellIdx].sCode + " 매도신청 전송 실패 \r\n"); //++

                                }
                                else
                                {
                                    nCurIdx = eachStockIdxArray[int.Parse(holdingsArray[allSellIdx].sCode)];
                                    ea[nCurIdx].myTradeManager.nSellReqCnt++; // 초기판매중인데 다른 작업이 접근할 수 없게
                                    testTextBox.AppendText((allSellIdx + 1).ToString() + " " + holdingsArray[allSellIdx].sCode + " 매도신청 전송 성공 \r\n"); //++
                                }
                                Delay(350); // 1초에 5번 제한이지만 혹시 모르니 1초에 3번정도로 제한으로
                            }
                        }
                    }
                }
            } // END ---- e.sRQName.Equals("계좌평가잔고내역요청")

            else if (e.sRQName.Equals("당일실현손익상세요청"))
            {
                int nTodayResult = int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "당일실현손익"));
                testTextBox.AppendText("당일실현손익 : " + nTodayResult.ToString() + "(원) \r\n"); //++
                int rows = axKHOpenAPI1.GetRepeatCnt(e.sTrCode, e.sRecordName);

                string sCode;
                string sCodeName;
                int nTradeVolume;
                double fBuyPrice;
                int nTradePrice;
                double fYield;

                for (int todayProfitIdx = 0; todayProfitIdx < rows; todayProfitIdx++)
                {
                    sCode = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, todayProfitIdx, "종목코드").Trim().Substring(1);
                    sCodeName = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, todayProfitIdx, "종목명").Trim();
                    fYield = double.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, todayProfitIdx, "손익율"));
                    nTradeVolume = Math.Abs(int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, todayProfitIdx, "체결량")));
                    fBuyPrice = Math.Abs(double.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, todayProfitIdx, "매입단가")));
                    nTradePrice = Math.Abs(int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, todayProfitIdx, "체결가")));

                    testTextBox.AppendText("종목명 : " + sCodeName + ", 종목코드 : " + sCode + ", 체결량 : " + nTradeVolume.ToString() + ", 매입단가 : " + fBuyPrice.ToString() + ", 체결가 : " + nTradePrice.ToString() + ", 손익율 : " + fYield.ToString() + "(%) \r\n"); //++
                }
            } // END ---- e.sRQName.Equals("당일실현손익상세요청")
            else if (e.sRQName.Equals("주식기본정보요청"))
            {
                int nCodeIdx = int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "종목코드"));
                nCurIdx = eachStockIdxArray[nCodeIdx];

                ea[nCurIdx].sCodeName = axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "종목명").Trim();
                try
                {
                    ea[nCurIdx].lShareOutstanding = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "유통주식")) * 1000;
                    ea[nCurIdx].lTotalNumOfStock = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "상장주식")) * 1000; ;
                    ea[nCurIdx].nYesterdayEndPrice = Math.Abs(int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "현재가")));
                }
                catch (Exception ex)
                {
                    ea[nCurIdx].lShareOutstanding = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "상장주식")) * 1000;
                    ea[nCurIdx].lTotalNumOfStock = ea[nCurIdx].lShareOutstanding;
                    ea[nCurIdx].nYesterdayEndPrice = Math.Abs(int.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRecordName, 0, "현재가")));
                }

                StreamWriter tmpSw = new StreamWriter(new FileStream(sBasicInfoPath + ea[nCurIdx].sCode + ".txt", FileMode.Create));
                tmpSw.Write(ea[nCurIdx].sCodeName + "," + ea[nCurIdx].lShareOutstanding.ToString() + "," + ea[nCurIdx].lTotalNumOfStock.ToString() + "," + ea[nCurIdx].nYesterdayEndPrice.ToString());
                tmpSw.Close();
            }

        } // END ---- TR 이벤트 핸들러




    }
}
