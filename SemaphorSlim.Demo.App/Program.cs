using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemaphorSlim.Demo.App
{
    class Program
    {
        const int LIMIT = 10;
        const int TIME_IN_SECONDS = 5;

        static void Main(string[] args)
        {
            int limit;
            int timeInSeconds;

            Console.Write("Enter Limit: ");
            limit = int.Parse(Console.ReadLine());

            Console.Write("Enter Time in seconds: ");
            timeInSeconds = int.Parse(Console.ReadLine());

            RunAsync(limit, timeInSeconds).Wait();
        }

        public static async Task RunAsync()
        {
            await RunAsync(LIMIT, TIME_IN_SECONDS);
        }

        public static async Task RunAsync(int limit, int timeInSeconds)
        {
            var limiter = new TaskLimiter(limit, TimeSpan.FromSeconds(timeInSeconds));

            // create 100 tasks 
            var tasks = Enumerable.Range(1, 25)
                                  .Select(e => limiter.LimitAsync(() => DoSomeActionAsync(e)));

            // wait unitl all 100 tasks are completed
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        static readonly Random _rng = new Random();

        public static async Task DoSomeActionAsync(int i)
        {
            Console.WriteLine("Task {0} started", i);
            var seconds = 2 + _rng.Next(0, 4);
            await Task.Delay(seconds * 1000).ConfigureAwait(false);
            Console.WriteLine("Completed Action {0}", i);
        }

        public static async Task<T> DoSomeActionAsync<T>(int i, T req)
        {
            Console.WriteLine("Task {0} started", i);
            var seconds = 2 + _rng.Next(0, 4);
            await Task.Delay(seconds * 1000).ConfigureAwait(false);
            Console.WriteLine("Completed Action {0}", i);

            return req;
        }
    }

    public class TaskLimiter
    {
        private readonly TimeSpan _timespan;
        private readonly SemaphoreSlim _semaphore;
        private Func<Task, Task> _release;

        public TaskLimiter(int count, TimeSpan timespan)
        {
            _semaphore = new SemaphoreSlim(count, count);
            _timespan = timespan;
            _release = async (e) => {
                          await Task.Delay(_timespan);
                          _semaphore.Release(1);
                       };
        }

        public async Task LimitAsync(Func<Task> taskFactory)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            var task = taskFactory();

            await task.ContinueWith(_release);
        }

        public async Task<T> LimitAsync<T>(Func<Task<T>> taskFactory)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            var task = taskFactory();

            Func<Task<T>, Task> release = async (e) =>
            {
                await Task.Delay(_timespan);
                Console.WriteLine($"Print {e.Result}");
                _semaphore.Release(1);
            };

            Task.WaitAll(task.ContinueWith(release));

            return task.Result;
        }
    }
}
