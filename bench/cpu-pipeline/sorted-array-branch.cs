// Evidence for /systems/cpu-pipeline/sorted-array-branch/ -- run with:
//   dotnet run bench/cpu-pipeline/sorted-array-branch.cs
//
// Builds a software model of a real branch predictor -- a table of 2-bit saturating
// counters indexed by recent history -- and counts exactly how many times it guesses
// wrong on four different orderings of the same 16,384 bytes. No timing anywhere.
// The same 16,384 bytes, summed with `if (data[i] >= 128) sum += data[i];`, seen in four
// different orders. Every order performs the same 16,384 comparisons and the same 8,206
// additions -- only the ORDER of taken/not-taken decisions changes.
using System.Linq;

const int N = 16384;
var rng = new Random(12345);
var shuffled = new byte[N];
rng.NextBytes(shuffled);

var sorted = (byte[])shuffled.Clone();
Array.Sort(sorted);

// The same values re-ordered into runs of `run` above the threshold, then `run` below it --
// not sorted, but a pattern with a short, exact period.
var high = shuffled.Where(b => b >= 128).ToArray();
var low = shuffled.Where(b => b < 128).ToArray();
byte[] Blocks(int run)
{
    var a = new byte[N];
    for (int i = 0, h = 0, l = 0; i < N; i++)
        a[i] = (i / run) % 2 == 0
            ? (h < high.Length ? high[h++] : low[l++])
            : (l < low.Length ? low[l++] : high[h++]);
    return a;
}

var orders = new (string Name, byte[] Data)[]
{
    ("shuffled",     shuffled),
    ("sorted",       sorted),
    ("blocks of 64", Blocks(64)),
    ("alternating",  Blocks(1)),
};

// A software model of a real branch predictor: `bits` of recent global history select one of
// 2^bits two-bit saturating counters (0 = strongly not-taken .. 3 = strongly taken). Each pass
// replays the SAME sequence, so the table carries state from the previous pass into the next --
// exactly what happens when a real predictor sees the same loop run many times.
static int[] Mispredicts(bool[] outcomes, int bits, int passes)
{
    int size = 1 << bits;
    var counters = new byte[size];
    for (int i = 0; i < size; i++) counters[i] = 1;      // start weakly "not taken"
    int history = 0, mask = size - 1;
    var result = new int[passes];
    for (int p = 0; p < passes; p++)
    {
        int mispredicts = 0;
        foreach (var taken in outcomes)
        {
            bool predicted = counters[history] >= 2;
            if (predicted != taken) mispredicts++;
            if (taken) { if (counters[history] < 3) counters[history]++; }
            else       { if (counters[history] > 0) counters[history]--; }
            history = ((history << 1) | (taken ? 1 : 0)) & mask;
        }
        result[p] = mispredicts;
    }
    return result;
}

Console.WriteLine($"{N:N0} elements, {high.Length:N0} at or above 128\n");
int[] widths = { 0, 1, 7, 14 };
foreach (var (name, data) in orders)
{
    var outcomes = data.Select(b => b >= 128).ToArray();
    foreach (var bits in widths)
    {
        var mp = Mispredicts(outcomes, bits, 3);
        Console.WriteLine($"{name,-14} history={bits,2} bits   pass 1: {mp[0],5} ({100.0 * mp[0] / N,4:F1}%)   pass 3: {mp[2],5} ({100.0 * mp[2] / N,4:F1}%)");
    }
    Console.WriteLine();
}
