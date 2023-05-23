using System.Text;

namespace Infrastructure;

public static class PasswordGenerator
{
    public static string GenerateTempBrokerPasswd(int SizeMiddle)
    {
        StringBuilder builder = new StringBuilder();
        Random random = new Random();
        var firstInt = random.Next(1, 10);
        if (firstInt < 5) builder.Append(firstInt + 3);
        char ch;
        for (int i = 0; i < SizeMiddle; i++)
        {
            ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
            builder.Append(ch);
        }
        if (firstInt < 3) builder.Append('!');
        else builder.Append("9!");
        return builder.ToString();
    }
}
