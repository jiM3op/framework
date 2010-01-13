﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Linq.Expressions;

namespace Signum.Entities.Extensions.Basics
{
    [Serializable]
    public class DateSpanDN : EmbeddedEntity
    {
        int years;
        public int Years
        {
            get { return years; }
            set  { SetToStr(ref years, value, () => Years); }
        }

        int months;
        public int Months
        {
            get { return months; }
            set { SetToStr(ref months, value, () => Months); }
        }

        int days;
        public int Days
        {
            get { return days; }
            set { SetToStr(ref days, value, () => Days); }
        }

        public bool IsZero()
        {
            return years == 0 && months == 0 && days == 0;
        }

        //static Expression<Func<DateSpanDN, DateTime>> MethodExpression =
        //     (ds, dt) => dt.AddYears(ds.Years).AddMonths(ds.Months).AddDays(ds.Days);
        //public DateTime Add(DateTime date)
        //{
        //    return date.AddYears(years).AddMonths(months).AddDays(days);
        //}


        static Expression<Func<DateSpanDN,DateTime, DateTime>> AddExpression =
             (ds, dt) => dt.AddYears(ds.Years).AddMonths(ds.Months).AddDays(ds.Days);
        public  DateTime Add( DateTime date)
        {
            return AddExpression.Invoke(this, date);
        }

        public DateSpan ToDateSpan()
        {
            return new DateSpan(years, months, days);
        }

        public override string ToString()
        {
            return ToDateSpan().ToString();
        }
    }
}
