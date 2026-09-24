// Write the same 1,000,000 bytes to /dev/null four ways. The bytes are identical; only how
// they're batched before crossing into the kernel changes. This file does the writing and
// checks it completed correctly — the actual syscall counts come from running the compiled
// version under `strace -c`, not from anything this program measures itself:
//
//   dotnet build -c Release -o out
//   strace -f -c -e trace=pwrite64 -- dotnet out/<assembly>.dll
//
// filtering to one mode at a time (see the page for the real counts this produced).

const int Bytes = 1_000_000;
byte[] one = new byte[1] { 1 };
byte[] block = new byte[4096];
Array.Fill(block, (byte)1);

static FileStream Open(int bufferSize) =>
    new("/dev/null", FileMode.Open, FileAccess.Write, FileShare.ReadWrite, bufferSize);

// A. unbuffered, one byte per Write call — every call crosses into the kernel
void PerByteUnbuffered()
{
    using var fs = Open(0);
    for (int i = 0; i < Bytes; i++) fs.Write(one, 0, 1);
}

// B. same one-byte calls, but a 64 KiB user-space buffer absorbs 65,536 of them per crossing
void PerByteBuffered()
{
    using var fs = Open(65536);
    for (int i = 0; i < Bytes; i++) fs.Write(one, 0, 1);
    fs.Flush();
}

// C. unbuffered again, batched by hand into 4 KiB calls
void PerBlockUnbuffered()
{
    int whole = Bytes / block.Length;                  // 244 full blocks, then the remainder
    using var fs = Open(0);
    for (int i = 0; i < whole; i++) fs.Write(block, 0, block.Length);
    fs.Write(block, 0, Bytes - whole * block.Length);   // exactly 1,000,000 bytes, like the others
}

// D. control: the same byte-at-a-time loop, but into a plain array — no stream, no syscall at all
long PerByteMemoryOnly()
{
    var userBuffer = new byte[65536];
    int pos = 0;
    long sum = 0;
    for (int i = 0; i < Bytes; i++)
    {
        userBuffer[pos++] = one[0];
        sum += userBuffer[pos - 1];                     // touch every byte so the loop can't be dropped
        if (pos == userBuffer.Length) pos = 0;
    }
    return sum;
}

PerByteUnbuffered();
PerByteBuffered();
PerBlockUnbuffered();
var sum = PerByteMemoryOnly();

if (sum != Bytes) throw new Exception($"FAIL: expected checksum {Bytes}, got {sum}");
Console.WriteLine("PASS");
