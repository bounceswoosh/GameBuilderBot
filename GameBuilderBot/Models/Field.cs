﻿namespace GameBuilderBot.Models
{
    public class Field
    {
        public string Expression { get; private set; }
        public int? Value { get; set; }

        public Field(FieldIngest f)
        {
            Expression = f.Expression;
            Value = f.Value;
        }
    }
}
