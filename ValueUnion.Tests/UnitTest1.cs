using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ValueUnion.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        X x = 1;

        float add = x switch
        {
            float y => y + 1,
            int z => z + 1,
        };
    }
}

[ValueUnion<int, float>] partial struct X;