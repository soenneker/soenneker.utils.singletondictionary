using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Soenneker.Utils.SingletonDictionary.Tests.Iterations;
using Soenneker.Utils.SingletonDictionary.Abstract;
using System.Threading;

namespace Soenneker.Utils.SingletonDictionary.Tests.Benchmarks;

[ThreadingDiagnoser]
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class Benchmark
{
    // Test parameters for different concurrency levels and operation counts
    [Params(1, 4, 8, 16)] public int ConcurrencyLevel;
    [Params(100, 1_000, 10_000)] public int OperationsPerThread;

    private SingletonDictionary<TestObject> _taskBasedDictionary = null!;
    private SingletonDictionaryLock<TestObject> _lockBasedDictionary = null!;
    
    // Test object to cache
    private class TestObject
    {
        public string Value { get; set; } = string.Empty;
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [GlobalSetup]
    public void Setup()
    {
        // Setup Task-based SingletonDictionary
        _taskBasedDictionary = new SingletonDictionary<TestObject>((key, cancellationToken, args) =>
        {
            // Simulate some work
            Thread.Sleep(1); // Small delay to simulate factory work
            return new ValueTask<TestObject>(new TestObject
            {
                Value = key,
                Id = key.GetHashCode(),
                CreatedAt = DateTime.UtcNow
            });
        });

        // Setup Lock-based SingletonDictionary
        _lockBasedDictionary = new SingletonDictionaryLock<TestObject>((key, cancellationToken, args) =>
        {
            // Simulate some work
            Thread.Sleep(1); // Small delay to simulate factory work
            return new ValueTask<TestObject>(new TestObject
            {
                Value = key,
                Id = key.GetHashCode(),
                CreatedAt = DateTime.UtcNow
            });
        });
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _taskBasedDictionary.DisposeAsync();
        await _lockBasedDictionary.DisposeAsync();
    }

    [Benchmark(Baseline = true)]
    public async Task TaskBasedDictionary_ConcurrentAccess()
    {
        await RunConcurrentTest(_taskBasedDictionary);
    }

    [Benchmark]
    public async Task LockBasedDictionary_ConcurrentAccess()
    {
        await RunConcurrentTest(_lockBasedDictionary);
    }

    [Benchmark]
    public async Task TaskBasedDictionary_MixedCacheHitsMisses()
    {
        await RunMixedAccessTest(_taskBasedDictionary);
    }

    [Benchmark]
    public async Task LockBasedDictionary_MixedCacheHitsMisses()
    {
        await RunMixedAccessTest(_lockBasedDictionary);
    }

    [Benchmark]
    public void TaskBasedDictionary_SyncAccess()
    {
        RunSyncTest(_taskBasedDictionary);
    }

    [Benchmark]
    public void LockBasedDictionary_SyncAccess()
    {
        RunSyncTest(_lockBasedDictionary);
    }

    private async Task RunConcurrentTest(ISingletonDictionary<TestObject> dictionary)
    {
        var tasks = new Task[ConcurrencyLevel];
        
        for (int i = 0; i < ConcurrencyLevel; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(async () =>
            {
                for (int j = 0; j < OperationsPerThread; j++)
                {
                    // Create keys that will cause some contention but not complete overlap
                    string key = $"key_{threadId}_{j % 10}";
                    await dictionary.Get(key);
                }
            });
        }

        await Task.WhenAll(tasks);
    }

    private async Task RunMixedAccessTest(ISingletonDictionary<TestObject> dictionary)
    {
        var tasks = new Task[ConcurrencyLevel];
        
        for (int i = 0; i < ConcurrencyLevel; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(async () =>
            {
                for (int j = 0; j < OperationsPerThread; j++)
                {
                    // Mix of unique keys (cache misses) and repeated keys (cache hits)
                    string key = j % 3 == 0 ? $"unique_{threadId}_{j}" : $"shared_{j % 5}";
                    await dictionary.Get(key);
                }
            });
        }

        await Task.WhenAll(tasks);
    }

    private void RunSyncTest(ISingletonDictionary<TestObject> dictionary)
    {
        var tasks = new Task[ConcurrencyLevel];
        
        for (int i = 0; i < ConcurrencyLevel; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < OperationsPerThread; j++)
                {
                    // Test synchronous access
                    string key = $"sync_key_{threadId}_{j % 5}";
                    dictionary.GetSync(key);
                }
            });
        }

        Task.WaitAll(tasks);
    }
}