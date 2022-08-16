namespace MJTradier
{
    public partial class Form1
    {
        // ===============================================================
        // KIWOOM_LIBRARY
        // ===============================================================

        /// <summary>
        /// kiwoom time형 데이터 간 시간차이를 kiwoom time형으로 반환해줌
        /// retTime = KiwoomTime(timeToBeSub - timeToSub)
        /// </summary>
        /// <param name="timeToBeSub"></param>
        /// <param name="timeToSub"></param>
        /// <returns></returns>
        int SubTimeToTime(int timeToBeSub, int timeToSub)
        {
            if (timeToBeSub <= timeToSub)
                return 0;

            int secToBeSub = (int)(timeToBeSub / 10000) * 3600 + (int)(timeToBeSub / 100) % 100 * 60 + timeToBeSub % 100;
            int secToSub = (int)(timeToSub / 10000) * 3600 + (int)(timeToSub / 100) % 100 * 60 + timeToSub % 100;
            int diffTime = secToBeSub - secToSub;
            int hour = diffTime / 3600;
            int minute = (diffTime % 3600) / 60;
            int second = diffTime % 60;

            return hour * 10000 + minute * 100 + second;
        }
        /// <summary>
        /// kiwoom time형 데이터 간 시간차이를 int형 sec로 반환해줌
        /// retSec = Seconds(timeToBeSub - timeToSub)
        /// </summary>
        /// <param name="timeToBeSub"></param>
        /// <param name="timeToSub"></param>
        /// <returns></returns>
        int SubTimeToTimeAndSec(int timeToBeSub, int timeToSub)
        {
            if (timeToBeSub <= timeToSub)
                return 0;

            int secToBeSub = (int)(timeToBeSub / 10000) * 3600 + (int)(timeToBeSub / 100) % 100 * 60 + timeToBeSub % 100;
            int secToSub = (int)(timeToSub / 10000) * 3600 + (int)(timeToSub / 100) % 100 * 60 + timeToSub % 100;

            return secToBeSub - secToSub;
        }

        /// <summary>
        /// kiwoom time형 데이터를 int형  sec만큼 감소하여 반환해줌
        /// retTime = KiwoomTime(Seconds(timeToBeSub) - subSec)
        /// </summary>
        /// <param name="timeToBeSub"></param>
        /// <param name="subSec"></param>
        /// <returns></returns>
        int SubTimeBySec(int timeToBeSub, int subSec)
        {
            int secToBeSub = (int)(timeToBeSub / 10000) * 3600 + (int)(timeToBeSub / 100) % 100 * 60 + timeToBeSub % 100;
            if (subSec <= 0)
                return timeToBeSub;

            if (secToBeSub <= subSec)
                return 0;
            secToBeSub -= subSec;
            int hour = secToBeSub / 3600;
            int minute = (secToBeSub % 3600) / 60;
            int second = secToBeSub % 60;

            return hour * 10000 + minute * 100 + second;
        }

        /// <summary>
        /// kiwoom time형 데이터를 int형 sec만큼 증가시켜 반환해줌
        /// retTime = KiwoomTime(Seconds(timeToBeAdd) + addSec)
        /// </summary>
        /// <param name="timeToBeAdd"></param>
        /// <param name="addSec"></param>
        /// <returns></returns>
        int AddTimeBySec(int timeToBeAdd, int addSec)
        {
            int secToBeAdd = (int)(timeToBeAdd / 10000) * 3600 + (int)(timeToBeAdd / 100) % 100 * 60 + timeToBeAdd % 100;
            secToBeAdd += addSec;
            int hour = secToBeAdd / 3600;
            int minute = (secToBeAdd % 3600) / 60;
            int second = secToBeAdd % 60;

            return hour * 10000 + minute * 100 + second;
        }

        /// <summary>
        /// int형 sec을 kiwoom time형으로 변환하여 반환해줌
        /// retTime = KiwoomTime(timeSec)
        /// </summary>
        /// <param name="timeSec"></param>
        /// <returns></returns>
        int GetKiwoomTime(int timeSec)
        {
            return (int)(timeSec / 3600) * 10000 + (int)(timeSec % 3600 / 60) * 100 + timeSec % 60;
        }

        /// <summary>
        /// kiwoom time형 데이터를 int형 sec로 변환하여 반환해줌
        /// retSec = Seconds(kiwoomTime)
        /// </summary>
        /// <param name="kiwoomTime"></param>
        /// <returns></returns>
        int GetSec(int kiwoomTime)
        {
            return (int)(kiwoomTime / 10000) * 3600 + (int)(kiwoomTime / 100) % 100 * 60 + kiwoomTime % 100;
        }

        /// <summary>
        /// Kosdaq현재가 기준 가격틱의 차이를 반환해준다.
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        int GetKosdaqGap(int price)
        {
            int gap;
            if (price < 1000)
                gap = 1;
            else if (price < 5000)
                gap = 5;
            else if (price < 10000)
                gap = 10;
            else if (price < 50000)
                gap = 50;
            else
                gap = 100;
            return gap;
        }

        /// <summary>
        /// Kospi현재가 기준 가격틱의 차이를 반환해준다.
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        int GetKospiGap(int price)
        {
            int gap;
            if (price < 1000)
                gap = 1;
            else if (price < 5000)
                gap = 5;
            else if (price < 10000)
                gap = 10;
            else if (price < 50000)
                gap = 50;
            else if (price < 100000)
                gap = 100;
            else if (price < 500000)
                gap = 500;
            else
                gap = 1000;

            return gap;
        }

        /// <summary>
        /// 마켓을 첫번째인자로 받아 해당마켓의 현재가 기준 가격틱의 차이를 반환해준다.
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        int GetAutoGap(int nMarketId, int price)
        {
            if (nMarketId == KOSPI_ID)
                return GetKospiGap(price);
            else
                return GetKosdaqGap(price);
        }






        /// KiwoomLibrary Part 종료
        /////////////////////////////////

    }
}
