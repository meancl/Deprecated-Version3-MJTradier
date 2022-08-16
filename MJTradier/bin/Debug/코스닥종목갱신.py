from pykrx import stock
from datetime import datetime, timedelta


yesterday = datetime.now() #- timedelta(2)
dtype_yesterday = datetime.strftime(yesterday, '%Y%m%d')

kospi_df = stock.get_market_ticker_list(market= 'KOSDAQ')
stock_info_list = []

for idx, stk in enumerate(kospi_df):
    df = stock.get_market_ohlcv(dtype_yesterday,dtype_yesterday,stk)
    stockName = stock.get_market_ticker_name(stk)
    stock_info_list.append([stk, df['거래량'].iat[0], stockName])

n_sep =  1300
stock_info_list.sort(key= lambda x:-x[1])
sep_kospi_df = stock_info_list
cnt = 0

w = open('today_kosdaq_stock_code.txt','w')

for idx, stock in enumerate(sep_kospi_df):

    if "스팩" in stock[2] or "인버스" in stock[2] or "레버리지" in stock[2] or "EX" in stock[2] or "선물" in stock[2] or "채권" in stock[2] or "국채" in stock[2] or "국고채" in stock[2] :
        print(idx ,'번째 종목 : ',stock[2], ' pass')
        continue
    
    try:
        int(stock[0])
        cnt += 1
        w.write(stock[0])
        if cnt < n_sep :
            w.write('\n')
        else:
            break
    except:
        pass

w.close()
