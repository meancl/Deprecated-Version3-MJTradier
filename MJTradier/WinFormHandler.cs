using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MJTradier
{
    public partial class Form1
    {

        // ============================================
        // 버튼 클릭 이벤트의 핸들러 메소드
        // 1. 예수금상세현황요청
        // 2. 계좌평가잔고내역요청
        // 3. (테스트용) 강제 장시작 버튼
        // 4. (테스트용) 계산용 예수금 세팅
        // ============================================
        private void Button_Click(object sender, EventArgs e)
        {

            if (sender.Equals(checkMyAccountInfoButton)) // 예수금상세현황요청
            {
                RequestDeposit();
            }
            else if (sender.Equals(checkMyHoldingsButton)) // 계좌평가현황요청 
            {
                isForCheckHoldings = true;
                RequestHoldings(0);
            }
            else if (sender.Equals(setOnMarketButton))//삭제
            {
                isMarketStart = true;
                testTextBox.AppendText("강제 장시작 완료\r\n"); //++
            }
            else if (sender.Equals(setDepositCalcButton))
            {
                depositCalcLabel.Text = nCurDepositCalc.ToString() + "(원)";
                testTextBox.AppendText("계산용예수금 세팅 완료\r\n"); //++
            }
            else if (sender.Equals(buyButton))
            {
                if (sCodeToBuyTextBox.Text.Trim().Length == 6)
                {
                    int nStockCode = int.Parse(sCodeToBuyTextBox.Text);
                    nCurIdx = eachStockIdxArray[nStockCode];
                    if (nCurIdx != INIT_CODEIDX_NUM)
                    {
                        ea[nCurIdx].TMPisOrderSignal = true;
                    }
                }
            }
        } // End ---- 버튼클릭 핸들러



    }
}
