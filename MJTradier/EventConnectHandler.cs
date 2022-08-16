using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MJTradier
{
    public partial class Form1
    {



        public string sAccountNum; // 계좌번호 // eventconnect


        // ============================================
        // 로그인 이벤트발생시 핸들러 메소드
        // ============================================
        private void OnEventConnectHandler(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnEventConnectEvent e)
        {
            if (e.nErrCode == 0) // 로그인 성공
            {



                testTextBox.AppendText("로그인 성공\r\n"); //++
                string sMyName = axKHOpenAPI1.GetLoginInfo("USER_NAME");
                string sAccList = axKHOpenAPI1.GetLoginInfo("ACCLIST"); // 로그인 사용자 계좌번호 리스트 요청
                string[] accountArray = sAccList.Split(';');

                sAccountNum = accountArray[0]; // 처음계좌가 main계좌
                accountComboBox.Text = sAccountNum;
                SubscribeRealData(); // 실시간 구독 
                RequestDeposit(); // 예수금상세현황요청 


                foreach (string sAccount in accountArray)
                {
                    if (sAccount.Length > 0)
                        accountComboBox.Items.Add(sAccount);
                }
                myNameLabel.Text = sMyName;

            }
            else
            {
                MessageBox.Show("로그인 실패");
            }
        } // END ---- 로그인 이벤트 핸들러


    }
}
