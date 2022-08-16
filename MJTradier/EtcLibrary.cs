using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MJTradier
{
    public partial class Form1
    {

        /// <summary>
        /// 기울기 변수 fN과 fM 간 사이각도를 ArcTangent를 통해 받아온다.
        /// 기울기는 2차원 좌표 내의 직선 기울기를 기준으로 함
        /// fN기울기가 fM기울기로 이동하는데 몇도 이동이 필요한 지 확인하기 위한 함수로써
        /// 값이 양수일경우 각도가 반시계방향만큼 이동이 필요하게 차이가 나고
        /// 값이 음수일경우 각도가 시계방향만큼 이동이 필요하게 차이가 난다.
        /// 수식 : ArcTangent((fM - fN) / ( fN + fM + 1 )) * ( 180 / PI )
        /// 기울기가 음수냐 양수냐에 따라 ArcTangent값이 달라져 처리가 필요했다.
        /// </summary>
        /// <param name="fN"></param>
        /// <param name="fM"></param>
        /// <returns></returns>
        double GetAngleBetween(double fN, double fM)
        {
            double fAngleDirection;

            if (fN == fM) // same
            {
                fAngleDirection = 0;
            }
            else if (fN * fM == -1) // -1
            {
                if (fN > 0)
                {
                    fAngleDirection = -90;
                }
                else
                {
                    fAngleDirection = 90;
                }
            }
            else
            {
                // 각도가 반시계 방향이면 + 
                //        시계방향이면 -
                if (fN < fM) // 분자는 양수
                {

                    if (fN * fM < 0) // 둘 중 하나가 음수면
                    {
                        if (fN * fM < -1)
                        {
                            fAngleDirection = 180 + System.Math.Atan((fM - fN) / (1 + fN * fM)) * (180 / System.Math.PI);
                        }
                        else
                        {
                            fAngleDirection = System.Math.Atan((fM - fN) / (1 + fN * fM)) * (180 / System.Math.PI);
                        }


                    }
                    else // 둘 다 음수거나 양수면
                    {
                        fAngleDirection = System.Math.Atan((fM - fN) / (1 + fN * fM)) * (180 / System.Math.PI);
                    }

                }
                else // 분자가 음수
                {
                    if (fN * fM < 0) // 둘 중 하나가 음수면
                    {
                        if (fN * fM < -1) // 둘의 곱이 -1 을 넘으면
                        {
                            fAngleDirection = -180 + System.Math.Atan((fM - fN) / (1 + fN * fM)) * (180 / System.Math.PI);
                        }
                        else // 둘의 곱이 -1 ~ 0 사이면
                        {
                            fAngleDirection = System.Math.Atan((fM - fN) / (1 + fN * fM)) * (180 / System.Math.PI);
                        }

                    }
                    else // 둘 다 음수거나 양수면
                    {
                        fAngleDirection = System.Math.Atan((fM - fN) / (1 + fN * fM)) * (180 / System.Math.PI);
                    }

                }

            }
            return fAngleDirection;
        }

        // a < b => a
        // else => b
        public int Min(int a, int b)
        {
            int retVal;
            if (a < b)
                retVal = a;
            else
                retVal = b;
            return retVal;
        }

        // a < b => a
        // else => b
        public double Min(double a, double b)
        {
            double retVal;
            if (a < b)
                retVal = a;
            else
                retVal = b;
            return retVal;
        }

        // b < a => a
        // else => b
        public int Max(int a, int b)
        {
            int retVal;
            if (a > b)
                retVal = a;
            else
                retVal = b;
            return retVal;
        }

        // b < a => a
        // else => b
        public double Max(double a, double b)
        {
            double retVal;
            if (a > b)
                retVal = a;
            else
                retVal = b;
            return retVal;
        }

        // 가변길이 매개변수용 double Min
        public double MinParams(params double[] itemList)
        {
            double retVal = double.MaxValue;

            foreach(double item in itemList)
            {
                if(retVal > item)
                {
                    retVal = item;
                }
            }
            return retVal;
        }

        // 가변길이 매개변수용 int Min
        public int MinParams(params int[] itemList)
        {
            int retVal = int.MaxValue;

            foreach (int item in itemList)
            {
                if (retVal > item)
                {
                    retVal = item;
                }
            }
            return retVal;
        }

        // 가변길이 매개변수용 double Max
        public double MaxParams(params double[] itemList)
        {
            double retVal = double.MinValue;

            foreach (double item in itemList)
            {
                if (retVal < item)
                {
                    retVal = item;
                }
            }
            return retVal;
        }

        // 가변길이 매개변수용 int Max
        public int MaxParams(params int[] itemList)
        {
            int retVal = int.MinValue;

            foreach (int item in itemList)
            {
                if (retVal < item)
                {
                    retVal = item;
                }
            }
            return retVal;
        }


        // min보다 작으면 min값을
        // max보다 크면 max값을
        // 그 사이라면 myVal를
        public int GetBetweenMinAndMax(int myVal, int minVal, int maxVal)
        {
            int retVal;

            if (myVal < minVal)
                retVal = minVal;
            else if (myVal > maxVal)
                retVal = maxVal;
            else
                retVal = myVal;

            return retVal;
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





    }
}
