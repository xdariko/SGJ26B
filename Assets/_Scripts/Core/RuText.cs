// Русские формы слов после числа: 1 посылку, 2 посылки, 5 посылок
public static class RuText
{
    // forms = «посылку|посылки|посылок»
    public static string Plural(int n, string forms)
    {
        string[] f = forms.Split('|');
        if (f.Length < 3)
            return forms;

        int m100 = System.Math.Abs(n) % 100;
        int m10 = m100 % 10;
        if (m100 >= 11 && m100 <= 14)
            return f[2];
        if (m10 == 1)
            return f[0];
        return m10 >= 2 && m10 <= 4 ? f[1] : f[2];
    }
}
