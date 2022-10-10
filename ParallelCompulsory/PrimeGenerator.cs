using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelCompulsory
{
    public class PrimeGenerator
    {
        // An attempt at Sieve of Eratosthenes --fauxtistic
        public List<long> GetPrimesSequential(long first, long last, CancellationToken token)
        {
            List<long> primes = new List<long>();

            if (last < 2)
            {
                return primes;
            }

            if (first <= 2)
            {
                primes.Add(2);
            }

            if (last < 3)
            {
                return primes;
            }            

            bool[] isNotPrime = new bool[last + 1];

            // Checking only odd numbers shaves off ~10% or so
            for (long i = 3; i <= last; i = i + 2)
            {
                // we may consider returning a partial list later;
                token.ThrowIfCancellationRequested();

                if (!isNotPrime[i])
                {
                    if (i >= first)
                    {
                        primes.Add(i);
                    }                    
                    for (long j = i; j <= last; j = j + i)
                    {
                        isNotPrime[j] = true;
                    }
                }
            }

            return primes;
        }

        public List<long> GetPrimesParallel(long first, long last, CancellationToken token)
        {
            List<long> primes = new List<long>();

            if (last < 2)
            {
                return primes;
            }

            long[] numbers;

            // doubled performance by only checking odd numbers (and 2) as prime candidates
            if (first <= 2)
            {
                numbers = new long[((last - 1) / 2) + 1];
                numbers[0] = 2;
                for (int i = 1; i < numbers.Length; i++)
                {
                    numbers[i] = i * 2 + 1;  
                }
            }
            else
            {
                long start = first % 2 == 0 ? first + 1 : first;
                numbers = new long[(last - first) / 2 + 1];
                for (int i = 0; i < numbers.Length; i++)
                {
                    numbers[i] = i * 2 + start;
                }
            }

            primes = numbers.AsParallel()
                .WithCancellation(token)
                .Where(x => IsPrime(x))
                .ToList();

            return primes;
        }

        private bool IsPrime(long number)
        {
            var isPrime = true;
            if (number == 2)
            {
                return isPrime;
            }

            // only checking up to squareroot is *essential* for performance
            // also doubled performance by only checking for divisibility with odd numbers
            for (long i = 3; i * i <= number; i = i + 2)
            {
                if (number % i == 0)
                {
                    isPrime = false;
                }
            }

            return isPrime;
        }

        // much faster than GetPrimesParallel in most cases; however is *almost* never faster than
        // GetPrimesSequential (but getting closer), whereas GetPrimesParallel is faster than GetPrimesSequential
        // in some special cases with finding prime numbers in small intervals between high numbers
        
        // As such, there is presently no use case for this method, but with some improvements,
        // it might beat the sequential version -- very much WIP
        public List<long> GetPrimesParallelEratosthenes(long first, long last, CancellationToken token)
        {
            bool[] isNotPrime = new bool[last + 1];

            isNotPrime[0] = true;
            isNotPrime[1] = true;

            Parallel.For(2, last + 1, (i) =>
            {
                token.ThrowIfCancellationRequested();

                if (!isNotPrime[i])
                {
                    for (long j = i + i; j <= last; j = j + i)
                    {
                        if (isNotPrime[i])
                        {
                            break;
                        }
                        isNotPrime[j] = true;
                    }
                }
                
            });

            List<long> primes = isNotPrime.AsParallel()
                .Select((x, i) => new KeyValuePair<int, bool>(i, x))
                .Where(x => x.Value == false && x.Key >= first)
                .Select(x => (long)x.Key)
                .ToList();

            return primes;
        }
    }
}
