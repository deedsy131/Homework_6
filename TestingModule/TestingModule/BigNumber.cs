using System;
using System.Collections.Generic;
using System.Linq;

namespace TestingModule
{
    public class BigNumber : IComparable<BigNumber>
    {
        private static Random random = new Random();

        public List<byte> Value { get; private set; }
        public bool IsNegative { get; private set; }

        #region Constructors
        public BigNumber()
        {
            Value = new List<byte> { 0 };
        }

        public BigNumber(long start)
        {
            var startStr = start.ToString().Replace("-", "");
            Value = new List<byte>(startStr.Length);
            IsNegative = start < 0;

            for (int index = startStr.Length - 1; index >= 0; index--)
            {
                Value.Add(byte.Parse(startStr[index].ToString()));
            }
        }

        public BigNumber(string numberStr)
        {
            if (string.IsNullOrWhiteSpace(numberStr) || numberStr == "-")
            {
                Value = new List<byte> { 0 };
                return;
            }

            if (numberStr[0] == '-')
            {
                numberStr = numberStr.Substring(1);
                IsNegative = numberStr.Length > 0;
            }

            Value = new List<byte>(numberStr.Length);

            for (int i = 0; i < numberStr.Length; i++)
            {
                var digitChar = numberStr[i];

                if (byte.TryParse(digitChar.ToString(), out byte digitByte) == false)
                {
                    throw new ApplicationException("BigNumber must be a valid integer.");
                }

                Value.Add(digitByte);
            }
        }

        public BigNumber(BigNumber clone)
        {
            IsNegative = clone.IsNegative;
            Value = new List<byte>(clone.Value.Count);
            for (int i = 0; i < clone.Value.Count; i++)
            {
                Value.Add(clone.Value[i]);
            }
        }

        public static BigNumber Random(int inclusiveMinimumLength = 0, int exclusiveMaximumLength = int.MaxValue)
        {
            int length = random.Next(inclusiveMinimumLength, exclusiveMaximumLength % 100000);
            BigNumber result = new BigNumber
            {
                IsNegative = random.Next() % 2 == 0,
                Value = new List<byte>(length)
            };
            
            for (int i = 0; i < length; i++)
            {
                byte temp = (byte)random.Next(0, 10);
                while (i == 0 && temp == 0)
                {
                    temp = (byte)random.Next(0, 10);
                }

                result.Value.Add(temp);
            }

            return result;
        }

        public void Set(BigNumber toCopy)
        {
            IsNegative = toCopy.IsNegative;
            Value = toCopy.Value;
        }
        #endregion

        #region Add

        public static BigNumber Add(string bn1, string bn2)
        {
            var result = new BigNumber(bn1);
            result.Add(bn2);
            return result;
        }

        public static BigNumber Add(BigNumber bn1, BigNumber bn2)
        {
            var result = new BigNumber(bn1);
            result.Add(bn2);
            return result;
        }

        public void Add(long addend)
        {
            Add(new BigNumber(addend));
        }

        public void Add(string addend)
        {
            Add(new BigNumber(addend));
        }

        public void Add(BigNumber addend)
        {
            // to do, check if either is negative and do subtract instead
            if (IsNegative && addend.IsNegative)
            {
                IsNegative = addend.IsNegative = false;
                Add(addend); // recursive
                IsNegative = addend.IsNegative = true;
            }
            else if (addend.IsNegative)
            {
                addend.IsNegative = false;
                Subtract(addend);
                addend.IsNegative = true;
            }
            else if (IsNegative)
            {
                IsNegative = false;
                var temp = Subtract(addend, this);
                Set(temp);
            }
            else // do the add, both are positive
            {             
                // make sure they are same Length
                ExpandCapacity(addend.Value.Count);
                addend.ExpandCapacity(Value.Count);

                for (int i = addend.Value.Count - 1; i >= 0; i--)
                {
                    byte temp = (byte)(Value[i] + addend.Value[i]);

                    //determine if carry
                    if (temp >= 10)
                    {
                        Value[i] = (byte)(temp % 10);

                        if (i == 0)
                        {
                            ExpandCapacity(Value.Count + 1);
                            Value[0] = 1;
                        }
                        else
                        {
                            Value[i - 1]++;
                        }
                    }
                    else
                    {
                        Value[i] = temp;
                    }
                }

                addend.Simplify();
                Simplify();
            }
        }
        #endregion

