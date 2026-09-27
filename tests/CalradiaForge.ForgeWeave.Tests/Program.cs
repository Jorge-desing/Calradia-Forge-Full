using System;
using CalradiaForge.ForgeWeave.Tests;

internal static class Program
{
    static int passed,failed;

    static int Main()
    {
        ForgeWeaveTests.Run(Test);
        ForgeUiContractTests.Run(Test);
        Console.WriteLine("RESULT: "+passed+" passed, "+failed+" failed");
        return failed==0?0:1;
    }

    static void Test(string name,Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS "+name); }
        catch(Exception error) { failed++; Console.WriteLine("FAIL "+name+": "+error); }
    }
}
