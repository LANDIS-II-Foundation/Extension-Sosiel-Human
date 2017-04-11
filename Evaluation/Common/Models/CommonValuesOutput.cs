﻿using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;

using FileHelpers;

namespace Common.Models
{
    public class ValueItem
    {
        public string Name { get; set; }

        public double Value { get; set; }


        public override string ToString()
        {
            return $"{Name};{Value.ToString("0.000")}";
        }
    }

    [DelimitedRecord(";")]
    public class CommonValuesOutput: IHeader
    {
        public string HeaderLine
        {
            get
            {
                return "Iteration;Variable;Value";
            }
        }


        [FieldOrder(0)]
        public int Iteration { get; set; }

        //todo: replace on property
        [FieldOrder(1)]
        [FieldConverter(typeof(ToStringConverter))]
        public List<ValueItem> Values;
    }
}
