My attempts at the 1BRC

Various implementation approaches are in the Implementations folder.

Notable implementations:
- NaiveImplementation.cs
    - Initial implementation (with some slight optimizations).  Ran around 2-2:30
- SepImplementation:
    - This is not a viable implementation for the challenge as it uses an external library called Sep.  I used this as a relative 'high performance' benchmark for evaluation.
- SpanParse:
    - This version changes the parsing significantly. Instead of using the default File ReadLines calls and splitting the string, we read into a raw byte buffer.  
    - Initially we were immediately converting to String and then doing string.Split to divide the line, but this didn't yield significant improvement over the Naive versions for obvious reasons.
    - The final version splits the byte array directly, and only converts to string when necessary to load the Dictionary and parse the measurement value.
    - This version got me to around 1:05
- ParallelSpanParse:
    - This takes the SpanParse version but divides the file into chunks and parses those chunks in parallel.
    - After some experimenting it seems like the 10_000_000 buffer size was the most optimal
    - Since we're splitting the file randomly, we need to make sure we handle the chunks at the end/beginning that get split, so there is special logic for saving off the truncated portions and processing them after the fact. 
    - I attempted some other versions like not using the Task Runtime and instead using Parallel.ForEach, or including the file parsing as part of the awaited task, but this seemed to be the best iteration for whatever reason.
        - These are som eof the other variations like ParallelSpanParse2/3/NoAsyncParse
    - This version got me to around 15-20ish seconds
- ParallelSpanBinaryKey:
    - I had the idea that instead of converting to string on every iteration and storing that as the key, what if we just used the binary array as the key and converted to string at the very end. 
    - To implement this we needed to implement a special IEqualityComparer and tell our dictionary to use that. 
    - This version was around 12-13 seconds
- ParallelSpanBinaryKeyManualParse:
    - Knowing we had specific limitations of the incoming measurements (single digit precision, relatively small total values), I wanted to optimize the double parse portion
    - This version just adds the ParseDouble method that evaluates the byte array and creates an associated double. 
    - This version is around 10-11 seconds. 

Potential improvements:
- migrate the final solution to a producer/consumer model like we do in the ChannelImplementation so we can start parsing as soon as we ingest a chunk... right now there's about 3-4 seconds of ingestion before we start actually parsing
- Better file reading... I experimented some with MemoryMapped file in MMapImplementation, but didn't see significant improvements. Maybe with the other changes we'd see some improvements manifest. 
- I experimented some with NativeAOT compilation, but saw very mixed results. This may be because of how the benchmark tool and our final run have warmup steps that get the JIT going.  Would like to experiment some more with this.
- We're using a record for the Measurement, but a struct might be more performant.


To build: 
`dotnet publish -c Release`


Other notes:
- BenchmarkTester.cs is used to run benchmarks between different versions. I removed the BenchmarkDotNet library in the final project and commented out all the benchmarks.

- IProcessFile is a simple interface so all the implementations adhere to a standard.