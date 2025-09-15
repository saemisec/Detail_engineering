from datetime import datetime,timedelta
import calendar
from functools import reduce


def main():
    results = []
    for year in range(2017,2023):
        for month in range(1,13):
            test_date = datetime(year,month,1)
            date_after = test_date + timedelta(days=-1)
            date_after_str = str(date_after.month) + '/' + str(date_after.day) + '/' + str(date_after.year)
            new_month = 0
            new_year = 0
            new_day = '01'
            if month<12:
                new_month = month+1
                new_year = year
            else:
                new_month = 1
                new_year = year+1
            date_before = str(new_month) + '/' + str(new_year) + '/' + new_day

            res = reduce(lambda x,y:y[1],[calendar.monthrange(test_date.year,test_date.month)],None)
            
            results.append(rf'New-MailboxExportRequest -Mailbox "e.jangjoo@JS.JONDISHAPOUR.COM"  -FilePath "\\127.0.0.1\Export\{year}-{month}.pst"'+
                           r" -ContentFilter {(Received -gt " +f"'{date_after_str}') -and (Received -lt '{date_before}')" +  "}")
        with open("results.txt", "w", encoding="utf-8") as out_stream:
            for item in results:
                out_stream.write('%s\n'%item)
        
        








if __name__ == "__main__":
    main()