        #region Subtract

        public static BigNumber Subtract(string minuend, string subtrahend)
        {
            var result = new BigNumber(minuend);
            result.Subtract(subtrahend);
            return result;
        }

        public static BigNumber Subtract(BigNumber minuend, BigNumber subtrahend)
        {
            var result = new BigNumber(minuend);
            result.Subtract(subtrahend);
            return result;
        }

        public void Subtract(long subtrahend)
        {
            Subtract(new BigNumber(subtrahend));
        }

        public void Subtract(string subtrahend)
        {
            Subtract(new BigNumber(subtrahend));
        }

        public void Subtract(BigNumber subtrahend)
        {
            if (subtrahend.IsNegative)
            {
                subtrahend.IsNegative = false;
                Add(subtrahend);
                subtrahend.IsNegative = true;
            }
            else if (IsNegative)
            {
                IsNegative = false;
                Add(subtrahend);
                IsNegative = true;
            }
            else if (this < subtrahend)
            {
                var temp = Subtract(subtrahend, this);
                Set(temp);
                IsNegative = true;
            }
            else
            { 
                // make sure they are same Length
                ExpandCapacity(subtrahend.Value.Count);
                subtrahend.ExpandCapacity(Value.Count);

                for (int i = subtrahend.Value.Count - 1; i >= 0; i--)
                {
                    if (Value[i] < subtrahend.Value[i])
                    {
                        Borrow(i);
                    }

                    Value[i] = (byte)(Value[i] - subtrahend.Value[i]);
                }

                subtrahend.Simplify();
                Simplify();
            }
        }
        #endregion

        #region Multiply

        public static BigNumber Multiply(BigNumber bn1, BigNumber bn2)
        {
            BigNumber result = new BigNumber(bn1);
            result.Multiply(bn2);
            return result;
        }

        public static BigNumber Multiply(string bn1, string bn2)
        {
            var bigNumber1 = new BigNumber(bn1);
            var bigNumber2 = new BigNumber(bn2);
            return Multiply(new BigNumber(bn1), new BigNumber(bn2));
        }

        public void Multiply(long factor)
        {
            Multiply(new BigNumber(factor));
        }

        public void Multiply(string factor)
        {
            Multiply(this, new BigNumber(factor));
        }

        public void Multiply(BigNumber factor)
        {
            BigNumber product = new BigNumber();

            if (this.IsNegative && factor.IsNegative)
            {
                this.IsNegative = false;
                factor.IsNegative = false;
            }
            else if (factor.IsNegative)
            {
                this.IsNegative = true;
            }
            
            for (BigNumber loopFactor = new BigNumber(); loopFactor < factor.Abs(); loopFactor++)
            {
                product.Add(this);
            }

            product.IsNegative = product.IsNegative || factor.IsNegative;
            Assign(product);
        }
        #endregion

        #region Compare
            public int CompareTo(BigNumber other)
        {
            if (other == null || other.IsNegative && !IsNegative)
            { 
                return 1;
            }
            else if (!other.IsNegative && IsNegative)
            {
                return -1;
            }
            else 
            {
                other.ExpandCapacity(Value.Count);
                ExpandCapacity(other.Value.Count);

                try
                {
                    bool checkIfLonger = true;
                    for (int i = 0; i < Value.Count; i++)
                    {
                        if (checkIfLonger)
                        {
                            // first to be non-zero is the longer digit number
                            if (Value[i] > 0 && other.Value[i] > 0)
                            {
                                checkIfLonger = false;
                            }
                            else if (Value[i] > 0)
                            {
                                return IsNegative ? -1 : 1;
                            }
                            else if (other.Value[i] > 0)
                            {
                                return IsNegative ? 1 : -1;
                            }
                        }

                        if (!checkIfLonger)
                        {
                            // both are same length digits. Check each digit individually
                            if (Value[i] > other.Value[i])
                            {
                                return IsNegative ? -1 : 1;
                            }
                            else if (Value[i] < other.Value[i])
                            {
                                return IsNegative ? 1 : -1;
                            }
                        }
                    }
                }
                finally
                {
                    other.Simplify();
                    Simplify();
                }
            }

            return 0;
        }

