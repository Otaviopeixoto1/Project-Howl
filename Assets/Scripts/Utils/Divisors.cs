using System;
using System.Collections.Generic;

public static class Divisors
    {
        /// <summary>
        /// Finds all the divisors of any positive integer passed as argument. 
        /// Returns an array of int with all the divisors of the argument.
        /// Returns null if the argument is zero or negative.
        /// </summary>
        public static int[] GetDivisors(int n)
        {
            if (n <= 0)
            {
                return null;
            }
            List<int> divisors = new List<int>();
            for (int i = 1; i <= Math.Sqrt(n); i++)
            {
                if (n % i == 0)
                {
                    divisors.Add(i);
                    if (i != n / i)
                    {
                        divisors.Add(n / i);
                    }
                }
            }
            divisors.Sort();
            return divisors.ToArray();
        }
    }
