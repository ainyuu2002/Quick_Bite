namespace QuickBite.Services;

public static class OrderCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate(int length = 6)
    {
        var characters = new char[length];
        for (var index = 0; index < length; index++)
        {
            characters[index] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return "QB-" + new string(characters);
    }
}