        public static bool operator <(BigNumber e1, BigNumber e2)
        {
            return e1.CompareTo(e2) < 0;
        }

        public static bool operator >(BigNumber e1, BigNumber e2)
        {
            return e1.CompareTo(e2) > 0;
        }

        public static bool operator ==(BigNumber e1, BigNumber e2)
        {
            if (e1 is null && e2 is null)
            {
                return true;
            }
            else if (e1 is null || e2 is null)
            {
                return false;
            }
            else
            { 
                return e1.CompareTo(e2) == 0;
            }
        }

        public static bool operator !=(BigNumber e1, BigNumber e2)
        {
            return e1.CompareTo(e2) != 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is BigNumber otherBigNumber)
            {
                return otherBigNumber == this;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                
                for (int i = 1; i <= Value.Count; i++)
                {
                    hashCode += (i * 10) + Value[i];
                }

                return IsNegative ? hashCode * -1 : hashCode;
            }
        }
        #endregion

        #region operator Overloads

        public static BigNumber operator *(BigNumber bn1, BigNumber bn2)
        {
            return BigNumber.Multiply(bn1, bn2);
        }

        public static BigNumber operator +(BigNumber bn1, BigNumber bn2)
        {
            return BigNumber.Add(bn1, bn2);
        }

        public static BigNumber operator -(BigNumber bn1, BigNumber bn2)
        {
            return BigNumber.Subtract(bn1, bn2);
        }

        public static BigNumber operator +(BigNumber bigNumber)
        {
            return new BigNumber(bigNumber);
        }

        public static BigNumber operator -(BigNumber bigNumber)
        {
            var result = new BigNumber(bigNumber);
            return result.Opposite();
        }

        public static BigNumber operator ++(BigNumber bigNumber)
        {
            return BigNumber.Add(bigNumber, new BigNumber(1));
        }

        public static BigNumber operator --(BigNumber bigNumber)
        {
            return BigNumber.Subtract(bigNumber, new BigNumber(-1));
        }
        #endregion

        #region Private
        private void ExpandCapacity(int greaterCapacity)
        {
            if (greaterCapacity > Value.Count)
            {
                byte[] temp = new byte[greaterCapacity];

                int currentindex = Value.Count - 1;
                for (int i = greaterCapacity - 1; i >= 0; i--)
                {
                    temp[i] = currentindex >= 0 ? Value[currentindex--] : (byte)0;
                }

                Value = temp.ToList();
            }
        }

        private void Simplify()
        {
            Value = Value.SkipWhile(value => value == 0).ToList();

            if (Value.Count == 0)
            {
                Value = new List<byte>() { 0 };
                IsNegative = false;
            }
        }

        private void Borrow(int index)
        {
            Value[index--] += 10;

            if (index == -1)
            {
                IsNegative = true;
            }
            else
            {
                if (Value[index] == 0)
                {
                    Borrow(index);
                }
                Value[index] -= 1;
            }
        }
        #endregion

        #region Public

        public void Assign(BigNumber copy)
        {
            Value = copy.Value;
            IsNegative = copy.IsNegative;
        }

        public BigNumber Opposite()
        {
            var defaultBigNumber = new BigNumber();
            var result = new BigNumber(this);
            result.Simplify();

            if (result != defaultBigNumber)
            {
                result.IsNegative = !result.IsNegative;
            }

            return result;
        }

        public BigNumber Abs()
        {
            var defaultBigNumber = new BigNumber();
            var result = new BigNumber(this);
            result.Simplify();
            result.IsNegative = false;
            
            return result;
        }

        public string ToCommaString()
        {
            string numberStr = IsNegative ? "-" : "";

            int leading = Value.Count % 3;

            if (leading > 0)
            {
                numberStr += string.Join(string.Empty, Value.Take(leading));
                if (Value.Count > 3)
                {
                    numberStr += ',';
                }
            }

            int commaGroup = 0;

            foreach (var digit in Value.Skip(leading))
            {
                if (commaGroup++ == 3)
                {
                    numberStr += ',';
                    commaGroup = 1;
                }
                numberStr += digit;
            }

            return string.IsNullOrEmpty(numberStr) ? "0" : numberStr;
        }

        public override string ToString()
        {
            string numberStr = IsNegative ? "-" : "";
            numberStr += string.Join("", Value);
            return numberStr;
        }
        #endregion
    }
}