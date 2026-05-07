using System;

public class Program {
    public static void Main() {
        string text1 = ""1,228"";
        string normalized1 = text1.Replace(""."", """").Replace("","", """").Trim();
        decimal value1 = 0;
        decimal.TryParse(normalized1, out value1);
        Console.WriteLine(string.Format(""{0} -> {1} -> {2}"", text1, normalized1, value1));
    }
}
